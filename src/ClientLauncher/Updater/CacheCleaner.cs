// <copyright file="CacheCleaner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;

/// <summary>
/// Removes the files which the launcher accumulates over time: old archives, backups of previous
/// installations and the log file. The client installation and its state are kept.
/// </summary>
internal static class CacheCleaner
{
    private const int RuntimeArchivesToKeep = 2;

    /// <summary>
    /// Removes the accumulated files and returns the number of freed bytes.
    /// </summary>
    /// <param name="rootDirectory">The directory which contains the launcher data.</param>
    /// <param name="includeLog">A value indicating whether the log files should be removed, too.</param>
    /// <returns>The number of freed bytes.</returns>
    internal static long Clean(string rootDirectory, bool includeLog = true)
    {
        LauncherPaths.EnsureCreated(rootDirectory);
        long freedBytes = 0;

        freedBytes += DeleteDirectoryContents(LauncherPaths.GetBackupDirectory(rootDirectory));
        freedBytes += DeleteDirectoryContents(Path.Combine(LauncherPaths.GetStagingDirectory(rootDirectory), "current"));
        freedBytes += CleanCache(LauncherPaths.GetCacheDirectory(rootDirectory));
        if (includeLog)
        {
            freedBytes += DeleteLogFiles(rootDirectory);
        }

        LauncherLog.Info($"Cleanup removed {freedBytes} bytes.");
        return freedBytes;
    }

    private static long CleanCache(string cacheDirectory)
    {
        long freedBytes = 0;
        var files = new DirectoryInfo(cacheDirectory).EnumerateFiles().ToList();

        // The newest archive of each group is the one which is currently used; the previous
        // runtime archive is kept for a rollback, everything else can be downloaded again.
        foreach (var group in new[] { ("MuMain-data-", 1), ("MuMain-audio-", 1), ("MuMain-runtime-", RuntimeArchivesToKeep) })
        {
            var oldFiles = files
                .Where(file => file.Name.StartsWith(group.Item1, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(group.Item2);
            foreach (var oldFile in oldFiles)
            {
                freedBytes += DeleteFile(oldFile.FullName);
            }
        }

        foreach (var partFile in files.Where(file => file.Name.EndsWith(".part", StringComparison.OrdinalIgnoreCase)))
        {
            freedBytes += DeleteFile(partFile.FullName);
        }

        return freedBytes;
    }

    private static long DeleteDirectoryContents(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        long freedBytes = 0;
        foreach (var entryPath in Directory.EnumerateFileSystemEntries(directoryPath))
        {
            freedBytes += GetSize(entryPath);
            try
            {
                if (Directory.Exists(entryPath))
                {
                    Directory.Delete(entryPath, recursive: true);
                }
                else
                {
                    File.Delete(entryPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LauncherLog.Warn($"Could not delete '{entryPath}': {ex.Message}");
            }
        }

        return freedBytes;
    }

    private static long DeleteLogFiles(string rootDirectory)
    {
        long freedBytes = 0;
        foreach (var logFile in new[] { "launcher.log", "launcher.log.old" })
        {
            var logFilePath = Path.Combine(rootDirectory, logFile);
            if (File.Exists(logFilePath))
            {
                freedBytes += DeleteFile(logFilePath);
            }
        }

        return freedBytes;
    }

    private static long DeleteFile(string filePath)
    {
        try
        {
            var size = new FileInfo(filePath).Length;
            File.Delete(filePath);
            return size;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LauncherLog.Warn($"Could not delete '{filePath}': {ex.Message}");
            return 0;
        }
    }

    private static long GetSize(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }

            return new FileInfo(path).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
