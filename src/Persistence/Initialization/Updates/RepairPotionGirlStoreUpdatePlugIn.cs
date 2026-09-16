// <copyright file="RepairPotionGirlStoreUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Repairs Potion Girl Amy's store after the inventory-extension update left its previous stock behind.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("9D5E3F71-2A84-4C6B-B1D9-8E0F7A6C5B43")]
public sealed class RepairPotionGirlStoreUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Repair Potion Girl Store";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Rebuilds Potion Girl Amy's store without duplicate stock items.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RepairPotionGirlStore;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var accounts = (await context.GetAsync<Account>().ConfigureAwait(false)).ToList();
        var potionGirl = gameConfiguration.Monsters.Single(monster => monster.Number == 253);
        var detachedItems = potionGirl.MerchantStore?.Items.ToList() ?? [];

        InstantServerConfiguration.ConfigurePotionGirlStore(context, gameConfiguration);

        foreach (var item in detachedItems.Where(item => !IsItemReferenced(gameConfiguration, accounts, item)))
        {
            await context.DeleteAsync(item).ConfigureAwait(false);
        }
    }

    private static bool IsItemReferenced(GameConfiguration gameConfiguration, IEnumerable<Account> accounts, Item item)
        => gameConfiguration.Monsters.Any(monster => monster.MerchantStore?.Items.Contains(item) == true)
           || accounts.Any(account => account.Vault?.Items.Contains(item) == true
                                      || account.Characters.Any(character => character.Inventory?.Items.Contains(item) == true));
}
