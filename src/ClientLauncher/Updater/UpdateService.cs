// <copyright file="UpdateService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;

/// <summary>
/// Checks for client updates and installs them.
/// </summary>
internal sealed class UpdateService : IDisposable
{
    private const long RequiredFreeSpaceInBytes = 1200L * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ManifestClient _manifestClient;
    private readonly Downloader _downloader;
    private readonly LauncherSettings _settings;
    private readonly string _rootDirectory;
    private byte[]? _preservedConfigFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateService"/> class.
    /// </summary>
    /// <param name="settings">The launcher settings.</param>
    /// <param name="rootDirectory">The optional root directory for the launcher data; used by tests.</param>
    internal UpdateService(LauncherSettings settings, string? rootDirectory = null)
    {
        this._settings = settings;
        this._rootDirectory = rootDirectory ?? LauncherPaths.RootDirectory;
        this._httpClient = CreateHttpClient();
        this._manifestClient = new ManifestClient(this._httpClient);
        this._downloader = new Downloader(this._httpClient);
        this.State = InstallState.Load(this.StateFilePath);
    }

    /// <summary>
    /// Gets the version of the running launcher.
    /// </summary>
    internal static string LauncherVersion =>
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(UpdateService).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>
    /// Gets or sets the number of free bytes which must be available before an update is started.
    /// </summary>
    internal long RequiredFreeSpace { get; set; } = RequiredFreeSpaceInBytes;

    /// <summary>
    /// Gets the root directory of the launcher data.
    /// </summary>
    internal string RootDirectory => this._rootDirectory;

    /// <summary>
    /// Gets the path of the state file.
    /// </summary>
    internal string StateFilePath => LauncherPaths.GetStateFilePath(this._rootDirectory);

    /// <summary>
    /// Gets the local installation state.
    /// </summary>
    internal InstallState State { get; }

    /// <summary>
    /// Gets the directory into which the client is installed.
    /// </summary>
    internal string InstallDirectory =>
        string.IsNullOrWhiteSpace(this._settings.InstallDirectory)
            ? LauncherPaths.DefaultInstallDirectory
            : this._settings.InstallDirectory!;

    /// <summary>
    /// Gets the path of the main executable of the client.
    /// </summary>
    internal string MainExePath => Path.Combine(this.InstallDirectory, "Main.exe");

    /// <summary>
    /// Gets a value indicating whether the client is installed locally.
    /// </summary>
    internal bool IsClientInstalled => File.Exists(this.MainExePath);

    /// <summary>
    /// Gets the url of the update manifest.
    /// </summary>
    internal string ManifestUrl =>
        string.IsNullOrWhiteSpace(this._settings.ManifestUrl)
            ? LauncherSettings.DefaultManifestUrl
            : this._settings.ManifestUrl!;

    /// <inheritdoc />
    public void Dispose()
    {
        this._httpClient.Dispose();
    }

    /// <summary>
    /// Loads the manifest and checks which parts of the client need to be updated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the check.</returns>
    internal async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var manifest = await this._manifestClient.GetManifestAsync(this.ManifestUrl, cancellationToken).ConfigureAwait(false);
        var needsRuntime = !string.Equals(this.State.Installed.RuntimeVersion, manifest.Runtime.Version, StringComparison.OrdinalIgnoreCase);
        var needsData = !string.Equals(this.State.Installed.DataId, manifest.Data.Id, StringComparison.OrdinalIgnoreCase);
        var needsAudio = manifest.Audio is not null
            && !string.Equals(this.State.Installed.AudioId, manifest.Audio.Id, StringComparison.OrdinalIgnoreCase);
        if (!this.IsClientInstalled)
        {
            needsRuntime = true;
            needsData = true;
        }

