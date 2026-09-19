// <copyright file="LauncherPathsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;

/// <summary>
/// Tests for the <see cref="LauncherPaths"/>.
/// </summary>
internal sealed class LauncherPathsTests
{
    [Test]
    public void DefaultDirectories_MatchTheLayout()
    {
        if (LauncherPaths.IsPortable)
        {
            // Portable layout: client and launcher data live next to the launcher executable.
            Assert.That(LauncherPaths.DefaultInstallDirectory, Is.EqualTo(LauncherPaths.LauncherDirectory));
            Assert.That(
                LauncherPaths.RootDirectory,
                Is.EqualTo(Path.Combine(LauncherPaths.LauncherDirectory, LauncherPaths.LauncherDataDirectoryName)));
        }
        else
        {
            Assert.That(LauncherPaths.DefaultInstallDirectory, Does.Contain("MuOnline"));
            Assert.That(LauncherPaths.RootDirectory, Does.Contain("MuOnline"));
        }
    }
}
