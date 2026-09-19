// <copyright file="LauncherPaths.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;

/// <summary>
/// Provides the local directories and files which are used by the launcher.
/// </summary>
internal static class LauncherPaths
{
    private const string ClientDirectoryName = "client";
    private const string StateFileName = "launcher-state.json";
    private const string LogFileName = "launcher.log";
    private const string CacheDirectoryName = ".cache";
    private const string StagingDirectoryName = ".staging";
    private const string BackupDirectoryName = ".backup";

    /// <summary>
    /// Gets the root directory of the launcher data, usually <c>%LOCALAPPDATA%\MuOnline</c>.
    /// </summary>
    internal static string RootDirectory { get; } = DetermineRootDirectory();

    /// <summary>
    /// Gets the default directory into which the game client is installed.
    /// </summary>
    internal static string DefaultInstallDirectory => Path.Combine(RootDirectory, ClientDirectoryName);

    /// <summary>
    /// Gets the path of the file which stores the installed versions.
    /// </summary>
    internal static string StateFilePath => GetStateFilePath(RootDirectory);

    /// <summary>
    /// Gets the path of the launcher log file.
    /// </summary>
    internal static string LogFilePath => Path.Combine(RootDirectory, LogFileName);

    /// <summary>
    /// Gets the directory which keeps verified archives for re-installations.
    /// </summary>
    internal static string CacheDirectory => GetCacheDirectory(RootDirectory);

    /// <summary>
    /// Gets the directory which is used for downloads and extracted files.
    /// </summary>
    internal static string StagingDirectory => GetStagingDirectory(RootDirectory);

    /// <summary>
    /// Gets the directory which keeps previous installations.
    /// </summary>
    internal static string BackupDirectory => GetBackupDirectory(RootDirectory);

    /// <summary>
    /// Gets the path of the state file for the specified root directory.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <returns>The path of the state file.</returns>
    internal static string GetStateFilePath(string rootDirectory) => Path.Combine(rootDirectory, StateFileName);

    /// <summary>
    /// Gets the cache directory for the specified root directory.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <returns>The path of the cache directory.</returns>
    internal static string GetCacheDirectory(string rootDirectory) => Path.Combine(rootDirectory, CacheDirectoryName);

    /// <summary>
    /// Gets the staging directory for the specified root directory.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <returns>The path of the staging directory.</returns>
    internal static string GetStagingDirectory(string rootDirectory) => Path.Combine(rootDirectory, StagingDirectoryName);

    /// <summary>
    /// Gets the backup directory for the specified root directory.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <returns>The path of the backup directory.</returns>
    internal static string GetBackupDirectory(string rootDirectory) => Path.Combine(rootDirectory, BackupDirectoryName);

    /// <summary>
    /// Ensures that the launcher directories exist.
    /// </summary>
    internal static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(StagingDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }

    /// <summary>
    /// Ensures that the launcher directories of the specified root directory exist.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    internal static void EnsureCreated(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(GetCacheDirectory(rootDirectory));
        Directory.CreateDirectory(GetStagingDirectory(rootDirectory));
        Directory.CreateDirectory(GetBackupDirectory(rootDirectory));
    }

    private static string DetermineRootDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "MuOnline");
    }
}
