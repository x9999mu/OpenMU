// <copyright file="DownloaderTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;

/// <summary>
/// Tests for the <see cref="Downloader"/>.
/// </summary>
internal sealed class DownloaderTests
{
    [Test]
    public async Task DownloadAsync_WithValidFile_CreatesVerifiedFile()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var content = CreateRandomContent(64 * 1024);
        server.AddFile("/file.bin", content);
        var targetPath = Path.Combine(directory.Path, "file.bin");
        using var httpClient = new HttpClient();
        var downloader = new Downloader(httpClient);

        await downloader.DownloadAsync(server.BaseUrl + "file.bin", targetPath, content.Length, Hash(content), null, CancellationToken.None);

        Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(content));
        Assert.That(File.Exists(targetPath + ".part"), Is.False);
    }

    [Test]
    public void DownloadAsync_WithWrongHash_Throws()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var content = CreateRandomContent(8 * 1024);
        server.AddFile("/file.bin", content);
        var targetPath = Path.Combine(directory.Path, "file.bin");
        using var httpClient = new HttpClient();
        var downloader = new Downloader(httpClient);

        Assert.That(
            async () => await downloader.DownloadAsync(server.BaseUrl + "file.bin", targetPath, content.Length, new string('0', 64), null, CancellationToken.None),
            Throws.TypeOf<InvalidDataException>());
        Assert.That(File.Exists(targetPath), Is.False);
    }

    [Test]
    public async Task DownloadAsync_WithInterruptedTransfer_ResumesDownload()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var content = CreateRandomContent(256 * 1024);
        server.AddFile("/file.bin", content);
        server.AbortAfterBytes = 100 * 1024;
        var targetPath = Path.Combine(directory.Path, "file.bin");
        using var httpClient = new HttpClient();
        var downloader = new Downloader(httpClient);

        await downloader.DownloadAsync(server.BaseUrl + "file.bin", targetPath, content.Length, Hash(content), null, CancellationToken.None);

        Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(content));
        Assert.That(server.GetRequestCount("/file.bin"), Is.EqualTo(2));
    }

    [Test]
    public async Task DownloadAsync_WithCachedFile_DoesNotDownloadAgain()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var content = CreateRandomContent(4 * 1024);
        server.AddFile("/file.bin", content);
        var targetPath = Path.Combine(directory.Path, "file.bin");
        File.WriteAllBytes(targetPath, content);
        using var httpClient = new HttpClient();
        var downloader = new Downloader(httpClient);

        await downloader.DownloadAsync(server.BaseUrl + "file.bin", targetPath, content.Length, Hash(content), null, CancellationToken.None);

        Assert.That(server.GetRequestCount("/file.bin"), Is.Zero);
    }

    private static byte[] CreateRandomContent(int length)
    {
        var content = new byte[length];
        Random.Shared.NextBytes(content);
        return content;
    }

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
