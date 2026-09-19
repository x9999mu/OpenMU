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

        SelfUpdater.CleanupStaleFiles();
        LauncherPaths.EnsureCreated();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
