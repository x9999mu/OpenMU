// <copyright file="MainForm.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

#pragma warning disable CA1416 // This project is compiled for windows
#pragma warning disable VSTHRD111 // The continuations must run on the UI thread of the form.
namespace MUnique.OpenMU.ClientLauncher;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

/// <summary>
/// The main form of the launcher.
/// </summary>
public partial class MainForm : Form
{
    private const string AutoServerDescription = "x9999 (auto)";

    private LauncherSettings _settings = new();
    private BindingList<ServerHostSettings> _hostsBindingList = new();
    private readonly LauncherCommandLine _commandLine;
    private string? _persistedMainExePath;
    private string? _persistedInstallDirectory;
    private string? _persistedManifestUrl;
    private UpdateService? _updateService;
    private bool _isBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
        : this(new LauncherCommandLine())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    /// <param name="commandLine">The command line options of the launcher.</param>
    internal MainForm(LauncherCommandLine commandLine)
    {
        this._commandLine = commandLine;
        this.InitializeComponent();
        this.LoadOptions();
        this._updateService = new UpdateService(this._settings, commandLine.DataDirectory);
        this.UpdateButtonStates();
        this.Shown += this.OnFormShown;
    }

    private BindingList<ServerHostSettings> Hosts
    {
        get => this._hostsBindingList;
        set
        {
            this._hostsBindingList = value;
            this._serversComboBox.DataSource = value;
        }
    }

    private BindingList<ClientResolution> Resolutions { get; set; } = new(LauncherSettings.DefaultResolutions);

