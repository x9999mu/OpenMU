// <copyright file="ArchiveExtractor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Formats.Tar;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Extracts the downloaded archives into a staging directory.
/// </summary>
internal static class ArchiveExtractor
{
    /// <summary>
    /// Extracts the specified archive into the target directory.
    /// </summary>
    /// <param name="archivePath">The path of the archive.</param>
    /// <param name="targetDirectory">The directory into which the archive is extracted.</param>
    /// <param name="format">The format of the archive, e.g. <c>tar.gz</c> or <c>zip</c>.</param>
    /// <exception cref="InvalidDataException">Thrown when the archive contains unsafe entries.</exception>
    internal static void Extract(string archivePath, string targetDirectory, string format)
    {
        Directory.CreateDirectory(targetDirectory);
        if (string.Equals(format, "zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, targetDirectory, overwriteFiles: true);
            return;
        }

        ExtractTarGz(archivePath, targetDirectory);
    }

    private static void ExtractTarGz(string archivePath, string targetDirectory)
    {
        var targetRoot = Path.GetFullPath(targetDirectory);
        using var fileStream = File.OpenRead(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var tarReader = new TarReader(gzipStream);
        while (tarReader.GetNextEntry() is { } entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var destinationPath = GetSafeDestinationPath(targetRoot, entry.Name);
            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(destinationPath);
                    break;
                case TarEntryType.SymbolicLink:
                case TarEntryType.HardLink:
                    throw new InvalidDataException($"The archive contains the link entry '{entry.Name}'.");
                default:
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                    break;
            }
        }
    }

    private static string GetSafeDestinationPath(string targetRoot, string entryName)
    {
        var segments = entryName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment == ".."))
        {
            throw new InvalidDataException($"The archive contains the unsafe entry '{entryName}'.");
        }

        var relativePath = Path.Combine(segments);
        var destinationPath = Path.GetFullPath(Path.Combine(targetRoot, relativePath));
        if (!destinationPath.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The archive entry '{entryName}' points outside of the target directory.");
        }

        return destinationPath;
    }
}
