// <copyright file="FixRageFighterSacredBootsDiscriminatorPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update fixes the ancient discriminator of the Rage Fighter's Sacred boots.
/// </summary>
/// <remarks>
/// The game client knows the Sacred boots only as a part of the Chamer set, and it registered them
/// with the first ancient discriminator, because they don't belong to any other ancient set. So the
/// server has to send them with the discriminator 1, otherwise the client doesn't count them to the
/// set and never shows the complete set bonus.
/// </remarks>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7F0A3D5C-6B24-4C8E-9E7B-1A53C0A9D4E6")]
public sealed class FixRageFighterSacredBootsDiscriminatorPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Fix Rage Fighter Sacred boots discriminator";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Gives the Sacred boots of the Chamer set the ancient discriminator which the game client expects.";

    /// <summary>
    /// The name of the ancient set which contains the Sacred boots.
    /// </summary>
    internal const string SetName = "Chamer";

    /// <summary>
    /// The discriminator which the game client expects for the Sacred boots.
    /// </summary>
    internal const int Discriminator = 1;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixRageFighterSacredBootsDiscriminator;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 20, 17, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var set = gameConfiguration.ItemSetGroups.FirstOrDefault(itemSetGroup => itemSetGroup.Name == SetName);
        var boots = set?.Items.FirstOrDefault(item => item.ItemDefinition?.Group == (byte)ItemGroups.Boots
            && item.ItemDefinition?.Number == 59);
        if (boots is not null && boots.AncientSetDiscriminator != Discriminator)
        {
            boots.AncientSetDiscriminator = Discriminator;
        }

        return default;
    }
}
