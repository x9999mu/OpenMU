// <copyright file="LauncherCommandLine.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

/// <summary>
/// The command line options of the launcher.
/// </summary>
internal sealed class LauncherCommandLine
{
    /// <summary>
    /// Gets the url of the update manifest, if it has been specified.
    /// </summary>
    internal string? ManifestUrl { get; private set; }

    /// <summary>
    /// Gets the directory into which the client is installed, if it has been specified.
    /// </summary>
    internal string? InstallDirectory { get; private set; }

    /// <summary>
    /// Gets the directory which contains the launcher state, cache and log, if it has been specified.
    /// </summary>
    internal string? DataDirectory { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the launcher should only update the client and exit without a user interface.
    /// </summary>
    internal bool UpdateOnly { get; private set; }

    /// <summary>
    /// Parses the specified command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The parsed options.</returns>
    internal static LauncherCommandLine Parse(IReadOnlyList<string> args)
    {
        var options = new LauncherCommandLine();
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--manifest":
                    options.ManifestUrl = GetValue(args, ref i);
                    break;
                case "--install-dir":
                    options.InstallDirectory = GetValue(args, ref i);
                    break;
                case "--data-dir":
                    options.DataDirectory = GetValue(args, ref i);
                    break;
                case "--update-only":
                case "--silent":
                    options.UpdateOnly = true;
                    break;
                default:
                    break;
            }
        }

        return options;
    }

    private static string? GetValue(IReadOnlyList<string> args, ref int index)
    {
        if (index + 1 >= args.Count)
        {
            return null;
        }

        index++;
        return args[index];
    }
}
