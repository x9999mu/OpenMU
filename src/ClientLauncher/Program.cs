// <copyright file="Program.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Windows.Forms;

/// <summary>
/// The static main program.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    [STAThread]
    internal static void Main(string[] args)
    {
        if (SelfUpdater.TryHandleCommandLine(args))
        {
            return;
        }

        var options = LauncherCommandLine.Parse(args);
        if (options.Cleanup)
        {
            var freedBytes = CacheCleaner.Clean(options.DataDirectory ?? LauncherPaths.RootDirectory);
            Console.WriteLine($"Freed {freedBytes / (1024.0 * 1024.0):0.0} MB.");
            return;
        }

        if (options.UpdateOnly)
        {
            Environment.ExitCode = HeadlessUpdateRunner.Run(options);
            return;
        }

        SelfUpdater.CleanupStaleFiles();
        LauncherPaths.EnsureCreated();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(options));
    }
}
