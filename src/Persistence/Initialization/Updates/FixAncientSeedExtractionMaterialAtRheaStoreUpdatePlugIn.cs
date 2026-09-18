// <copyright file="FixAncientSeedExtractionMaterialAtRheaStoreUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Repairs the material item which Rhea sells for the seed extraction. The seed extraction
/// crafting requires an ancient item of level 4 or higher with an ancient bonus option,
/// while the item was created without the level and the option before.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8CEB18C5-AB12-433A-90D5-ED5D29E3F676")]
public sealed class FixAncientSeedExtractionMaterialAtRheaStoreUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Fix Ancient Seed Extraction Material At Rhea";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Turns the ancient item sold at Rhea into a usable material for the seed extraction (level 4 and ancient bonus option).";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixAncientSeedExtractionMaterialAtRheaStore;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 18, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var rheaStore = gameConfiguration.Monsters
            .FirstOrDefault(monster => monster.Number == AncientSeedExtractionMaterial.RheaNpcNumber)?.MerchantStore;
        var definition = AncientSeedExtractionMaterial.FindItemDefinition(gameConfiguration);
        var ancientItem = definition is null
            ? null
            : AncientSeedExtractionMaterial.FindAncientSetItem(gameConfiguration, definition);
        if (rheaStore is null || definition is null || ancientItem is null)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var item in rheaStore.Items.Where(item => item.Definition == definition).ToList())
        {
            AncientSeedExtractionMaterial.MakeUsableForSeedExtraction(context, item, ancientItem);
        }

        return ValueTask.CompletedTask;
    }
}
