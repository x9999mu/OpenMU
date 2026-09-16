// <copyright file="AddInventoryExtensionItemToPotionGirlStoreUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the item which unlocks an inventory extension to the store of Potion Girl Amy.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("4C1D7E52-8A36-4F19-B27D-3E5F6A7B8C90")]
public sealed class AddInventoryExtensionItemToPotionGirlStoreUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Sell Inventory Extension At Potion Girl";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds the item which unlocks an inventory extension to the store of Potion Girl Amy.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddInventoryExtensionItemToPotionGirlStore;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
#pragma warning disable CS1998
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
#pragma warning restore CS1998
    {
        // Rebuilds the complete store, which keeps this update idempotent.
        InstantServerConfiguration.ConfigurePotionGirlStore(context, gameConfiguration);
    }
}
