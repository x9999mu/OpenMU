// <copyright file="TestDirectoryHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.IO;

/// <summary>
/// A temporary directory which is deleted when it is disposed.
/// </summary>
internal sealed class TestDirectoryHelper : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestDirectoryHelper"/> class.
    /// </summary>
    internal TestDirectoryHelper()
    {
        this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "openmu-launcher-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.Path);
    }

    /// <summary>
    /// Gets the path of the directory.
    /// </summary>
    internal string Path { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this.Path))
            {
                Directory.Delete(this.Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // The temp directory is cleaned up by the operating system.
        }
    }
}
