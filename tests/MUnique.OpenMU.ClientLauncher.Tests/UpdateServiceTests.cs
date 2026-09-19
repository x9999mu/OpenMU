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
    private const string AudioFileName = "/audio.tar.gz";

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
    public async Task CheckAndApply_InstallsAndUpdatesTheAudioPackage()
    {
        using var directory = new TestDirectoryHelper();
        using var server = new TestHttpServer();
        var installDirectory = Path.Combine(directory.Path, "client");
        var runtimeBytes = CreateRuntimePackage(directory.Path, "1.0.0", "runtime v1");
        var dataBytes = CreateDataPackage(directory.Path, "data v1", []);
        var audioBytes = CreateAudioPackage(directory.Path, "audio v1");
        server.AddFile(RuntimeFileName, runtimeBytes);
        server.AddFile(DataFileName, dataBytes);
        server.AddFile(AudioFileName, audioBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id1", runtimeBytes, dataBytes, ("audio1", audioBytes)));
        using var service = CreateService(directory.Path, installDirectory, server.BaseUrl);

        var check = await service.CheckAsync(CancellationToken.None);
        Assert.That(check.NeedsAudio, Is.True);

        await service.ApplyAsync(check, null, CancellationToken.None);

        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Data", "Sound", "test.wav")), Is.EqualTo("audio v1"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Data", "Music", "test.mp3")), Is.EqualTo("audio v1"));
        Assert.That(server.GetRequestCount(AudioFileName), Is.EqualTo(1));

        // Nothing changed, so nothing is downloaded again.
        var secondCheck = await service.CheckAsync(CancellationToken.None);
        Assert.That(secondCheck.UpdateRequired, Is.False);
        Assert.That(secondCheck.NeedsAudio, Is.False);
        Assert.That(server.GetRequestCount(AudioFileName), Is.EqualTo(1));

        // Only the audio package changed.
        var newAudioBytes = CreateAudioPackage(directory.Path, "audio v2");
        server.AddFile(AudioFileName, newAudioBytes);
        server.AddFile("/manifest.json", CreateManifestJson(server.BaseUrl, "1.0.0", "id1", runtimeBytes, dataBytes, ("audio2", newAudioBytes)));

        var thirdCheck = await service.CheckAsync(CancellationToken.None);
        Assert.That(thirdCheck.NeedsRuntime, Is.False);
        Assert.That(thirdCheck.NeedsData, Is.False);
        Assert.That(thirdCheck.NeedsAudio, Is.True);

        await service.ApplyAsync(thirdCheck, null, CancellationToken.None);

        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Data", "Sound", "test.wav")), Is.EqualTo("audio v2"));
        Assert.That(server.GetRequestCount(RuntimeFileName), Is.EqualTo(1));
        Assert.That(server.GetRequestCount(DataFileName), Is.EqualTo(1));
        Assert.That(server.GetRequestCount(AudioFileName), Is.EqualTo(2));
    }

    [Test]
    public void RegisterUpdateRun_CountsEveryRunOverSessionsAndCleansUp()
    {
        using var directory = new TestDirectoryHelper();
        var installDirectory = Path.Combine(directory.Path, "client");
        var settings = new LauncherSettings
        {
            InstallDirectory = installDirectory,
            CleanupAfterUpdates = 2,
        };
        using var service = new UpdateService(settings, directory.Path)
        {
            RequiredFreeSpace = 0,
        };

        // The automatic check at startup counts, too.
        service.RegisterUpdateRun();
        Assert.That(service.State.UpdateCount, Is.EqualTo(1));

        // The counter is stored in the state file, so it survives closing and reopening the launcher.
        using var restartedService = new UpdateService(settings, directory.Path)
        {
            RequiredFreeSpace = 0,
        };
        Assert.That(restartedService.State.UpdateCount, Is.EqualTo(1));

        var backupDirectory = LauncherPaths.GetBackupDirectory(directory.Path);
        var oldBackup = Path.Combine(backupDirectory, "20260101-000000");
        Directory.CreateDirectory(oldBackup);

        // The second update reaches the threshold and removes the accumulated files.
        restartedService.RegisterUpdateRun();

        Assert.That(restartedService.State.UpdateCount, Is.Zero);
        Assert.That(Directory.Exists(oldBackup), Is.False);
    }

    [Test]
    public async Task CleanInstall_RemovesTheOlderBackups()
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

        var backupDirectory = LauncherPaths.GetBackupDirectory(directory.Path);
        Directory.CreateDirectory(Path.Combine(backupDirectory, "20260101-000000"));
        Directory.CreateDirectory(Path.Combine(backupDirectory, "20260102-000000"));

        service.PrepareCleanInstall();

        Assert.That(Directory.Exists(Path.Combine(backupDirectory, "20260101-000000")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(backupDirectory, "20260102-000000")), Is.True);
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
        File.WriteAllText(Path.Combine(installDirectory, "launcher.config"), "launcher settings");
        Directory.CreateDirectory(Path.Combine(installDirectory, LauncherPaths.LauncherDataDirectoryName));
        File.WriteAllText(
            Path.Combine(installDirectory, LauncherPaths.LauncherDataDirectoryName, "launcher-state.json"),
            "{}");
        service.PrepareCleanInstall();
        var check = await service.CheckAsync(CancellationToken.None);
        Assert.That(check.IsClientInstalled, Is.False);

        await service.ApplyAsync(check, null, CancellationToken.None);

        Assert.That(File.Exists(Path.Combine(installDirectory, "leftover.txt")), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "Main.exe")), Is.EqualTo("runtime v1"));
        Assert.That(File.ReadAllText(Path.Combine(installDirectory, "config.ini")), Is.EqualTo("ServerIP=local"));
        Assert.That(File.Exists(Path.Combine(installDirectory, "launcher.config")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(installDirectory, LauncherPaths.LauncherDataDirectoryName)), Is.True);
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

    private static byte[] CreateAudioPackage(string directory, string marker)
    {
        var archivePath = Path.Combine(directory, $"audio-{marker.Replace(" ", "-", StringComparison.Ordinal)}-{Guid.NewGuid():N}.tar.gz");
        TestArchiveHelper.CreateTarGz(
            archivePath,
            [
                ("Data/Sound/test.wav", marker),
                ("Data/Music/test.mp3", marker),
            ]);
        return File.ReadAllBytes(archivePath);
    }

    private static string CreateManifestJson(
        string baseUrl,
        string runtimeVersion,
        string dataId,
        byte[] runtimeBytes,
        byte[] dataBytes,
        (string Id, byte[] Content)? audio = null)
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

        if (audio is { } audioPackage)
        {
            manifest.Audio = new UpdateManifest.PackageInfo
            {
                Id = audioPackage.Id,
                Archive = new UpdateManifest.ArchiveInfo
                {
                    Url = baseUrl + AudioFileName.TrimStart('/'),
                    Size = audioPackage.Content.Length,
                    Sha256 = Hash(audioPackage.Content),
                    Format = "tar.gz",
                },
            };
        }

        return JsonSerializer.Serialize(manifest, LauncherJson.Options);
    }

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
