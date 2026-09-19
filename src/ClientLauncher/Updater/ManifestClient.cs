// <copyright file="ManifestClient.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

/// <summary>
/// Loads the update manifest from a remote location.
/// </summary>
internal sealed class ManifestClient
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestClient"/> class.
    /// </summary>
    /// <param name="httpClient">The http client.</param>
    internal ManifestClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <summary>
    /// Downloads and validates the manifest.
    /// </summary>
    /// <param name="url">The url of the manifest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The manifest.</returns>
    /// <exception cref="InvalidDataException">Thrown when the manifest could not be loaded or is not valid.</exception>
    internal async Task<UpdateManifest> GetManifestAsync(string url, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                LauncherLog.Info($"Loading manifest from {url} (attempt {attempt}).");
                using var response = await this._httpClient
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var manifest = await JsonSerializer
                    .DeserializeAsync<UpdateManifest>(stream, LauncherJson.Options, cancellationToken)
                    .ConfigureAwait(false);
                if (manifest is null)
                {
                    throw new InvalidDataException("The manifest is empty.");
                }

                manifest.Validate();
                LauncherLog.Info($"Manifest loaded: runtime {manifest.Runtime.Version}, data {manifest.Data.Id}.");
                return manifest;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidDataException)
            {
                lastException = ex;
                LauncherLog.Warn($"Could not load the manifest from {url}: {ex.Message}");
                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        throw new InvalidDataException($"Could not load the update manifest from '{url}'.", lastException);
    }
}
