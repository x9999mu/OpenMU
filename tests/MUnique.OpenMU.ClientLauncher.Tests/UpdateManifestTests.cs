// <copyright file="UpdateManifestTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

/// <summary>
/// Tests for the <see cref="UpdateManifest"/> and the <see cref="ManifestClient"/>.
/// </summary>
internal sealed class UpdateManifestTests
{
    private const string ValidSha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Test]
    public void Validate_WithValidManifest_DoesNotThrow()
    {
        var manifest = CreateValidManifest();

        Assert.That(manifest.Validate, Throws.Nothing);
    }

    [Test]
    public void Validate_WithUnsupportedSchemaVersion_Throws()
    {
        var manifest = CreateValidManifest();
        manifest.SchemaVersion = 99;

        Assert.That(manifest.Validate, Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void Validate_WithoutDataId_Throws()
    {
        var manifest = CreateValidManifest();
        manifest.Data.Id = null;

        Assert.That(manifest.Validate, Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void Validate_WithInsecureUrl_Throws()
    {
        var manifest = CreateValidManifest();
        manifest.Runtime.Archive!.Url = "http://example.org/runtime.tar.gz";

        Assert.That(manifest.Validate, Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void Validate_WithInvalidHash_Throws()
    {
        var manifest = CreateValidManifest();
        manifest.Data.Archive!.Sha256 = "abc";

        Assert.That(manifest.Validate, Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public async Task GetManifestAsync_WithValidManifest_ReturnsManifest()
    {
        using var server = new TestHttpServer();
        var manifest = CreateValidManifest();
        server.AddFile("/manifest.json", JsonSerializer.Serialize(manifest, LauncherJson.Options));
        using var httpClient = new HttpClient();
        var client = new ManifestClient(httpClient);

        var result = await client.GetManifestAsync(server.BaseUrl + "manifest.json", CancellationToken.None);

        Assert.That(result.Runtime.Version, Is.EqualTo("1.0.0"));
        Assert.That(result.Data.Id, Is.EqualTo("id1"));
        Assert.That(result.Preserve, Does.Contain("config.ini"));
    }

    [Test]
    public void GetManifestAsync_WithMissingFile_Throws()
    {
        using var server = new TestHttpServer();
        using var httpClient = new HttpClient();
        var client = new ManifestClient(httpClient);

        Assert.That(
            async () => await client.GetManifestAsync(server.BaseUrl + "missing.json", CancellationToken.None),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void Deserialize_WithManifestOfTheGeneratorScript_IsValid()
    {
        // This is the output of client/tools/generate_manifest.py of the MuMain repository.
        // It guards the contract between the generator and this launcher.
        const string json = """
        {
          "schemaVersion": 1,
          "channel": "stable",
          "generatedAtUtc": "2026-09-19T07:02:04Z",
          "runtime": {
            "version": "1.4.2",
            "tag": "v1.4.2",
            "archive": {
              "url": "https://github.com/x9999mu/MuMain/releases/download/v1.4.2/MuMain-windows-native-x64-release-editor-off-no-data.tar.gz",
              "size": 16777216,
              "sha256": "21b0e33f316915551cca77e617b073b99b53ac813b450f6ae0ff26025a086029",
              "format": "tar.gz"
            }
          },
          "data": {
            "id": "7fce146eb3fe34a0",
            "tag": "data-7fce146eb3fe34a0",
            "archive": {
              "url": "https://github.com/x9999mu/MuMain/releases/download/data-7fce146eb3fe34a0/MuMain-data-7fce146eb3fe34a0.tar.gz",
              "size": 468713472,
              "sha256": "8148c2a6c4b5470839a220026db23cf6aa2246010fe38c740c4ed7455c445910",
              "format": "tar.gz"
            }
          },
          "server": {
            "host": "100.108.169.118",
            "hostName": "",
            "port": 44405
          },
          "preserve": [
            "config.ini"
          ],
          "launcher": {
            "version": "1.0.0",
            "url": "https://github.com/x9999mu/OpenMU/releases/download/launcher-v1.0.0/MuMainLauncher.exe",
            "size": 41943040,
            "sha256": "ec9a6e9fe278eb1a471fbab6f40367d8548078b651d9c71581c57c2a6ca379e0"
          }
        }
        """;

        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, LauncherJson.Options);

        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest!.Validate, Throws.Nothing);
        Assert.That(manifest.Runtime.Version, Is.EqualTo("1.4.2"));
        Assert.That(manifest.Data.Id, Is.EqualTo("7fce146eb3fe34a0"));
        Assert.That(manifest.Server!.Port, Is.EqualTo(44405));
        Assert.That(manifest.Launcher!.Version, Is.EqualTo("1.0.0"));
    }

    private static UpdateManifest CreateValidManifest()
    {
        return new UpdateManifest
        {
            SchemaVersion = UpdateManifest.SupportedSchemaVersion,
            Channel = "stable",
            Runtime = new UpdateManifest.PackageInfo
            {
                Version = "1.0.0",
                Archive = new UpdateManifest.ArchiveInfo
                {
                    Url = "https://example.org/runtime.tar.gz",
                    Size = 1024,
                    Sha256 = ValidSha256,
                    Format = "tar.gz",
                },
            },
            Data = new UpdateManifest.PackageInfo
            {
                Id = "id1",
                Archive = new UpdateManifest.ArchiveInfo
                {
                    Url = "https://example.org/data.tar.gz",
                    Size = 2048,
                    Sha256 = ValidSha256,
                    Format = "tar.gz",
                },
            },
        };
    }
}
