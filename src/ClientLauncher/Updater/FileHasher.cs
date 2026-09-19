// <copyright file="FileHasher.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.IO;
using System.Security.Cryptography;
using System.Threading;

/// <summary>
/// Calculates sha256 hashes of files.
/// </summary>
internal static class FileHasher
{
    /// <summary>
    /// Calculates the sha256 hash of the specified file, using a streaming algorithm.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hash in lower case hexadecimal notation.</returns>
    internal static async Task<string> HashFileAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
