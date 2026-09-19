// <copyright file="ArchiveExtractorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;

/// <summary>
/// Tests for the <see cref="ArchiveExtractor"/>.
/// </summary>
internal sealed class ArchiveExtractorTests
{
    [Test]
    public void Extract_WithTarGz_CreatesAllFiles()
    {
        using var directory = new TestDirectoryHelper();
        var archivePath = Path.Combine(directory.Path, "archive.tar.gz");
        TestArchiveHelper.CreateTarGz(
            archivePath,
            [
                ("Main.exe", "runtime"),
                ("Data/Dec2.dat", "data"),
                ("fonts/DejaVuSans.ttf", "font"),
            ]);
        var targetDirectory = Path.Combine(directory.Path, "target");

        ArchiveExtractor.Extract(archivePath, targetDirectory, "tar.gz");

        Assert.That(File.ReadAllText(Path.Combine(targetDirectory, "Main.exe")), Is.EqualTo("runtime"));
        Assert.That(File.ReadAllText(Path.Combine(targetDirectory, "Data", "Dec2.dat")), Is.EqualTo("data"));
        Assert.That(File.ReadAllText(Path.Combine(targetDirectory, "fonts", "DejaVuSans.ttf")), Is.EqualTo("font"));
    }

    [Test]
    public void Extract_WithZip_CreatesAllFiles()
    {
        using var directory = new TestDirectoryHelper();
        var archivePath = Path.Combine(directory.Path, "archive.zip");
        TestArchiveHelper.CreateZip(archivePath, [("Main.exe", "runtime")]);
        var targetDirectory = Path.Combine(directory.Path, "target");

        ArchiveExtractor.Extract(archivePath, targetDirectory, "zip");

        Assert.That(File.ReadAllText(Path.Combine(targetDirectory, "Main.exe")), Is.EqualTo("runtime"));
    }

    [Test]
    public void Extract_WithParentDirectoryEntry_Throws()
    {
        using var directory = new TestDirectoryHelper();
        var archivePath = Path.Combine(directory.Path, "hostile.tar.gz");
        TestArchiveHelper.CreateTarGzWithRawEntryName(archivePath, "../evil.txt", "boom");
        var targetDirectory = Path.Combine(directory.Path, "target");

        Assert.That(
            () => ArchiveExtractor.Extract(archivePath, targetDirectory, "tar.gz"),
            Throws.TypeOf<InvalidDataException>());
        Assert.That(File.Exists(Path.Combine(directory.Path, "evil.txt")), Is.False);
    }

    [Test]
    public void Extract_WithAbsolutePathEntry_ExtractsIntoTargetDirectory()
    {
        using var directory = new TestDirectoryHelper();
        var archivePath = Path.Combine(directory.Path, "hostile-absolute.tar.gz");
        TestArchiveHelper.CreateTarGzWithRawEntryName(archivePath, "/tmp/evil.txt", "boom");
        var targetDirectory = Path.Combine(directory.Path, "target");

        ArchiveExtractor.Extract(archivePath, targetDirectory, "tar.gz");

        Assert.That(File.Exists(Path.Combine(targetDirectory, "tmp", "evil.txt")), Is.True);
    }

    [Test]
    public void Extract_WithSymbolicLink_Throws()
    {
        using var directory = new TestDirectoryHelper();
        var archivePath = Path.Combine(directory.Path, "link.tar.gz");
        TestArchiveHelper.CreateTarGzWithLink(archivePath, "link", "target");
        var targetDirectory = Path.Combine(directory.Path, "target");

        Assert.That(
            () => ArchiveExtractor.Extract(archivePath, targetDirectory, "tar.gz"),
            Throws.TypeOf<InvalidDataException>());
    }
}