    private static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "launcher.config");

    /// <inheritdoc />
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        this._updateService?.Dispose();
        this._updateService = null;
        base.OnFormClosed(e);
    }

    /// <summary>
    /// Launches the MU Online client (Main.exe) to connect to the configured address.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Catching all Exceptions.")]
    private async void LaunchClick(object sender, EventArgs e)
    {
        if (this._isBusy)
        {
            return;
        }

        try
        {
            if (this._serversComboBox.SelectedItem is not ServerHostSettings selectedHost)
            {
                MessageBox.Show("Please select a server.", "MU Game Client Launcher");
                return;
            }

            this._isBusy = true;
            this.UpdateButtonStates();
            try
            {
                if (!await this.EnsureUpToDateAsync())
                {
                    return;
                }

                this.StartClient(selectedHost);
            }
            finally
            {
                this._isBusy = false;
                this.UpdateButtonStates();
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Can't access Windows Registry. To use this option, run the launcher as Administrator.");
        }
        catch (Exception ex)
        {
            LauncherLog.Error("Could not start the game client.", ex);
            MessageBox.Show("Error starting MU. Path correct?" + Environment.NewLine + ex.Message);
        }
    }

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Catching all Exceptions.")]
    private async void OnFormShown(object? sender, EventArgs e)
    {
        try
        {
            var isUpToDate = await this.EnsureUpToDateAsync();
            if (!isUpToDate && !this._updateService!.IsClientInstalled)
            {
                this.SetStatus("The client is not installed yet.");
            }
        }
        catch (Exception ex)
        {
            LauncherLog.Error("The update check at startup failed.", ex);
            this.SetStatus("Could not check for updates.");
        }
    }

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Catching all Exceptions.")]
    private async void OnUpdateButtonClick(object sender, EventArgs e)
    {
        if (this._isBusy)
        {
            return;
        }

        try
        {
            this._isBusy = true;
            this.UpdateButtonStates();
            try
            {
                await this.EnsureUpToDateAsync();
            }
            finally
            {
                this._isBusy = false;
                this.UpdateButtonStates();
            }
        }
        catch (Exception ex)
        {
            LauncherLog.Error("The manual update check failed.", ex);
        }
    }

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Catching all Exceptions.")]
    private async void OnCleanInstallButtonClick(object sender, EventArgs e)
    {
        if (this._isBusy)
        {
            return;
        }

        var result = MessageBox.Show(
            "The client will be downloaded again and all local client files will be replaced." + Environment.NewLine +
            "Your configuration file (config.ini) is kept." + Environment.NewLine + Environment.NewLine +
            "Do you want to continue?",
            "Clean install",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            this._isBusy = true;
            this.UpdateButtonStates();
            try
            {
                this.SyncSettingsFromUi();
                this._updateService!.PrepareCleanInstall();
                await this.EnsureUpToDateAsync();
            }
            finally
            {
                this._isBusy = false;
                this.UpdateButtonStates();
            }
        }
        catch (Exception ex)
        {
            LauncherLog.Error("The clean install failed.", ex);
            MessageBox.Show("The clean install failed:" + Environment.NewLine + ex.Message);
        }
    }

    private void LoadOptions()
    {
        this._serversComboBox.DataSource = this.Hosts;
        this._settings = new LauncherSettings();
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var reader = new XmlSerializer(typeof(LauncherSettings));
                using var file = new StreamReader(ConfigFilePath);
                if (reader.Deserialize(file) is LauncherSettings launcherSettings)
                {
                    this._settings = launcherSettings;
                    this.Hosts = new BindingList<ServerHostSettings>(launcherSettings.Hosts);
                    this.Resolutions = launcherSettings.AvailableResolutions?.Any() is true
                        ? new BindingList<ClientResolution>(launcherSettings.AvailableResolutions)
                        : new BindingList<ClientResolution>(LauncherSettings.DefaultResolutions);
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Warn($"Could not read '{ConfigFilePath}': {ex.Message}");
                this.ResetHosts();
            }
        }
        else
        {
            this.ResetHosts();
        }

        if (this._settings.Hosts.Count == 0)
        {
            this.ResetHosts();
        }

        this._settings.ManifestUrl ??= LauncherSettings.DefaultManifestUrl;
        this._settings.Channel ??= "stable";
        if (this._settings.UseDefaultInstallDirectory || string.IsNullOrWhiteSpace(this._settings.InstallDirectory))
        {
            this._settings.InstallDirectory = LauncherPaths.DefaultInstallDirectory;
            this._settings.MainExePath = Path.Combine(LauncherPaths.DefaultInstallDirectory, "Main.exe");
        }

        this._persistedMainExePath = this._settings.MainExePath;
        this._persistedInstallDirectory = this._settings.InstallDirectory;
        this._persistedManifestUrl = this._settings.ManifestUrl;

        if (!string.IsNullOrWhiteSpace(this._commandLine.InstallDirectory))
        {
            this._settings.InstallDirectory = this._commandLine.InstallDirectory;
            this._settings.MainExePath = Path.Combine(this._commandLine.InstallDirectory!, "Main.exe");
        }

        if (!string.IsNullOrWhiteSpace(this._commandLine.ManifestUrl))
        {
            this._settings.ManifestUrl = this._commandLine.ManifestUrl;
        }

        this._settings.MainExePath ??= Path.Combine(this._settings.InstallDirectory, "Main.exe");
        this.MainExePathTextBox.Text = this._settings.MainExePath;
        this.SetStatus($"Launcher {UpdateService.LauncherVersion}");
    }

    private void ResetHosts()
    {
        this.Hosts = new BindingList<ServerHostSettings>
        {
            new() { Description = "Local ConnectServer", Address = "localhost", Port = 44405 },
            new() { Description = "Local GameServer 1", Address = "localhost", Port = 55901 },
        };
    }

    private void SaveCurrentOptions()
    {
        this.SyncSettingsFromUi();
        var mainExePath = this._settings.MainExePath;
        var installDirectory = this._settings.InstallDirectory;
        var manifestUrl = this._settings.ManifestUrl;
        if (!string.IsNullOrWhiteSpace(this._commandLine.InstallDirectory))
        {
            // Command line overrides are only valid for this run.
            this._settings.MainExePath = this._persistedMainExePath;
            this._settings.InstallDirectory = this._persistedInstallDirectory;
        }

        if (!string.IsNullOrWhiteSpace(this._commandLine.ManifestUrl))
        {
            this._settings.ManifestUrl = this._persistedManifestUrl;
        }

        try
        {
            var writer = new XmlSerializer(typeof(LauncherSettings));
            using var file = File.Create(ConfigFilePath);
            writer.Serialize(file, this._settings);
        }
        catch (Exception ex)
        {
            LauncherLog.Warn($"Could not save '{ConfigFilePath}': {ex.Message}");
        }
        finally
        {
            this._settings.MainExePath = mainExePath;
            this._settings.InstallDirectory = installDirectory;
            this._settings.ManifestUrl = manifestUrl;
        }
    }

    private void SyncSettingsFromUi()
    {
        this._settings.Hosts = this._hostsBindingList.ToList();
        this._settings.MainExePath = this.MainExePathTextBox.Text;
        if (!string.IsNullOrWhiteSpace(this._settings.MainExePath))
        {
            this._settings.InstallDirectory = Path.GetDirectoryName(this._settings.MainExePath);
            this._settings.UseDefaultInstallDirectory = IsDefaultInstallDirectory(this._settings.InstallDirectory);
        }

        this._settings.AvailableResolutions = this.Resolutions.ToList();
    }

    private static bool IsDefaultInstallDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return true;
        }

        return string.Equals(
            Path.TrimEndingDirectorySeparator(directory),
            Path.TrimEndingDirectorySeparator(LauncherPaths.DefaultInstallDirectory),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures that the client is installed and up to date.
    /// </summary>
    /// <returns><c>true</c> if the client can be started.</returns>
    private async Task<bool> EnsureUpToDateAsync()
    {
        this.SyncSettingsFromUi();
        var updateService = this._updateService!;

        UpdateCheckResult check;
        this.SetStatus("Checking for updates...");
        try
        {
            check = await updateService.CheckAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            LauncherLog.Warn($"Could not check for updates: {ex.Message}");
            this.SetStatus("Could not check for updates.");
            if (updateService.IsClientInstalled)
            {
                return true;
            }

            MessageBox.Show(
                "The client is not installed and the update information could not be loaded:" + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Please check your internet connection and try again.",
                "Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        this.ApplyServerSettings(check.Manifest);

        if (await this.TryRunSelfUpdateAsync(updateService, check.Manifest))
        {
            return false;
        }

        if (!check.UpdateRequired)
        {
            this.SetStatus($"Client is up to date (runtime {check.Manifest.Runtime.Version}, data {check.Manifest.Data.Id}).");
            return updateService.IsClientInstalled;
        }

        this.SetStatus(check.IsClientInstalled ? "Installing update..." : "Installing the game client...");
        var result = ProgressForm.Run(
            this,
            check.IsClientInstalled ? "Updating the game client" : "Installing the game client",
            (progress, cancellationToken) => updateService.ApplyAsync(check, progress, cancellationToken));

        if (result.Succeeded)
        {
            this.SetStatus($"Client is up to date (runtime {check.Manifest.Runtime.Version}, data {check.Manifest.Data.Id}).");
            return true;
        }

        if (result.Canceled)
        {
            this.SetStatus("The update was canceled.");
            return updateService.IsClientInstalled;
        }

        this.SetStatus("The update failed.");
        if (!updateService.IsClientInstalled)
        {
            MessageBox.Show(
                "The game client could not be installed:" + Environment.NewLine + result.ErrorMessage,
                "Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        var continueWithoutUpdate = MessageBox.Show(
            "The update failed:" + Environment.NewLine + result.ErrorMessage + Environment.NewLine + Environment.NewLine +
            "Do you want to start the installed version anyway?",
            "Update",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        return continueWithoutUpdate == DialogResult.Yes;
    }

    private async Task<bool> TryRunSelfUpdateAsync(UpdateService updateService, UpdateManifest manifest)
    {
        if (manifest.Launcher is null)
        {
            return false;
        }

        var selfUpdateStarted = false;
        var result = ProgressForm.Run(
            this,
            "Updating the launcher",
            async (progress, cancellationToken) =>
            {
                selfUpdateStarted = await updateService.TrySelfUpdateAsync(manifest, progress, cancellationToken);
            });

        if (selfUpdateStarted)
        {
            LauncherLog.Info("The launcher restarts itself for the self update.");
            this.Close();
            return true;
        }

        if (!result.Succeeded && !result.Canceled && result.ErrorMessage is not null)
        {
            LauncherLog.Warn($"The launcher update failed: {result.ErrorMessage}");
        }

        return false;
    }

    private void ApplyServerSettings(UpdateManifest manifest)
    {
        if (manifest.Server is not { } server || string.IsNullOrWhiteSpace(server.Host))
        {
            return;
        }

        var existing = this._hostsBindingList.FirstOrDefault(
            host => string.Equals(host.Description, AutoServerDescription, StringComparison.Ordinal));
        if (existing is null)
        {
            existing = new ServerHostSettings { Description = AutoServerDescription };
            this._hostsBindingList.Add(existing);
        }

        existing.Address = server.Host;
        existing.Port = server.Port > 0 ? server.Port : 44405;
        this._serversComboBox.SelectedItem = existing;
        this.SaveCurrentOptions();
    }

    private void StartClient(ServerHostSettings selectedHost)
    {
        this.SyncSettingsFromUi();
        var launcher = new Launcher
        {
            HostAddress = selectedHost.Address,
            HostPort = selectedHost.Port,
            MainExePath = this._settings.MainExePath,
            WriteConnectionToRegistry = this._settings.WriteConnectionToRegistry,
        };

        launcher.LaunchClient();
        this._updateService?.RememberLaunch();
        this.SaveCurrentOptions();
    }

    private void SearchMainExeButtonClick(object sender, EventArgs e)
    {
        var dialogResult = this.openFileDialog.ShowDialog(this);
        if (dialogResult == DialogResult.OK)
        {
            this.MainExePathTextBox.Text = this.openFileDialog.FileName;
            this.SyncSettingsFromUi();
        }
    }

    private void ConfigurationDialogButtonClick(object sender, EventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            using var configDialog = new ClientSettingsDialog();
            configDialog.Resolutions = this.Resolutions;
            configDialog.ShowDialog(this);
        }
        else
        {
            MessageBox.Show("Changing the configuration of the MU game client is only supported on windows.");
        }
    }

    private void OnAddHostButtonClick(object sender, EventArgs e)
    {
        using var dialog = new HostConfigurationDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var settings = dialog.Settings;
            this._hostsBindingList.Add(settings);
            this.SaveCurrentOptions();
            this.UpdateButtonStates();
        }
    }

    private void OnEditHostButtonClick(object sender, EventArgs e)
    {
        var selectedConfiguration = (ServerHostSettings)this._serversComboBox.SelectedItem!;
        using var dialog = new HostConfigurationDialog();
        dialog.Settings = selectedConfiguration;

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var editedConfiguration = dialog.Settings;
            selectedConfiguration.Port = editedConfiguration.Port;
            selectedConfiguration.Address = editedConfiguration.Address;
            selectedConfiguration.Description = editedConfiguration.Description;
            this.SaveCurrentOptions();
        }
    }

    private void OnRemoveHostButtonClick(object sender, EventArgs e)
    {
        this._hostsBindingList.RemoveAt(this._serversComboBox.SelectedIndex);
        this.SaveCurrentOptions();
    }

    private void OnServersComboBoxSelectedIndexChanged(object sender, EventArgs e)
    {
        this.UpdateButtonStates();
    }

    private void SetStatus(string message)
    {
        this._statusLabel.Text = message;
    }

    private void UpdateButtonStates()
    {
        var isServerSelected = this._serversComboBox.SelectedItem is ServerHostSettings;
        this._editHostButton.Enabled = isServerSelected && !this._isBusy;
        this._removeHostButton.Enabled = isServerSelected && !this._isBusy;
        this._launchButton.Enabled = isServerSelected && !this._isBusy;
        this._updateButton.Enabled = !this._isBusy;
        this._cleanInstallButton.Enabled = !this._isBusy;
    }
}
