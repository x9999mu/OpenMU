// <copyright file="UpdateCheckResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

/// <summary>
/// The result of an update check.
/// </summary>
/// <param name="Manifest">The remote manifest.</param>
/// <param name="NeedsRuntime">A value indicating whether the runtime package needs to be downloaded.</param>
/// <param name="NeedsData">A value indicating whether the data package needs to be downloaded.</param>
/// <param name="IsClientInstalled">A value indicating whether the client is installed locally.</param>
internal sealed record UpdateCheckResult(UpdateManifest Manifest, bool NeedsRuntime, bool NeedsData, bool IsClientInstalled)
{
    /// <summary>
    /// Gets a value indicating whether any package needs to be downloaded.
    /// </summary>
    internal bool UpdateRequired => this.NeedsRuntime || this.NeedsData;
}
