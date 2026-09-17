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
/// Adds an Ancient item with a +4 option to Rhea's Elvenland store for Seed Extraction.
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
    internal const string PlugInDescription = "Adds a Semeden Red Wing Helm with a +4 option to Rhea's Elvenland store for Seed Extraction.";

    private const short RheaNpcNumber = 416;
    private const byte RedWingHelmNumber = 40;
    private const string AncientSetName = "Semeden";
    private const int AncientSetDiscriminator = 2;

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
        var rheaStore = gameConfiguration.Monsters.FirstOrDefault(monster => monster.Number == RheaNpcNumber)?.MerchantStore;
        if (rheaStore is null)
        {
            return;
        }

        var definition = gameConfiguration.Items.Single(item => item.Group == (byte)ItemGroups.Helm && item.Number == RedWingHelmNumber);
        var ancientItem = gameConfiguration.ItemSetGroups.Single(group => group.Name == AncientSetName).Items
            .Single(item => item.ItemDefinition == definition && item.AncientSetDiscriminator == AncientSetDiscriminator);
        if (rheaStore.Items.Any(item => item.Definition == definition && item.ItemSetGroups.Contains(ancientItem)))
        {
            return;
        }

        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        item.ItemSetGroups.Add(ancientItem);
        var option = context.CreateNew<ItemOptionLink>();
        option.ItemOption = definition.PossibleItemOptions.SelectMany(options => options.PossibleOptions)
            .Single(itemOption => itemOption.OptionType == ItemOptionTypes.Option);
        option.Level = 1; // The first additional-option level is displayed as +4.
        item.ItemOptions.Add(option);

        var storage = new Storage(InventoryConstants.WarehouseSize, rheaStore);
        if (!await storage.AddItemAsync(item).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Rhea's store has no room for the Ancient Seed Extraction material.");
        }
    }
}
