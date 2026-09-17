// <copyright file="RestructureIcarusAndKalimaSevenJewelDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Moves the Gemstone, Jewel of Harmony and Jewel of Guardian drops to the individual
/// Icarus and Kalima 7 monsters and limits the loot of the Illusion of Kundun 7.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8F5B2C41-7D6E-4A93-B1C8-5E70A2D94F13")]
public sealed class RestructureIcarusAndKalimaSevenJewelDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Restructure Icarus and Kalima 7 Jewel Drops";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Moves Gemstone, Jewel of Harmony and Jewel of Guardian to per-monster drops in Icarus and Kalima 7, and limits Illusion of Kundun 7 to three GM Gifts, six Jewels of Harmony and three Jewels of Guardian.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RestructureIcarusAndKalimaSevenJewelDrops;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        InstantServerConfiguration.ConfigureIcarusAndKalimaSevenJewelDrops(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
