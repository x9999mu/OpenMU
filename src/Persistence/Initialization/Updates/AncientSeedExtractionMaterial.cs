// <copyright file="AncientSeedExtractionMaterial.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Items;

/// <summary>
/// Configures the item which the seed master sells as material for the seed extraction crafting.
/// The crafting requires an ancient item of level 4 or higher which carries an ancient bonus
/// option, so adding the item set group alone is not enough.
/// </summary>
internal static class AncientSeedExtractionMaterial
{
    /// <summary>
    /// The npc number of Rhea, the seed master in Elveland.
    /// </summary>
    internal const short RheaNpcNumber = 416;

    /// <summary>
    /// The item number of the Red Wing Helm of the Semeden set.
    /// </summary>
    internal const byte RedWingHelmNumber = 40;

    /// <summary>
    /// The name of the ancient set of the material item.
    /// </summary>
    internal const string AncientSetName = "Semeden";

    /// <summary>
    /// The ancient set discriminator of the material item.
    /// </summary>
    internal const int AncientSetDiscriminator = 2;

    /// <summary>
    /// The item level which the seed extraction crafting expects.
    /// </summary>
    private const byte RequiredItemLevel = 4;

    /// <summary>
    /// Gets the item definition of the material item.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The item definition, if it exists.</returns>
    internal static ItemDefinition? FindItemDefinition(GameConfiguration gameConfiguration)
        => gameConfiguration.Items.FirstOrDefault(item => item.Group == (byte)ItemGroups.Helm && item.Number == RedWingHelmNumber);

    /// <summary>
    /// Gets the ancient set item of the material item.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="itemDefinition">The item definition of the material item.</param>
    /// <returns>The item of the ancient set, if it exists.</returns>
    internal static ItemOfItemSet? FindAncientSetItem(GameConfiguration gameConfiguration, ItemDefinition itemDefinition)
        => gameConfiguration.ItemSetGroups
            .FirstOrDefault(group => group.Name == AncientSetName)?.Items
            .FirstOrDefault(item => item.ItemDefinition == itemDefinition && item.AncientSetDiscriminator == AncientSetDiscriminator);

    /// <summary>
    /// Turns the specified item into a material which the seed extraction crafting accepts.
    /// </summary>
    /// <param name="context">The context which is used to create the missing option link.</param>
    /// <param name="item">The item.</param>
    /// <param name="itemOfSet">The ancient set item which the item belongs to.</param>
    internal static void MakeUsableForSeedExtraction(IContext context, Item item, ItemOfItemSet itemOfSet)
    {
        item.Level = RequiredItemLevel;
        if (!item.ItemSetGroups.Contains(itemOfSet))
        {
            item.ItemSetGroups.Add(itemOfSet);
        }

        if (item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.AncientBonus))
        {
            return;
        }

        var bonusOptionLink = context.CreateNew<ItemOptionLink>();
        bonusOptionLink.ItemOption = itemOfSet.BonusOption;
        bonusOptionLink.Level = 1;
        item.ItemOptions.Add(bonusOptionLink);
    }
}
