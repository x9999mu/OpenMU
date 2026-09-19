// <copyright file="UpdateServiceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

/// <summary>
/// End to end tests for the <see cref="UpdateService"/>, using a local http server.
/// </summary>
internal sealed class UpdateServiceTests
{
    private const string RuntimeFileName = "/runtime.tar.gz";
    private const string DataFileName = "/data.tar.gz";

    [Test]
    public async Task CheckAndApply_DownloadsOnlyChangedPackages()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var installDirectory = Path.Combine(directory.Path, "client");
        var runtimeBytes = CreateRuntimePackage(directory.Path, "1.0.0", "runtime v1");
        var dataBytes = CreateDataPackage(directory.Path, "data v1", ["fonts/DejaVuSans.ttf"]);
        server.AddFile(RuntimeFileName, runtimeBytes);
        server.AddFile(DataFileName, dataBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id1", runtimeBytes, dataBytes));
        using var service = CreateService(directory.Path, installDirectory, server.BaseUrl);

        var check = await service.CheckAsync(CancellationToken.None);
        Assert.That(check.UpdateRequired, Is.True);
        Assert.That(check.IsClientInstalled, Is.False);

        await service.ApplyAsync(check, null, CancellationToken.None);

        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Main.exe")), Is.EqualTo("runtime v1"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Data", "Dec2.dat")), Is.EqualTo("data v1"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "config.ini")), Is.EqualTo("ServerIP=archive"));
        Assert.That(server.GetRequestCount(RuntimeFileName), Is.EqualTo(1));
        Assert.That(server.GetRequestCount(DataFileName), Is.EqualTo(1));

        var secondCheck = await service.CheckAsync(CancellationToken.None);
        Assert.That(secondCheck.UpdateRequired, Is.False);
        Assert.That(server.GetRequestCount(RuntimeFileName), Is.EqualTo(1));
        Assert.That(server.GetRequestCount(DataFileName), Is.EqualTo(1));

        // The player changes the configuration; a new data package is published.
        File.WriteAllText(Path.Combine(installDirectory, "config.ini"), "ServerIP=local");
        var newDataBytes = CreateDataPackage(directory.Path, "data v2", []);
        server.AddFile(DataFileName, newDataBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id2", runtimeBytes, newDataBytes));

        var thirdCheck = await service.CheckAsync(CancellationToken.None);
        Assert.That(thirdCheck.NeedsRuntime, Is.False);
        Assert.That(thirdCheck.NeedsData, Is.True);
        await service.ApplyAsync(thirdCheck, null, CancellationToken.None);

        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Data", "Dec2.dat")), Is.EqualTo("data v2"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "config.ini")), Is.EqualTo("ServerIP=local"));
        Assert.That(server.GetRequestCount(RuntimeFileName), Is.EqualTo(1));
        Assert.That(server.GetRequestCount(DataFileName), Is.EqualTo(2));

        // Files which are not part of the new archive are kept, see design decision D9.
        Assert.That(File.Exists(Path.Combine(installDirectory, "fonts", "DejaVuSans.ttf")), Is.True);
    }

    [Test]
    public async Task Apply_WhenGameIsRunning_Throws()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var installDirectory = Path.Combine(directory.Path, "client");
        var runtimeBytes = CreateRuntimePackage(directory.Path, "1.0.0", "runtime v1");
        var dataBytes = CreateDataPackage(directory.Path, "data v1", []);
        server.AddFile(RuntimeFileName, runtimeBytes);
        server.AddFile(DataFileName, dataBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id1", runtimeBytes, dataBytes));
        using var service = CreateService(directory.Path, installDirectory, server.BaseUrl);
        var firstCheck = await service.CheckAsync(CancellationToken.None);
        await service.ApplyAsync(firstCheck, null, CancellationToken.None);

        var newRuntimeBytes = CreateRuntimePackage(directory.Path, "1.1.0", "runtime v2");
        server.AddFile(RuntimeFileName, newRuntimeBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.1.0", "id1", newRuntimeBytes, dataBytes));
        var nextCheck = await service.CheckAsync(CancellationToken.None);
        Assert.That(nextCheck.NeedsRuntime, Is.True);

        using var lockStream = new FileStream(Path.Combine(installDirectory, "Main.exe"), FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.That(
            async () => await service.ApplyAsync(nextCheck, null, CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task CleanInstall_ReplacesFilesAndKeepsConfiguration()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var installDirectory = Path.Combine(directory.Path, "client");
        var runtimeBytes = CreateRuntimePackage(directory.Path, "1.0.0", "runtime v1");
        var dataBytes = CreateDataPackage(directory.Path, "data v1", []);
        server.AddFile(RuntimeFileName, runtimeBytes);
        server.AddFile(DataFileName, dataBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id1", runtimeBytes, dataBytes));
        using var service = CreateService(directory.Path, installDirectory, server.BaseUrl);
        await service.ApplyAsync(await service.CheckAsync(CancellationToken.None), null, CancellationToken.None);

        File.WriteAllText(Path.Combine(installDirectory, "leftover.txt"), "stale");
        File.WriteAllText(Path.Combine(installDirectory, "config.ini"), "ServerIP=local");
        service.PrepareCleanInstall();
        var check = await service.CheckAsync(CancellationToken.None);
        Assert.That(check.IsClientInstalled, Is.False);

        await service.ApplyAsync(check, null, CancellationToken.None);

        Assert.That(File.Exists(Path.Combine(installDirectory, "leftover.txt")), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Main.exe")), Is.EqualTo("runtime v1"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "config.ini")), Is.EqualTo("ServerIP=local"));
    }

    private static UpdateService CreateService(string rootDirectory, string installDirectory, string baseUrl)
    {
        var settings = new LauncherSettings
        {
            InstallDirectory = installDirectory,
            ManifestUrl = baseUrl + "manifest.json",
        };
        return new UpdateService(settings, rootDirectory)
        {
            RequiredFreeSpace = 0,
        };
    }

    private static byte[] CreateRuntimePackage(string directory, string version, string marker)
    {
        var archivePath = Path.Combine(directory, $"runtime-{version}.tar.gz");
        TestArchiveHelper.CreateTarGz(
            archivePath,
            [
                ("Main.exe", marker),
                ("MUnique.Client.Library.dll", "library"),
                ("config.ini", "ServerIP=archive"),
                ("shaders/basic.vert", "shader"),
            ]);
        return File.ReadAllBytes(archivePath);
    }

    private static byte[] CreateDataPackage(string directory, string marker, IEnumerable<string> additionalEntries)
    {
        var archivePath = Path.Combine(directory, $"data-{marker.Replace(" ", "-", StringComparison.Ordinal)}-{Guid.NewGuid():N}.tar.gz");
        var entries = new List<(string Name, string Content)>
        {
            ("Data/Dec2.dat", marker),
        };
        entries.AddRange(additionalEntries.Select(entry => (entry, "additional")));
        TestArchiveHelper.CreateTarGz(archivePath, entries);
        return File.ReadAllBytes(archivePath);
    }

    private static string CreateManifestJson(string baseUrl, string runtimeVersion, string dataId, byte[] runtimeBytes, byte[] dataBytes)
    {
        var manifest = new UpdateManifest
        {
            SchemaVersion = UpdateManifest.SupportedSchemaVersion,
            Channel = "stable",
            Runtime = new UpdateManifest.PackageInfo
            {
                Version = runtimeVersion,
                Archive = new UpdateManifest.ArchiveInfo
                {
                    Url = baseUrl + RuntimeFileName.TrimStart('/'),
                    Size = runtimeBytes.Length,
                    Sha256 = Hash(runtimeBytes),
                    Format = "tar.gz",
                },
            },
            Data = new UpdateManifest.PackageInfo
            {
                Id = dataId,
                Archive = new UpdateManifest.ArchiveInfo
                {
                    Url = baseUrl + DataFileName.TrimStart('/'),
                    Size = dataBytes.Length,
                    Sha256 = Hash(dataBytes),
                    Format = "tar.gz",
                },
            },
        };
        return JsonSerializer.Serialize(manifest, LauncherJson.Options);
    }

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
