// <copyright file="HeadlessUpdateRunner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Threading;

/// <summary>
/// Updates the client without showing a user interface. It is used for automated tests and
/// for pre-installing the client on a machine.
/// </summary>
internal static class HeadlessUpdateRunner
{
    /// <summary>
    /// Runs the update with the specified options.
    /// </summary>
    /// <param name="options">The command line options.</param>
    /// <returns>The exit code: 0 on success, 1 on failure.</returns>
    internal static int Run(LauncherCommandLine options)
    {
        var settings = new LauncherSettings
        {
            ManifestUrl = options.ManifestUrl,
            InstallDirectory = options.InstallDirectory,
        };

        var rootDirectory = options.DataDirectory ?? LauncherPaths.RootDirectory;
        LauncherPaths.EnsureCreated(rootDirectory);
        LauncherLog.Info($"Headless update started (install directory: {settings.InstallDirectory ?? LauncherPaths.DefaultInstallDirectory}).");

        using var service = new UpdateService(settings, rootDirectory);
        var progress = new Progress<UpdateProgress>(report => Console.WriteLine($"[{report.Stage}] {report.Message}"));
        try
        {
            var check = service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (!check.UpdateRequired)
            {
                Console.WriteLine($"The client is up to date (runtime {check.Manifest.Runtime.Version}, data {check.Manifest.Data.Id}).");
                return 0;
            }

            service.ApplyAsync(check, progress, CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine($"The client has been updated (runtime {check.Manifest.Runtime.Version}, data {check.Manifest.Data.Id}).");
            return 0;
        }
        catch (Exception ex)
        {
            LauncherLog.Error("The headless update failed.", ex);
            Console.Error.WriteLine($"The update failed: {ex.Message}");
            return 1;
        }
    }
}
