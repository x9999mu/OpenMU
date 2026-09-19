// <copyright file="CacheCleanerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;

/// <summary>
/// Tests for the <see cref="CacheCleaner"/>.
/// </summary>
internal sealed class CacheCleanerTests
{
    [Test]
    public void Clean_KeepsTheCurrentArchivesAndRemovesTheRest()
    {
        using var directory = new TestDirectoryHelper();
        var cacheDirectory = LauncherPaths.GetCacheDirectory(directory.Path);
        var backupDirectory = LauncherPaths.GetBackupDirectory(directory.Path);
        var stagingDirectory = LauncherPaths.GetStagingDirectory(directory.Path);
        Directory.CreateDirectory(cacheDirectory);
        Directory.CreateDirectory(Path.Combine(backupDirectory, "20260101-000000"));
        Directory.CreateDirectory(Path.Combine(backupDirectory, "20260102-000000"));
        Directory.CreateDirectory(Path.Combine(stagingDirectory, "current"));

        CreateArchive(cacheDirectory, "MuMain-data-old.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-data-new.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-audio-old.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-audio-new.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-runtime-1.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-runtime-2.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-runtime-3.tar.gz");
        CreateArchive(cacheDirectory, "MuMain-data-part.tar.gz.part");
        File.WriteAllText(Path.Combine(stagingDirectory, "current", "leftover.txt"), "leftover");
        File.WriteAllText(Path.Combine(directory.Path, "launcher.log"), "some log entries");

        var freedBytes = CacheCleaner.Clean(directory.Path);

        Assert.That(freedBytes, Is.GreaterThan(0));
        Assert.That(Directory.GetFiles(cacheDirectory).Select(Path.GetFileName), Is.EquivalentTo(new[]
        {
            "MuMain-data-new.tar.gz",
            "MuMain-audio-new.tar.gz",
            "MuMain-runtime-2.tar.gz",
            "MuMain-runtime-3.tar.gz",
        }));
        Assert.That(Directory.Exists(Path.Combine(backupDirectory, "20260101-000000")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(backupDirectory, "20260102-000000")), Is.False);
        Assert.That(File.Exists(Path.Combine(stagingDirectory, "current", "leftover.txt")), Is.False);
        Assert.That(File.Exists(Path.Combine(directory.Path, "launcher.log")), Is.False);
    }

    private static void CreateArchive(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllBytes(filePath, new byte[1024]);

        // Give the newest archive of each group a newer timestamp.
        var isNewest = fileName.Contains("new", StringComparison.Ordinal)
                       || fileName.EndsWith("2.tar.gz", StringComparison.Ordinal)
                       || fileName.EndsWith("3.tar.gz", StringComparison.Ordinal);
        File.SetLastWriteTimeUtc(filePath, isNewest ? DateTime.UtcNow : DateTime.UtcNow.AddDays(-1));
    }
}
