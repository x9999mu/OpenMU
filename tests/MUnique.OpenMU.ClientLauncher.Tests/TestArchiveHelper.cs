// <copyright file="TestArchiveHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

/// <summary>
/// Creates small archives for the tests.
/// </summary>
internal static class TestArchiveHelper
{
    /// <summary>
    /// Creates a tar.gz archive with the specified regular file entries.
    /// </summary>
    /// <param name="archivePath">The path of the archive to create.</param>
    /// <param name="entries">The entries of the archive.</param>
    internal static void CreateTarGz(string archivePath, IEnumerable<(string Name, string Content)> entries)
    {
        using var fileStream = File.Create(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Pax, leaveOpen: false);
        foreach (var (name, content) in entries)
        {
            var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
            {
                DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
            };
            writer.WriteEntry(entry);
        }
    }

    /// <summary>
    /// Creates a tar.gz archive with a root directory entry and a "./" prefixed file, which is
    /// what <c>tar -C directory .</c> produces.
    /// </summary>
    /// <param name="archivePath">The path of the archive to create.</param>
    /// <param name="content">The content of the "./Main.exe" entry.</param>
    internal static void CreateTarGzWithRootEntry(string archivePath, string content)
    {
        using var fileStream = File.Create(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Pax, leaveOpen: false);
        writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "./"));
        writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "./Main.exe")
        {
            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
        });
        writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "./Data/Dec2.dat")
        {
            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
        });
    }

    /// <summary>
    /// Creates a tar.gz archive which contains a symbolic link entry.
    /// </summary>
    /// <param name="archivePath">The path of the archive to create.</param>
    /// <param name="linkName">The name of the link entry.</param>
    /// <param name="linkTarget">The target of the link.</param>
    internal static void CreateTarGzWithLink(string archivePath, string linkName, string linkTarget)
    {
        using var fileStream = File.Create(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Pax, leaveOpen: false);
        var entry = new PaxTarEntry(TarEntryType.SymbolicLink, linkName)
        {
            LinkName = linkTarget,
        };
        writer.WriteEntry(entry);
    }

    /// <summary>
    /// Creates a zip archive with the specified entries.
    /// </summary>
    /// <param name="archivePath">The path of the archive to create.</param>
    /// <param name="entries">The entries of the archive.</param>
    internal static void CreateZip(string archivePath, IEnumerable<(string Name, string Content)> entries)
    {
        using var fileStream = File.Create(archivePath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(content);
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }

    /// <summary>
    /// Calculates the sha256 hash of the specified file.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The hash in lower case hexadecimal notation.</returns>
    internal static async Task<string> HashAsync(string filePath) => await FileHasher.HashFileAsync(filePath, CancellationToken.None);

    /// <summary>
    /// Creates a tar.gz archive which contains the given entry name as-is, to test unsafe entries.
    /// </summary>
    /// <param name="archivePath">The path of the archive to create.</param>
    /// <param name="entryName">The raw entry name.</param>
    /// <param name="content">The content of the entry.</param>
    internal static void CreateTarGzWithRawEntryName(string archivePath, string entryName, string content)
    {
        using var fileStream = File.Create(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize);
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var header = new byte[512];
        WriteAscii(header, 0, 100, entryName);
        WriteOctal(header, 100, 8, 0b110100100);
        WriteOctal(header, 108, 8, 0);
        WriteOctal(header, 116, 8, 0);
        WriteOctal(header, 124, 12, contentBytes.Length);
        WriteOctal(header, 136, 12, 0);
        for (var i = 148; i < 156; i++)
        {
            header[i] = (byte)' ';
        }

        header[156] = (byte)'0';
        WriteAscii(header, 257, 6, "ustar");
        header[263] = (byte)'0';
        header[264] = (byte)'0';
        for (var i = 148; i < 156; i++)
        {
            header[i] = (byte)' ';
        }

        var checksum = header.Sum(value => (int)value);
        WriteOctal(header, 148, 7, checksum);
        header[155] = (byte)' ';

        gzipStream.Write(header, 0, header.Length);
        gzipStream.Write(contentBytes, 0, contentBytes.Length);
        var padding = (512 - (contentBytes.Length % 512)) % 512;
        if (padding > 0)
        {
            gzipStream.Write(new byte[padding], 0, padding);
        }

        gzipStream.Write(new byte[1024], 0, 1024);
    }

    private static void WriteAscii(byte[] buffer, int offset, int length, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        Array.Copy(bytes, 0, buffer, offset, Math.Min(bytes.Length, length));
    }

    private static void WriteOctal(byte[] buffer, int offset, int length, long value)
    {
        var text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
        WriteAscii(buffer, offset, length - 1, text);
        buffer[offset + length - 1] = 0;
    }
}
