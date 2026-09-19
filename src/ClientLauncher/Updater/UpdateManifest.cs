// <copyright file="UpdateManifest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;

/// <summary>
/// Describes the latest available client build and where to download it.
/// </summary>
internal sealed class UpdateManifest
{
    /// <summary>
    /// The schema version which is supported by this launcher.
    /// </summary>
    internal const int SupportedSchemaVersion = 1;

    /// <summary>
    /// Gets or sets the schema version of the manifest.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the release channel, e.g. <c>stable</c>.
    /// </summary>
    public string Channel { get; set; } = "stable";

    /// <summary>
    /// Gets or sets the time at which the manifest was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the runtime package (executable, libraries and shaders).
    /// </summary>
    public PackageInfo Runtime { get; set; } = new();

    /// <summary>
    /// Gets or sets the data package (<c>Data</c> and <c>fonts</c>).
    /// </summary>
    public PackageInfo Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the information about the latest launcher version.
    /// </summary>
    public LauncherInfo? Launcher { get; set; }

    /// <summary>
    /// Gets or sets the default game server address.
    /// </summary>
    public ServerInfo? Server { get; set; }

    /// <summary>
    /// Gets or sets the files which must not be overwritten when applying an update.
    /// </summary>
    public List<string> Preserve { get; set; } = ["config.ini"];

    /// <summary>
    /// Validates the manifest and throws an exception if it can not be used.
    /// </summary>
    /// <exception cref="InvalidDataException">Thrown when the manifest is not valid.</exception>
    internal void Validate()
    {
        if (this.SchemaVersion != SupportedSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported manifest schema version {this.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(this.Runtime?.Version))
        {
            throw new InvalidDataException("The manifest does not contain a runtime version.");
        }

        ValidateArchive(this.Runtime!.Archive, "runtime");

        if (string.IsNullOrWhiteSpace(this.Data?.Id))
        {
            throw new InvalidDataException("The manifest does not contain a data id.");
        }

        ValidateArchive(this.Data!.Archive, "data");

        if (this.Launcher is { } launcher)
        {
            if (string.IsNullOrWhiteSpace(launcher.Version) || string.IsNullOrWhiteSpace(launcher.Url))
            {
                throw new InvalidDataException("The launcher information of the manifest is incomplete.");
            }
        }
    }

    private static void ValidateArchive(ArchiveInfo? archive, string name)
    {
        if (archive is null
            || string.IsNullOrWhiteSpace(archive.Url)
            || string.IsNullOrWhiteSpace(archive.Sha256)
            || archive.Size <= 0)
        {
            throw new InvalidDataException($"The {name} archive information of the manifest is incomplete.");
        }

        if (!Uri.TryCreate(archive.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && !IsLoopback(uri)))
        {
            throw new InvalidDataException($"The {name} archive url '{archive.Url}' is not a valid https url.");
        }

        if (archive.Sha256.Length != 64)
        {
            throw new InvalidDataException($"The {name} archive hash '{archive.Sha256}' is not a sha256 hash.");
        }
    }

    private static bool IsLoopback(Uri uri) => uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;

    /// <summary>
    /// Describes one downloadable package of the client.
    /// </summary>
    internal sealed class PackageInfo
    {
        /// <summary>
        /// Gets or sets the version of the runtime package.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the content id of the data package.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the release tag which contains the archive.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets the archive of the package.
        /// </summary>
        public ArchiveInfo? Archive { get; set; }
    }

    /// <summary>
    /// Describes a downloadable archive.
    /// </summary>
    internal sealed class ArchiveInfo
    {
        /// <summary>
        /// Gets or sets the download url.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the archive in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the sha256 hash of the archive.
        /// </summary>
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the format of the archive, e.g. <c>tar.gz</c> or <c>zip</c>.
        /// </summary>
        public string Format { get; set; } = "tar.gz";
    }

    /// <summary>
    /// Describes the latest launcher version.
    /// </summary>
    internal sealed class LauncherInfo
    {
        /// <summary>
        /// Gets or sets the version of the launcher.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download url of the launcher executable.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the executable in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the sha256 hash of the executable.
        /// </summary>
        public string Sha256 { get; set; } = string.Empty;
    }

    /// <summary>
    /// Describes the default game server address.
    /// </summary>
    internal sealed class ServerInfo
    {
        /// <summary>
        /// Gets or sets the address of the connect server.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the optional host name of the connect server.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the port of the connect server.
        /// </summary>
        public int Port { get; set; } = 44405;
    }
}
