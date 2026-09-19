// <copyright file="UpdateProgress.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

/// <summary>
/// The stage of an update process.
/// </summary>
internal enum UpdateStage
{
    /// <summary>
    /// The manifest is loaded and compared with the local state.
    /// </summary>
    Checking,

    /// <summary>
    /// Archives are downloaded.
    /// </summary>
    Downloading,

    /// <summary>
    /// Archives are verified.
    /// </summary>
    Verifying,

    /// <summary>
    /// Archives are extracted.
    /// </summary>
    Extracting,

    /// <summary>
    /// Files are copied into the client directory.
    /// </summary>
    Applying,

    /// <summary>
    /// The update is completed.
    /// </summary>
    Completed,
}

/// <summary>
/// Reports the progress of an update process.
/// </summary>
/// <param name="Stage">The current stage.</param>
/// <param name="Message">The message which is shown to the user.</param>
/// <param name="Fraction">The progress between 0 and 1, or a negative value for an indeterminate progress.</param>
internal readonly record struct UpdateProgress(UpdateStage Stage, string Message, double Fraction)
{
    /// <summary>
    /// Gets a value indicating whether the progress is indeterminate.
    /// </summary>
    internal bool IsIndeterminate => this.Fraction < 0;
}