        var result = new UpdateCheckResult(manifest, needsRuntime, needsData, needsAudio, this.IsClientInstalled);
        LauncherLog.Info($"Update check: runtime={result.NeedsRuntime}, data={result.NeedsData}, audio={result.NeedsAudio}, installed={result.IsClientInstalled}.");
        return result;
    }

    /// <summary>
    /// Removes the current installation, keeping the configuration file of the player.
    /// </summary>
    internal void PrepareCleanInstall()
    {
        this.CleanupBackups();

        var configFilePath = Path.Combine(this.InstallDirectory, "config.ini");
        this._preservedConfigFile = File.Exists(configFilePath) ? File.ReadAllBytes(configFilePath) : null;

        if (!Directory.Exists(this.InstallDirectory))
        {
            return;
        }

        var backupDirectory = Path.Combine(LauncherPaths.GetBackupDirectory(this._rootDirectory), DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(backupDirectory);

        // Files of the launcher itself are never removed; everything else is moved out of the
        // way, so the client can be installed from scratch.
        foreach (var entryPath in Directory.EnumerateFileSystemEntries(this.InstallDirectory))
        {
            var name = Path.GetFileName(entryPath);
            if (IsLauncherFile(name) || string.Equals(name, "config.ini", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetPath = Path.Combine(backupDirectory, name);
            try
            {
                if (Directory.Exists(entryPath))
                {
                    Directory.Move(entryPath, targetPath);
                }
                else
                {
                    File.Move(entryPath, targetPath);
                }
            }
            catch (IOException ex)
            {
                LauncherLog.Warn($"Could not move '{entryPath}' to the backup, it will be deleted: {ex.Message}");
                if (Directory.Exists(entryPath))
                {
                    Directory.Delete(entryPath, recursive: true);
                }
                else
                {
                    File.Delete(entryPath);
                }
            }
        }

        LauncherLog.Info($"Moved the previous installation to {backupDirectory}.");
    }

    /// <summary>
    /// Removes all but the newest backup of a previous installation.
    /// </summary>
    private void CleanupBackups()
    {
        var backupRoot = LauncherPaths.GetBackupDirectory(this._rootDirectory);
        try
        {
            if (!Directory.Exists(backupRoot))
            {
                return;
            }

            var oldBackups = new DirectoryInfo(backupRoot)
                .EnumerateDirectories()
                .OrderByDescending(directory => directory.Name, StringComparer.Ordinal)
                .Skip(1)
                .ToList();
            foreach (var oldBackup in oldBackups)
            {
                LauncherLog.Info($"Removing the old backup {oldBackup.FullName}.");
                oldBackup.Delete(recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LauncherLog.Warn($"Could not remove the old backups: {ex.Message}");
        }
    }

    private static bool IsLauncherFile(string name)
    {
        if (string.Equals(name, LauncherPaths.LauncherDataDirectoryName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "launcher.config", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var processName = Path.GetFileName(Environment.ProcessPath ?? string.Empty);
        if (!string.IsNullOrEmpty(processName) && string.Equals(name, processName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return name.StartsWith("MuMainLauncher", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("MUnique.OpenMU.ClientLauncher", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Downloads and applies the required archives.
    /// </summary>
    /// <param name="check">The result of the update check.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the game client is running or there is not enough disk space.</exception>
    internal async Task ApplyAsync(UpdateCheckResult check, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        LauncherPaths.EnsureCreated(this._rootDirectory);
        var manifest = check.Manifest;
        var stagingRoot = Path.Combine(LauncherPaths.GetStagingDirectory(this._rootDirectory), "current");
        DeleteDirectory(stagingRoot);
        Directory.CreateDirectory(stagingRoot);

        var cacheDirectory = LauncherPaths.GetCacheDirectory(this._rootDirectory);
        var runtimeArchive = Path.Combine(cacheDirectory, $"MuMain-runtime-{Sanitize(manifest.Runtime.Version!)}.{GetExtension(manifest.Runtime.Archive!)}");
        var dataArchive = Path.Combine(cacheDirectory, $"MuMain-data-{Sanitize(manifest.Data.Id!)}.{GetExtension(manifest.Data.Archive!)}");
        var audioPackage = manifest.Audio;
        var audioArchive = audioPackage is null
            ? null
            : Path.Combine(cacheDirectory, $"MuMain-audio-{Sanitize(audioPackage.Id!)}.{GetExtension(audioPackage.Archive!)}");
        var totalDownloadSize = (check.NeedsRuntime ? manifest.Runtime.Archive!.Size : 0)
                                + (check.NeedsData ? manifest.Data.Archive!.Size : 0)
                                + (check.NeedsAudio && audioPackage is not null ? audioPackage.Archive!.Size : 0);
        this.EnsureFreeSpace(totalDownloadSize);

        long downloadedBytes = 0;
        void ReportDownload(DownloadProgress downloadProgress)
        {
            var total = downloadedBytes + downloadProgress.BytesReceived;
            var fraction = totalDownloadSize > 0 ? 0.75 * Math.Clamp((double)total / totalDownloadSize, 0, 1) : 0;
            var message = string.Create(
                CultureInfo.InvariantCulture,
                $"Downloading {downloadProgress.FileName}: {FormatBytes(downloadProgress.BytesReceived)} of {FormatBytes(downloadProgress.TotalBytes)} ({FormatBytes((long)downloadProgress.BytesPerSecond)}/s)");
            progress?.Report(new UpdateProgress(UpdateStage.Downloading, message, fraction));
        }

        var runtimePayload = Path.Combine(stagingRoot, "runtime");
        if (check.NeedsRuntime)
        {
            progress?.Report(new UpdateProgress(UpdateStage.Downloading, "Downloading the client...", 0));
            await this._downloader.DownloadAsync(manifest.Runtime.Archive!.Url, runtimeArchive, manifest.Runtime.Archive.Size, manifest.Runtime.Archive.Sha256, new Progress<DownloadProgress>(ReportDownload), cancellationToken).ConfigureAwait(false);
            downloadedBytes += manifest.Runtime.Archive.Size;

            progress?.Report(new UpdateProgress(UpdateStage.Extracting, "Extracting the client...", 0.78));
            await Task.Run(() => ArchiveExtractor.Extract(runtimeArchive, runtimePayload, manifest.Runtime.Archive.Format, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        var dataPayload = Path.Combine(stagingRoot, "data");
        if (check.NeedsData)
        {
            progress?.Report(new UpdateProgress(UpdateStage.Downloading, "Downloading the game data...", downloadedBytes / (double)Math.Max(totalDownloadSize, 1)));
            await this._downloader.DownloadAsync(manifest.Data.Archive!.Url, dataArchive, manifest.Data.Archive.Size, manifest.Data.Archive.Sha256, new Progress<DownloadProgress>(ReportDownload), cancellationToken).ConfigureAwait(false);
            downloadedBytes += manifest.Data.Archive.Size;

            progress?.Report(new UpdateProgress(UpdateStage.Extracting, "Extracting the game data...", 0.84));
            await Task.Run(() => ArchiveExtractor.Extract(dataArchive, dataPayload, manifest.Data.Archive.Format, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        var audioPayload = Path.Combine(stagingRoot, "audio");
        if (check.NeedsAudio && audioPackage?.Archive is { } audioArchiveInfo && audioArchive is not null)
        {
            progress?.Report(new UpdateProgress(UpdateStage.Downloading, "Downloading the game audio...", downloadedBytes / (double)Math.Max(totalDownloadSize, 1)));
            await this._downloader.DownloadAsync(audioArchiveInfo.Url, audioArchive, audioArchiveInfo.Size, audioArchiveInfo.Sha256, new Progress<DownloadProgress>(ReportDownload), cancellationToken).ConfigureAwait(false);
            downloadedBytes += audioArchiveInfo.Size;

            progress?.Report(new UpdateProgress(UpdateStage.Extracting, "Extracting the game audio...", 0.87));
            await Task.Run(() => ArchiveExtractor.Extract(audioArchive, audioPayload, audioArchiveInfo.Format, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        if (this.IsGameRunning())
        {
            throw new InvalidOperationException("The game client is currently running. Please close it before updating.");
        }

        progress?.Report(new UpdateProgress(UpdateStage.Applying, "Installing files...", 0.9));
        var preserve = manifest.Preserve.Count > 0 ? manifest.Preserve : ["config.ini"];
        await Task.Run(
            () =>
            {
                if (check.NeedsRuntime)
                {
                    ApplyDirectory(runtimePayload, this.InstallDirectory, preserve, cancellationToken);
                }

                if (check.NeedsData)
                {
                    ApplyDirectory(dataPayload, this.InstallDirectory, preserve, cancellationToken);
                }

                if (check.NeedsAudio)
                {
                    ApplyDirectory(audioPayload, this.InstallDirectory, preserve, cancellationToken);
                }

                if (this._preservedConfigFile is { } preservedConfig)
                {
                    File.WriteAllBytes(Path.Combine(this.InstallDirectory, "config.ini"), preservedConfig);
                    this._preservedConfigFile = null;
                }
            },
            cancellationToken).ConfigureAwait(false);

        this.State.Channel = manifest.Channel;
        this.State.InstallDirectory = this.InstallDirectory;
        this.State.Installed.RuntimeVersion = manifest.Runtime.Version;
        this.State.Installed.DataId = manifest.Data.Id;
        if (audioPackage is not null)
        {
            this.State.Installed.AudioId = audioPackage.Id;
        }

        this.State.InstalledAtUtc = DateTimeOffset.UtcNow;
        this.State.LauncherVersion = LauncherVersion;
        this.State.Save(this.StateFilePath);

        DeleteDirectory(stagingRoot);
        this.CleanupCache(runtimeArchive, dataArchive, audioArchive);
        progress?.Report(new UpdateProgress(UpdateStage.Completed, "The client is up to date.", 1));
        LauncherLog.Info($"Update applied: runtime {manifest.Runtime.Version}, data {manifest.Data.Id}.");
    }

    /// <summary>
    /// Counts a finished update run and cleans up the accumulated files after the configured
    /// number of runs. Every run counts, also the automatic check when the launcher is opened
    /// without anything to download.
    /// The counter is stored in the state file, so it adds up over the whole usage time of the
    /// launcher and is not reset when it is closed and opened again.
    /// </summary>
    internal void RegisterUpdateRun()
    {
        this.State.UpdateCount++;
        if (this._settings.CleanupAfterUpdates > 0 && this.State.UpdateCount >= this._settings.CleanupAfterUpdates)
        {
            LauncherLog.Info($"Running the periodic cleanup after {this.State.UpdateCount} update runs.");
            CacheCleaner.Clean(this._rootDirectory, includeLog: false);
            this.State.UpdateCount = 0;
        }

        this.State.Save(this.StateFilePath);
    }

    /// <summary>
    /// Remembers that the client was started.
    /// </summary>
    internal void RememberLaunch()
    {
        this.State.LastLaunchUtc = DateTimeOffset.UtcNow;
        this.State.Save(this.StateFilePath);
    }

    /// <summary>
    /// Downloads and starts a new launcher version if the manifest contains one.
    /// </summary>
    /// <param name="manifest">The manifest.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if a new launcher version has been started and this process should exit.</returns>
    internal async Task<bool> TrySelfUpdateAsync(UpdateManifest manifest, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        var launcher = manifest.Launcher;
        var targetPath = Environment.ProcessPath;
        if (launcher is null
            || string.IsNullOrWhiteSpace(launcher.Url)
            || targetPath is null
            || !IsNewerVersion(launcher.Version, LauncherVersion))
        {
            return false;
        }

        var newExecutablePath = SelfUpdater.NewExecutablePath;
        progress?.Report(new UpdateProgress(UpdateStage.Downloading, "Downloading a new launcher version...", 0));
        await this._downloader.DownloadAsync(launcher.Url, newExecutablePath, launcher.Size, launcher.Sha256, null, cancellationToken).ConfigureAwait(false);

        var arguments = $"--apply-self-update --target \"{targetPath}\" --wait-pid {Environment.ProcessId}";
        LauncherLog.Info($"Starting the self update to version {launcher.Version}.");
        Process.Start(new ProcessStartInfo(newExecutablePath)
        {
            UseShellExecute = true,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(newExecutablePath),
        });
        return true;
    }

    private static bool IsNewerVersion(string candidate, string current)
    {
        static Version? Parse(string value)
        {
            var cleaned = value.Split('+')[0].Split('-')[0];
            return Version.TryParse(cleaned, out var version) ? version : null;
        }

        var candidateVersion = Parse(candidate);
        var currentVersion = Parse(current);
        if (candidateVersion is null || currentVersion is null)
        {
            return !string.Equals(candidate, current, StringComparison.OrdinalIgnoreCase);
        }

        return candidateVersion > currentVersion;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
        };

        var client = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MuMainLauncher/1.0");
        return client;
    }

    private static string GetExtension(UpdateManifest.ArchiveInfo archive) =>
        string.Equals(archive.Format, "zip", StringComparison.OrdinalIgnoreCase) ? "zip" : "tar.gz";

    private static string Sanitize(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value:0.#} {units[unit]}");
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException ex)
        {
            LauncherLog.Warn($"Could not delete the directory '{path}': {ex.Message}");
        }
    }

    private static void ApplyDirectory(
        string sourceDirectory,
        string targetDirectory,
        ICollection<string> preserve,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(targetDirectory);
        foreach (var sourceFilePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFilePath);
            var targetFilePath = Path.Combine(targetDirectory, relativePath);
            if (preserve.Contains(relativePath, StringComparer.OrdinalIgnoreCase) && File.Exists(targetFilePath))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);
            ReplaceFile(sourceFilePath, targetFilePath);
        }
    }

    private static void ReplaceFile(string sourceFilePath, string targetFilePath)
    {
        if (File.Exists(targetFilePath))
        {
            var attributes = File.GetAttributes(targetFilePath);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(targetFilePath, attributes & ~FileAttributes.ReadOnly);
            }

            try
            {
                File.Replace(sourceFilePath, targetFilePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
                return;
            }
            catch (IOException)
            {
                // Fall back to a plain copy below.
            }
            catch (PlatformNotSupportedException)
            {
                // Fall back to a plain copy below.
            }
        }

        File.Copy(sourceFilePath, targetFilePath, overwrite: true);
    }

    private void EnsureFreeSpace(long requiredBytes)
    {
        if (requiredBytes <= 0)
        {
            return;
        }

        var root = Path.GetPathRoot(Path.GetFullPath(this.InstallDirectory));
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var drive = new DriveInfo(root);
        var required = requiredBytes + (this.RequiredFreeSpace / 2);
        if (drive.AvailableFreeSpace < required)
        {
            throw new InvalidOperationException(
                $"Not enough free disk space on drive {drive.Name}: {FormatBytes(drive.AvailableFreeSpace)} available, about {FormatBytes(required)} required.");
        }
    }

    private bool IsGameRunning()
    {
        if (!File.Exists(this.MainExePath))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(this.MainExePath, FileMode.Open, FileAccess.Write, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            LauncherLog.Warn($"Could not acquire write access to '{this.MainExePath}'; it is assumed to be in use.");
            return true;
        }
    }

    private void CleanupCache(string currentRuntimeArchive, string currentDataArchive, string? currentAudioArchive)
    {
        try
        {
            var cacheFiles = Directory.EnumerateFiles(LauncherPaths.GetCacheDirectory(this._rootDirectory))
                .Where(file => !file.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var dataFile in cacheFiles.Where(file => Path.GetFileName(file).StartsWith("MuMain-data-", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(dataFile, currentDataArchive, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(dataFile);
                }
            }

            foreach (var audioFile in cacheFiles.Where(file => Path.GetFileName(file).StartsWith("MuMain-audio-", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(audioFile, currentAudioArchive, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(audioFile);
                }
            }

            var runtimeFiles = cacheFiles
                .Where(file => Path.GetFileName(file).StartsWith("MuMain-runtime-", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
            foreach (var oldRuntimeFile in runtimeFiles.Skip(2))
            {
                File.Delete(oldRuntimeFile);
            }
        }
        catch (IOException ex)
        {
            LauncherLog.Warn($"Could not clean up the cache: {ex.Message}");
        }
    }
}
