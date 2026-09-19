// <copyright file="ArchiveExtractor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

    using System.Formats.Tar;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;

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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidDataException">Thrown when the archive contains unsafe entries.</exception>
    internal static void Extract(string archivePath, string targetDirectory, string format, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);
        if (string.Equals(format, "zip", StringComparison.OrdinalIgnoreCase))
        {
            ExtractZip(archivePath, targetDirectory, cancellationToken);
            return;
        }

        ExtractTarGz(archivePath, targetDirectory, cancellationToken);
    }

    private static void ExtractTarGz(string archivePath, string targetDirectory, CancellationToken cancellationToken)
    {
        var targetRoot = Path.GetFullPath(targetDirectory);
        using var fileStream = File.OpenRead(archivePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var tarReader = new TarReader(gzipStream);
        while (tarReader.GetNextEntry() is { } entry)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

    private static void ExtractZip(string archivePath, string targetDirectory, CancellationToken cancellationToken)
    {
        var targetRoot = Path.GetFullPath(targetDirectory);
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            var destinationPath = GetSafeDestinationPath(targetRoot, entry.FullName);
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static string GetSafeDestinationPath(string targetRoot, string entryName)
    {
        var segments = entryName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".."))
        {
            throw new InvalidDataException($"The archive contains the unsafe entry '{entryName}'.");
        }

        var relativePath = Path.Combine(segments);
        var destinationPath = Path.GetFullPath(Path.Combine(targetRoot, relativePath));

        // Contains checks via relative path, so entries of the root directory itself (like "./")
        // are accepted while everything outside of the target directory is rejected.
        var relativeToRoot = Path.GetRelativePath(targetRoot, destinationPath);
        if (Path.IsPathRooted(relativeToRoot)
            || relativeToRoot == ".."
            || relativeToRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The archive entry '{entryName}' points outside of the target directory.");
        }

        return destinationPath;
    }
}
