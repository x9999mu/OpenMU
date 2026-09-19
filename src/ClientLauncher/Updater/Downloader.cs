// <copyright file="Downloader.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

/// <summary>
/// Downloads archives with resume support and verifies them by their sha256 hash.
/// </summary>
internal sealed class Downloader
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloader"/> class.
    /// </summary>
    /// <param name="httpClient">The http client.</param>
    internal Downloader(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <summary>
    /// Downloads the specified archive into the target file and verifies it.
    /// </summary>
    /// <param name="url">The download url.</param>
    /// <param name="targetFilePath">The path of the file to create.</param>
    /// <param name="expectedSize">The expected size in bytes.</param>
    /// <param name="expectedSha256">The expected sha256 hash.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    internal async Task DownloadAsync(
        string url,
        string targetFilePath,
        long expectedSize,
        string expectedSha256,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (File.Exists(targetFilePath)
            && new FileInfo(targetFilePath).Length == expectedSize
            && string.Equals(await FileHasher.HashFileAsync(targetFilePath, cancellationToken).ConfigureAwait(false), expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            LauncherLog.Info($"Files from cache used: {targetFilePath}");
            return;
        }

        var partFilePath = targetFilePath + ".part";
        Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await this.DownloadWithResumeAsync(url, partFilePath, expectedSize, progress, cancellationToken).ConfigureAwait(false);

                var partLength = new FileInfo(partFilePath).Length;
                if (partLength != expectedSize)
                {
                    throw new InvalidDataException($"Expected {expectedSize} bytes but downloaded {partLength} bytes.");
                }

                progress?.Report(new DownloadProgress(expectedSize, expectedSize, 0, Path.GetFileName(targetFilePath), true));
                var hash = await FileHasher.HashFileAsync(partFilePath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(hash, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Hash mismatch for '{Path.GetFileName(targetFilePath)}': expected {expectedSha256}, got {hash}.");
                }

                if (File.Exists(targetFilePath))
                {
                    File.Delete(targetFilePath);
                }

                File.Move(partFilePath, targetFilePath);
                LauncherLog.Info($"Downloaded and verified {targetFilePath} ({expectedSize} bytes).");
                return;
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidDataException)
            {
                LauncherLog.Warn($"Download attempt {attempt} for {url} failed: {ex.Message}");
                if (attempt >= MaxAttempts)
                {
                    throw;
                }

                if (ex is InvalidDataException)
                {
                    TryDelete(partFilePath);
                }

                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException ex)
        {
            LauncherLog.Warn($"Could not delete '{filePath}': {ex.Message}");
        }
    }

    private async Task DownloadWithResumeAsync(
        string url,
        string partFilePath,
        long expectedSize,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var alreadyDownloaded = File.Exists(partFilePath) ? new FileInfo(partFilePath).Length : 0;
        if (alreadyDownloaded > expectedSize)
        {
            TryDelete(partFilePath);
            alreadyDownloaded = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (alreadyDownloaded > 0)
        {
            request.Headers.Range = new RangeHeaderValue(alreadyDownloaded, null);
        }

        using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && alreadyDownloaded == expectedSize)
        {
            return;
        }

        response.EnsureSuccessStatusCode();

        if (alreadyDownloaded > 0 && response.StatusCode != HttpStatusCode.PartialContent)
        {
            // The server does not support resuming; start from the beginning.
            TryDelete(partFilePath);
            alreadyDownloaded = 0;
        }

        var totalBytes = response.Content.Headers.ContentLength is { } contentLength
            ? alreadyDownloaded + contentLength
            : expectedSize;

        var fileMode = alreadyDownloaded > 0 ? FileMode.Append : FileMode.Create;
        await using var fileStream = new FileStream(partFilePath, fileMode, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var buffer = new byte[1024 * 1024];
        var received = alreadyDownloaded;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        int read;
        while ((read = await responseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;

            if (progress is not null && (stopwatch.Elapsed - lastReport).TotalMilliseconds >= 250)
            {
                lastReport = stopwatch.Elapsed;
                var bytesPerSecond = stopwatch.Elapsed.TotalSeconds > 0 ? received / stopwatch.Elapsed.TotalSeconds : 0;
                progress.Report(new DownloadProgress(received, totalBytes, bytesPerSecond, Path.GetFileName(partFilePath), false));
            }
        }

        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
