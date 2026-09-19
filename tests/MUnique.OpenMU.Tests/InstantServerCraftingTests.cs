// <copyright file="InstantServerCraftingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Tests the instant-server Chaos Machine handlers with runtime-calculated rates.
/// </summary>
[TestFixture]
public class InstantServerCraftingTests
{
    /// <summary>
    /// Event ticket handlers return 100% for low and high event levels.
    /// </summary>
    [TestCaseSource(nameof(EventTicketCases))]
    public async Task EventTicketsAlwaysSucceedAsync(BaseEventTicketCrafting handler, string firstItem, string secondItem, byte level)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await AddItemAsync(player, firstItem, level).ConfigureAwait(false);
        await AddItemAsync(player, secondItem, level).ConfigureAwait(false);
        await AddItemAsync(player, "Jewel of Chaos", 0).ConfigureAwait(false);

        var result = handler.TryGetRequiredItems(player, out _, out var successRate);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(successRate, Is.EqualTo(100));
        });
    }

    /// <summary>
    /// A valid Fenrir upgrade keeps its validation rules but receives a fixed rate.
    /// </summary>
    [Test]
    public async Task FenrirUpgradeAlwaysSucceedsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await AddItemAsync(player, "Horn of Fenrir", 0).ConfigureAwait(false);
        await AddItemAsync(player, "Jewel of Chaos", 0).ConfigureAwait(false);
        for (var i = 0; i < 5; i++)
        {
            await AddItemAsync(player, "Jewel of Life", 0).ConfigureAwait(false);
        }
        var sacrifice = await AddItemAsync(player, "Sacrificial Sword", 4).ConfigureAwait(false);
        sacrifice.Definition!.Group = 0;
        sacrifice.Definition.Number = 30;
        sacrifice.Definition.DropLevel = 100;
        sacrifice.Definition.Width = 1;
        sacrifice.Definition.Durability = 100;
        var slotType = player.PersistenceContext.CreateNew<ItemSlotType>();
        slotType.ItemSlots.Add(0);
        sacrifice.Definition.ItemSlot = slotType;
        var attackSpeed = player.PersistenceContext.CreateNew<ItemBasePowerUpDefinition>();
        attackSpeed.TargetAttribute = Stats.AttackSpeedByWeapon;
        sacrifice.Definition.BasePowerUpAttributes.Add(attackSpeed);
        var option = player.PersistenceContext.CreateNew<IncreasableItemOption>();
        option.OptionType = ItemOptionTypes.Option;
        var optionLink = player.PersistenceContext.CreateNew<ItemOptionLink>();
        optionLink.ItemOption = option;
        optionLink.Level = 4;
        sacrifice.ItemOptions.Add(optionLink);

        var handler = new InstantServerFenrirUpgradeCrafting();
        var result = handler.TryGetRequiredItems(player, out _, out var successRate);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(successRate, Is.EqualTo(100));
        });
    }

    /// <summary>
    /// Overlapping crafting requirements consume distinct items.
    /// </summary>
    [Test]
    public async Task OverlappingCraftingRequirementsConsumeDistinctItemsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var excellentOptionType = player.PersistenceContext.CreateNew<ItemOptionType>();
        var ancientBonusOptionType = player.PersistenceContext.CreateNew<ItemOptionType>();
        var settings = player.PersistenceContext.CreateNew<SimpleCraftingSettings>();
        var excellentRequirement = player.PersistenceContext.CreateNew<ItemCraftingRequiredItem>();
        excellentRequirement.MinimumAmount = 1;
        excellentRequirement.MaximumAmount = 1;
        excellentRequirement.MinimumItemLevel = 4;
        excellentRequirement.MaximumItemLevel = 15;
        excellentRequirement.RequiredItemOptions.Add(excellentOptionType);
        settings.RequiredItems.Add(excellentRequirement);
        var ancientRequirement = player.PersistenceContext.CreateNew<ItemCraftingRequiredItem>();
        ancientRequirement.MinimumAmount = 1;
        ancientRequirement.MaximumAmount = 1;
        ancientRequirement.MinimumItemLevel = 4;
        ancientRequirement.MaximumItemLevel = 15;
        ancientRequirement.RequiredItemOptions.Add(ancientBonusOptionType);
        settings.RequiredItems.Add(ancientRequirement);

        var fullAncientItem = await AddCraftingItemAsync(player, 4, excellentOptionType, ancientBonusOptionType).ConfigureAwait(false);
        var secondAncientItem = await AddCraftingItemAsync(player, 4, excellentOptionType, ancientBonusOptionType).ConfigureAwait(false);


        var result = new SimpleItemCraftingHandler(settings).TryGetRequiredItems(player, out var items, out _);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            var ancientLink = items.Single(link => link.ItemRequirement == ancientRequirement);
            var excellentLink = items.Single(link => link.ItemRequirement == excellentRequirement);
            Assert.That(ancientLink.Items.Single(), Is.Not.SameAs(excellentLink.Items.Single()));
            Assert.That(new[] { fullAncientItem, secondAncientItem }, Does.Contain(ancientLink.Items.Single()));
            Assert.That(new[] { fullAncientItem, secondAncientItem }, Does.Contain(excellentLink.Items.Single()));
        });
    }

    private static IEnumerable<TestCaseData> EventTicketCases()
    {
        foreach (var level in new byte[] { 1, 7 })
        {
            yield return new TestCaseData(new InstantServerBloodCastleTicketCrafting(), "Scroll of Archangel", "Blood Bone", level);
            yield return new TestCaseData(new InstantServerDevilSquareTicketCrafting(), "Devil's Eye", "Devil's Key", level);
            yield return new TestCaseData(new InstantServerIllusionTempleTicketCrafting(), "Old Scroll", "Illusion Sorcerer Covenant", level);
        }
    }

    private static async ValueTask<Item> AddItemAsync(Player player, string name, byte level)
    {
        var definition = player.PersistenceContext.CreateNew<ItemDefinition>();
        definition.Name = new LocalizedString(name);
        definition.Width = 1;
        definition.Height = 1;
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Level = level;
        item.Durability = 1;
        Assert.That(await player.TemporaryStorage!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return item;
    }

    private static async ValueTask<Item> AddCraftingItemAsync(Player player, byte level, params ItemOptionType[] optionTypes)
    {
        var definition = player.PersistenceContext.CreateNew<ItemDefinition>();
        definition.Width = 1;
        definition.Height = 1;
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Level = level;
        item.Durability = 1;
        foreach (var optionType in optionTypes)
        {
            var option = player.PersistenceContext.CreateNew<IncreasableItemOption>();
            option.OptionType = optionType;
            var optionLink = player.PersistenceContext.CreateNew<ItemOptionLink>();
            optionLink.ItemOption = option;
            item.ItemOptions.Add(optionLink);
        }

        Assert.That(await player.TemporaryStorage!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return item;
    }
}
