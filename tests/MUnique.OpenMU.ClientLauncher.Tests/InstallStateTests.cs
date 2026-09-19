// <copyright file="InstallStateTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;

/// <summary>
/// Tests for the <see cref="InstallState"/>.
/// </summary>
internal sealed class InstallStateTests
{
    [Test]
    public void SaveAndLoad_RestoresValues()
    {
        using var directory = new TestDirectoryHelper();
        var stateFilePath = Path.Combine(directory.Path, "launcher-state.json");
        var state = new InstallState
        {
            Channel = "stable",
            InstallDirectory = Path.Combine(directory.Path, "client"),
            Installed = new InstallState.InstalledPackage
            {
                RuntimeVersion = "1.2.3",
                DataId = "abc123",
            },
        };

        state.Save(stateFilePath);
        var loaded = InstallState.Load(stateFilePath);

        Assert.That(loaded.Installed.RuntimeVersion, Is.EqualTo("1.2.3"));
        Assert.That(loaded.Installed.DataId, Is.EqualTo("abc123"));
        Assert.That(loaded.InstallDirectory, Is.EqualTo(Path.Combine(directory.Path, "client")));
    }

    [Test]
    public void Save_WithExistingFile_KeepsBackup()
    {
        using var directory = new TestDirectoryHelper();
        var stateFilePath = Path.Combine(directory.Path, "launcher-state.json");
        var state = new InstallState();
        state.Save(stateFilePath);
        state.Installed.RuntimeVersion = "2.0.0";

        state.Save(stateFilePath);

        Assert.That(File.Exists(stateFilePath + ".bak"), Is.True);
        Assert.That(InstallState.Load(stateFilePath + ".bak").Installed.RuntimeVersion, Is.Null.Or.Empty);
    }

    [Test]
    public void Load_WithBrokenJson_ReturnsNewState()
    {
        using var directory = new TestDirectoryHelper();
        var stateFilePath = Path.Combine(directory.Path, "launcher-state.json");
        File.WriteAllText(stateFilePath, "{ not json");

        var loaded = InstallState.Load(stateFilePath);

        Assert.That(loaded.Installed.RuntimeVersion, Is.Null);
    }

    [Test]
    public void Load_WithoutFile_ReturnsNewState()
    {
        using var directory = new TestDirectoryHelper();

        var loaded = InstallState.Load(Path.Combine(directory.Path, "missing.json"));

        Assert.That(loaded.Installed.DataId, Is.Null);
    }
}
