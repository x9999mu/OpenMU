// <copyright file="ProgressResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

/// <summary>
/// The result of an operation which was shown in the <see cref="ProgressForm"/>.
/// </summary>
/// <param name="Succeeded">A value indicating whether the operation finished successfully.</param>
/// <param name="Canceled">A value indicating whether the user canceled the operation.</param>
/// <param name="ErrorMessage">The error message, if the operation failed.</param>
internal readonly record struct ProgressResult(bool Succeeded, bool Canceled, string? ErrorMessage);
