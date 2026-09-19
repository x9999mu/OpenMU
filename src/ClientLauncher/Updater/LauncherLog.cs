// <copyright file="LauncherLog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;

/// <summary>
/// Writes diagnostic messages into a log file next to the launcher state.
/// </summary>
internal static class LauncherLog
{
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Writes an informational message.
    /// </summary>
    /// <param name="message">The message.</param>
    internal static void Info(string message) => Write("INFO ", message, null);

    /// <summary>
    /// Writes a warning.
    /// </summary>
    /// <param name="message">The message.</param>
    internal static void Warn(string message) => Write("WARN ", message, null);

    /// <summary>
    /// Writes an error, including the exception if there is one.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The optional exception.</param>
    internal static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} {level} {message}";
        if (exception is not null)
        {
            line = line + Environment.NewLine + exception;
        }

        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LauncherPaths.RootDirectory);
                File.AppendAllText(LauncherPaths.LogFilePath, line + Environment.NewLine);
            }
        }
        catch (Exception)
        {
            // Logging must never break the launcher.
        }
    }
}
