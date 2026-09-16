// <copyright file="InventoryExtensionConsumeHandlerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

/// <summary>
/// Tests the consumption of the item which unlocks inventory extensions.
/// </summary>
[TestFixture]
public class InventoryExtensionConsumeHandlerTest
{
    private const byte ItemSlot = 12;

    /// <summary>
    /// Verifies that consuming the item unlocks an extension whose slots can be used without re-entering the game.
    /// </summary>
    [Test]
    public async ValueTask UnlocksExtensionWithUsableSlotsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        var item = CreateItem();
        await player.Inventory!.AddItemAsync(ItemSlot, item).ConfigureAwait(false);

        var consumed = await new InventoryExtensionConsumeHandlerPlugIn()
            .ConsumeItemAsync(player, item, null, FruitUsage.Undefined)
            .ConfigureAwait(false);

        Assert.That(consumed, Is.True);
        Assert.That(item.Durability, Is.Zero, "the item should be used up");
        Assert.That(character.InventoryExtensions, Is.EqualTo(1));
        Assert.That(
            await player.Inventory.AddItemAsync(InventoryConstants.FirstExtensionItemSlotIndex, CreateItem()).ConfigureAwait(false),
            Is.True,
            "the first slot of the new extension should be usable right away");
    }

    /// <summary>
    /// Verifies that the item is not consumed when the character already unlocked all extensions.
    /// </summary>
    [Test]
    public async ValueTask DoesNotConsumeWhenAllExtensionsAreUnlockedAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        character.InventoryExtensions = InventoryConstants.MaximumNumberOfExtensions;
        var item = CreateItem();
        await player.Inventory!.AddItemAsync(ItemSlot, item).ConfigureAwait(false);

        var consumed = await new InventoryExtensionConsumeHandlerPlugIn()
            .ConsumeItemAsync(player, item, null, FruitUsage.Undefined)
            .ConfigureAwait(false);

        Assert.That(consumed, Is.False);
        Assert.That(item.Durability, Is.EqualTo(1), "the item should not be used up");
        Assert.That(character.InventoryExtensions, Is.EqualTo(InventoryConstants.MaximumNumberOfExtensions));
    }

    private static Item CreateItem()
    {
        return new()
        {
            Definition = new ItemDefinition { Group = 14, Number = 90, Width = 1, Height = 1, Durability = 1 },
            Durability = 1,
        };
    }
}
