// <copyright file="LauncherJson.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher;

using System.Text.Json;

/// <summary>
/// Provides the json options which are shared by the launcher components.
/// </summary>
internal static class LauncherJson
{
    /// <summary>
    /// Gets the options which are used to read and write the launcher files.
    /// </summary>
    internal static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}
