// <copyright file="AddAncientSeedExtractionMaterialToRheaStoreUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the ancient item to Rhea's Elvenland store which is required for the seed extraction.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("F2A693E1-58BC-4D70-915F-4B97601C8D2A")]
public sealed class AddAncientSeedExtractionMaterialToRheaStoreUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Sell Ancient Seed Extraction Material At Rhea";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds a +4 Semeden Red Wing Helm with an ancient bonus option to Rhea's Elvenland store, so it can be used for the seed extraction.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddAncientSeedExtractionMaterialToRheaStore;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 18, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var rheaStore = gameConfiguration.Monsters
            .FirstOrDefault(monster => monster.Number == AncientSeedExtractionMaterial.RheaNpcNumber)?.MerchantStore;
        var definition = AncientSeedExtractionMaterial.FindItemDefinition(gameConfiguration);
        var ancientItem = definition is null
            ? null
            : AncientSeedExtractionMaterial.FindAncientSetItem(gameConfiguration, definition);
        if (rheaStore is null || definition is null || ancientItem is null)
        {
            return;
        }

        if (rheaStore.Items.Any(item => item.Definition == definition && item.ItemSetGroups.Contains(ancientItem)))
        {
            return;
        }

        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        AncientSeedExtractionMaterial.MakeUsableForSeedExtraction(context, item, ancientItem);

        var option = context.CreateNew<ItemOptionLink>();
        option.ItemOption = definition.PossibleItemOptions.SelectMany(options => options.PossibleOptions)
            .Single(itemOption => itemOption.OptionType == ItemOptionTypes.Option);
        option.Level = 1;
        item.ItemOptions.Add(option);

        var storage = new Storage(InventoryConstants.WarehouseSize, rheaStore);
        if (!await storage.AddItemAsync(item).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Rhea's store has no room for the Ancient Seed Extraction material.");
        }
    }
}
