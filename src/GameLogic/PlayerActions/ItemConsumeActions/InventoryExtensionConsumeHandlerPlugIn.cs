// <copyright file="InventoryExtensionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the item which unlocks the next inventory extension.
/// </summary>
[Guid("7C2E5A41-9B63-4D18-8F2A-5E6D7C8B9A01")]
[PlugIn]
[Display(Name = "Inventory Extension", Description = "Unlocks the next inventory extension when the item is consumed.")]
public class InventoryExtensionConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => ItemConstants.InventoryExtension;

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (player.SelectedCharacter is not { } character
            || character.InventoryExtensions >= InventoryConstants.MaximumNumberOfExtensions
            || !this.CheckPreconditions(player, item))
        {
            return false;
        }

        character.InventoryExtensions += 1;

        // The storages are created when the character enters the world, so the new slots have to be added to the
        // existing storage instance. Replacing it would silently drop its subscribers, e.g. the skill list.
        if (player.Inventory is InventoryStorage inventoryStorage)
        {
            inventoryStorage.AddExtension();
        }

        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateCharacterStatsPlugIn>(p => p.UpdateCharacterStatsAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(p => p.UpdateInventoryListAsync()).ConfigureAwait(false);
        return true;
    }
}
