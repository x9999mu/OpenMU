// <copyright file="ConfigureFullAncientLootUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes Ancient Set rewards to include all supported excellent options and Luck.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A4D8E3F1-6B27-4C90-9E15-73F2B8C4D601")]
public sealed class ConfigureFullAncientLootUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure Full Ancient Loot";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds full excellent Ancient Set options and changes GM Gift fireworks to 3 percent.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureFullAncientLoot;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 19, 23, 45, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        InstantServerConfiguration.ConfigureKalimaSevenBossDrops(context, gameConfiguration);
        InstantServerConfiguration.ConfigureGmGiftLoot(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
