// <copyright file="ConfigureLumenEventTicketsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces Lumen the Barmaid's component materials with completed event tickets.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6132DFE2-CB84-4F9D-B7BB-C802C75FA2A9")]
public sealed class ConfigureLumenEventTicketsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure Lumen Event Tickets";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Replaces Lumen's event-ticket components with completed event tickets and a Kalima 7 Lost Map.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureLumenEventTickets;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 14, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
        => ConfigureStoreAsync(context, gameConfiguration);

    /// <summary>
    /// Replaces the store contents and packs every ticket according to its actual dimensions.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The asynchronous operation.</returns>
    internal static async ValueTask ConfigureStoreAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var store = gameConfiguration.Monsters.Single(monster => monster.Number == 255).MerchantStore!;
        var previousItems = store.Items.ToList();
        store.Items.Clear();

        var itemHelper = new ItemHelper(context, gameConfiguration);
        byte slot = 0;
        foreach (var level in Enumerable.Range(1, 8))
        {
            store.Items.Add(itemHelper.CreateItem(slot++, 18, 13, 1, (byte)level));
        }

        foreach (var level in Enumerable.Range(1, 7))
        {
            store.Items.Add(itemHelper.CreateItem(slot++, 19, 14, 1, (byte)level));
        }

        foreach (var level in Enumerable.Range(1, 6))
        {
            store.Items.Add(itemHelper.CreateItem(slot++, 51, 13, 1, (byte)level));
        }

        store.Items.Add(itemHelper.CreateItem(slot++, 28, 14, 1, 7));
        store.Items.Add(itemHelper.CreatePotion(slot++, 9, 1, 0));
        store.Items.Add(itemHelper.CreateItem(slot, 29, 13, 1, 0));

        var currentItems = store.Items.ToList();
        store.Items.Clear();
        var merchantStorage = new Storage(InventoryConstants.WarehouseSize, store);
        foreach (var item in currentItems)
        {
            if (!await merchantStorage.AddItemAsync(item).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Lumen's store cannot fit {item.Definition}.");
            }
        }

        foreach (var item in previousItems)
        {
            await context.DeleteAsync(item).ConfigureAwait(false);
        }
    }
}
