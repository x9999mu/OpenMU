// <copyright file="DownloadProgress.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

/// <summary>
/// Reports the progress of a download.
/// </summary>
/// <param name="BytesReceived">The number of received bytes.</param>
/// <param name="TotalBytes">The total number of bytes.</param>
/// <param name="BytesPerSecond">The current speed.</param>
/// <param name="FileName">The name of the file which is downloaded.</param>
/// <param name="Verified">A value indicating whether the download has been verified.</param>
internal readonly record struct DownloadProgress(long BytesReceived, long TotalBytes, double BytesPerSecond, string FileName, bool Verified)
{
    /// <summary>
    /// Gets the progress as a fraction between 0 and 1.
    /// </summary>
    internal double Fraction => this.TotalBytes > 0 ? Math.Clamp((double)this.BytesReceived / this.TotalBytes, 0, 1) : 0;
}
