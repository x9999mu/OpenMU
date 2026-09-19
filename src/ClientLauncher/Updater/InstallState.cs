// <copyright file="InstallState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;
using System.Text.Json;

/// <summary>
/// Stores which client versions are installed locally.
/// </summary>
internal sealed class InstallState
{
    /// <summary>
    /// The schema version which is used by this launcher.
    /// </summary>
    internal const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Gets or sets the schema version of the state file.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the release channel.
    /// </summary>
    public string Channel { get; set; } = "stable";

    /// <summary>
    /// Gets or sets the directory into which the client is installed.
    /// </summary>
    public string? InstallDirectory { get; set; }

    /// <summary>
    /// Gets or sets the installed package versions.
    /// </summary>
    public InstalledPackage Installed { get; set; } = new();

    /// <summary>
    /// Gets or sets the time of the last successful installation or update.
    /// </summary>
    public DateTimeOffset? InstalledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the version of the launcher which wrote the state.
    /// </summary>
    public string? LauncherVersion { get; set; }

    /// <summary>
    /// Gets or sets the time at which the client was started the last time.
    /// </summary>
    public DateTimeOffset? LastLaunchUtc { get; set; }

    /// <summary>
    /// Loads the state from the specified file.
    /// </summary>
    /// <param name="filePath">The path of the state file.</param>
    /// <returns>The loaded state, or a new state if the file does not exist or is not valid.</returns>
    internal static InstallState Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new InstallState();
            }

            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<InstallState>(json, LauncherJson.Options);
            if (state is null || state.SchemaVersion != CurrentSchemaVersion)
            {
                LauncherLog.Warn($"The state file '{filePath}' has an unsupported schema version and is reset.");
                return new InstallState();
            }

            return state;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            LauncherLog.Error($"Could not load the state file '{filePath}'.", ex);
            return new InstallState();
        }
    }

    /// <summary>
    /// Saves the state into the specified file, keeping a backup of the previous state.
    /// </summary>
    /// <param name="filePath">The path of the state file.</param>
    internal void Save(string filePath)
    {
        var json = JsonSerializer.Serialize(this, LauncherJson.Options);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        try
        {
            if (File.Exists(filePath))
            {
                File.Copy(filePath, filePath + ".bak", overwrite: true);
            }

            File.WriteAllText(filePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LauncherLog.Error($"Could not save the state file '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// The installed versions of the client packages.
    /// </summary>
    internal sealed class InstalledPackage
    {
        /// <summary>
        /// Gets or sets the installed runtime version.
        /// </summary>
        public string? RuntimeVersion { get; set; }

        /// <summary>
        /// Gets or sets the installed data id.
        /// </summary>
        public string? DataId { get; set; }

        /// <summary>
        /// Gets or sets the installed audio id.
        /// </summary>
        public string? AudioId { get; set; }
    }
}
