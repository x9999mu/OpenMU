// <copyright file="SelfUpdater.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Diagnostics;
using System.IO;
using System.Threading;

/// <summary>
/// Replaces the launcher executable by itself when a new version has been downloaded.
/// </summary>
internal static class SelfUpdater
{
    private const string ApplySwitch = "--apply-self-update";
    private const string TargetOption = "--target";
    private const string WaitPidOption = "--wait-pid";
    private const string NewExecutableName = "MuMainLauncher.new.exe";

    /// <summary>
    /// Gets the path into which a new launcher version is downloaded.
    /// </summary>
    internal static string NewExecutablePath =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, NewExecutableName);

    /// <summary>
    /// Handles the command line of a self-updating launcher process.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns><c>true</c> if this process only performs the self update and should exit afterwards.</returns>
    internal static bool TryHandleCommandLine(IEnumerable<string> args)
    {
        var arguments = args.ToList();
        if (!arguments.Contains(ApplySwitch, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var targetPath = GetOption(arguments, TargetOption);
        var processIdText = GetOption(arguments, WaitPidOption);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            LauncherLog.Error("The self update is missing the target executable.");
            return true;
        }

        if (int.TryParse(processIdText, out var processId))
        {
            WaitForProcessExit(processId);
        }

        ApplyUpdate(targetPath);
        TryStartTarget(targetPath);
        return true;
    }

    /// <summary>
    /// Deletes a leftover launcher executable of a previous self update, if possible.
    /// </summary>
    internal static void CleanupStaleFiles()
    {
        try
        {
            if (File.Exists(NewExecutablePath)
                && !string.Equals(Path.GetFullPath(NewExecutablePath), Path.GetFullPath(Environment.ProcessPath ?? string.Empty), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(NewExecutablePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LauncherLog.Warn($"Could not delete '{NewExecutableName}': {ex.Message}");
        }
    }

    private static string? GetOption(IReadOnlyList<string> arguments, string option)
    {
        for (var i = 0; i < arguments.Count - 1; i++)
        {
            if (string.Equals(arguments[i], option, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[i + 1];
            }
        }

        return null;
    }

    private static void WaitForProcessExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit(60_000))
            {
                LauncherLog.Warn("The running launcher did not exit within 60 seconds.");
            }
        }
        catch (ArgumentException)
        {
            // The process already exited.
        }
    }

    private static void ApplyUpdate(string targetPath)
    {
        var sourcePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            LauncherLog.Error("The path of the running launcher could not be determined.");
            return;
        }

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                File.Copy(sourcePath, targetPath, overwrite: true);
                LauncherLog.Info($"The launcher was updated at {targetPath}.");
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LauncherLog.Warn($"Attempt {attempt} to update '{targetPath}' failed: {ex.Message}");
                Thread.Sleep(1000);
            }
        }

        LauncherLog.Error($"Could not update the launcher executable at '{targetPath}'.");
    }

    private static void TryStartTarget(string targetPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(targetPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(targetPath),
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            LauncherLog.Error($"Could not start the updated launcher '{targetPath}'.", ex);
        }
    }
}
