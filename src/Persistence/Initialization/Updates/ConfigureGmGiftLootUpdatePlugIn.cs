// <copyright file="ConfigureGmGiftLootUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces the GM Gift equipment jackpot with its configured outcome table.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E7A31C52-4D86-48F0-AB29-7C15D9E642B8")]
public sealed class ConfigureGmGiftLootUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure GM Gift Loot";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Replaces the GM Gift equipment jackpot with jewelry, GM Gift boxes, fireworks and Ancient Set item outcomes.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureGmGiftLoot;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 19, 23, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        InstantServerConfiguration.ConfigureGmGiftLoot(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
