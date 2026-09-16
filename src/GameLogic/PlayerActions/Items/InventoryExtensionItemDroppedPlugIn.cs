// <copyright file="InventoryExtensionItemDroppedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;
using static MUnique.OpenMU.GameLogic.PlugIns.IItemDropPlugIn;

/// <summary>
/// Provides a drop-to-use fallback for the item which unlocks an inventory extension.
/// Clients which don't send a consume request for this item can drop it to apply the same effect.
/// If the character unlocked all extensions already, the drop is a normal drop.
/// </summary>
[PlugIn]
[Display(Name = "Inventory Extension Item Drop", Description = "Unlocks an inventory extension when the item gets dropped.")]
[Guid("5D2F8A63-9C47-4E51-A38B-7D9E0F1A2B34")]
public sealed class InventoryExtensionItemDroppedPlugIn : IItemDropPlugIn
{
    /// <inheritdoc />
    public async ValueTask HandleItemDropAsync(Player player, Item item, Point target, ItemDropArguments dropArgs)
    {
        var itemIdentifier = ItemConstants.InventoryExtension;
        if (item.Definition is not { } definition
            || definition.Group != itemIdentifier.Group
            || definition.Number != itemIdentifier.Number)
        {
            return;
        }

        dropArgs.Success = await InventoryExtensionConsumeHandlerPlugIn.TryUnlockExtensionAsync(player).ConfigureAwait(false);
        dropArgs.WasHandled = dropArgs.Success;
    }
}
