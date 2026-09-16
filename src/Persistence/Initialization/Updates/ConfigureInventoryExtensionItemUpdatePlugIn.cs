// <copyright file="ConfigureInventoryExtensionItemUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update prepares the item which unlocks an inventory extension: it makes sure that the item exists
/// and that it lasts exactly one use.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2F9A6C13-5D84-4B27-A1E3-6C7D8E9F0A12")]
public sealed class ConfigureInventoryExtensionItemUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure Inventory Extension Item";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Makes the item which unlocks an inventory extension exist and last exactly one use.";

    /// <summary>
    /// The item group of the item.
    /// </summary>
    private const byte ItemGroup = 14;

    /// <summary>
    /// The item number of the item.
    /// </summary>
    private const short ItemNumber = 90;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureInventoryExtensionItem;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
#pragma warning disable CS1998
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
#pragma warning restore CS1998
    {
        var itemDefinition = gameConfiguration.Items.FirstOrDefault(item => item.Group == ItemGroup && item.Number == ItemNumber);
        if (itemDefinition is null)
        {
            itemDefinition = context.CreateNew<ItemDefinition>();
            itemDefinition.Name = "Golden Cherry Blossom Branch";
            itemDefinition.Number = ItemNumber;
            itemDefinition.Group = ItemGroup;
            itemDefinition.Width = 1;
            itemDefinition.Height = 2;
            itemDefinition.SetGuid(itemDefinition.Group, itemDefinition.Number);
            gameConfiguration.Items.Add(itemDefinition);
        }

        // Always set it: the item is consumed to unlock an inventory extension, so it must not last more than one use.
        itemDefinition.Durability = 1;
    }
}
