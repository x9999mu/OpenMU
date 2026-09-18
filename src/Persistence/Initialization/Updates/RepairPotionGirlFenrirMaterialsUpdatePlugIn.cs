// <copyright file="RepairPotionGirlFenrirMaterialsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update rebuilds the store of Potion Girl Amy, so that the Fenrir materials and the Horn of Uniria
/// are sold with their full durability. The previous update sold them with a durability of 1, which made
/// them unusable for the Dinorant and Fenrir mixes.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6B4A2F18-5D93-4E7C-A0B1-9C8E7F3D2A55")]
public sealed class RepairPotionGirlFenrirMaterialsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Repair Fenrir Materials Durability At Potion Girl";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Rebuilds the store of Potion Girl Amy so that the Horn of Uniria, Splinter of Armor, Bless of Guardian and Claw of Beast are sold with full durability.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RepairPotionGirlFenrirMaterials;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 19, 12, 0, 0, DateTimeKind.Utc);

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
