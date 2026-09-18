// <copyright file="AddPotionGirlFenrirMaterialsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the Fenrir crafting materials and the Horn of Uniria to the store of Potion Girl Amy.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2E7B1D64-9C43-4B0E-8F27-6A5D3C1E9B84")]
public sealed class AddPotionGirlFenrirMaterialsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Sell Fenrir Materials At Potion Girl";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds the Horn of Uniria, Splinter of Armor, Bless of Guardian and Claw of Beast to the store of Potion Girl Amy.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddPotionGirlFenrirMaterials;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 19, 0, 0, 0, DateTimeKind.Utc);

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
