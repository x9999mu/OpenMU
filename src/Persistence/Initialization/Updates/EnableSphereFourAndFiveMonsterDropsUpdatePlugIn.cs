// <copyright file="EnableSphereFourAndFiveMonsterDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables the fourth and fifth sphere levels to drop from monsters.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C520B132-BA94-4CB2-B6C1-833F2F787E2A")]
public sealed class EnableSphereFourAndFiveMonsterDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Enable Sphere (4) and Sphere (5) Monster Drops";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Enables Sphere (4) and Sphere (5) monster drops, with dedicated 2% drop groups starting at monster levels 142 and 145.";

    private const double SphereDropChance = 0.02;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.EnableSphereFourAndFiveMonsterDrops;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var sphere4 = GetSphere(gameConfiguration, 73);
        var sphere5 = GetSphere(gameConfiguration, 74);

        EnableMonsterDrop(sphere4, 142);
        EnableMonsterDrop(sphere5, 145);

        EnsureDropGroup(context, gameConfiguration, sphere4, 142);
        EnsureDropGroup(context, gameConfiguration, sphere5, 145);

        return ValueTask.CompletedTask;
    }

    private static ItemDefinition GetSphere(GameConfiguration gameConfiguration, short number)
        => gameConfiguration.Items.Single(item => item is { Group: 12 } && item.Number == number);

    private static void EnableMonsterDrop(ItemDefinition sphere, byte dropLevel)
    {
        sphere.DropLevel = dropLevel;
        sphere.DropsFromMonsters = true;
    }

    private static void EnsureDropGroup(IContext context, GameConfiguration gameConfiguration, ItemDefinition sphere, byte minimumMonsterLevel)
    {
        var groupId = GuidHelper.CreateGuid<DropItemGroup>((short)sphere.Group, sphere.Number, 147);
        var dropGroup = gameConfiguration.DropItemGroups.FirstOrDefault(group => group.GetId() == groupId);
        if (dropGroup is null)
        {
            dropGroup = context.CreateNew<DropItemGroup>();
            dropGroup.SetGuid(groupId);
            gameConfiguration.DropItemGroups.Add(dropGroup);
        }

        dropGroup.Description = $"The improved drop item group for {sphere.Name}";
        dropGroup.Chance = SphereDropChance;
        dropGroup.MinimumMonsterLevel = minimumMonsterLevel;
        dropGroup.MaximumMonsterLevel = byte.MaxValue;
        if (dropGroup.PossibleItems.All(item => item.GetId() != sphere.GetId()))
        {
            dropGroup.PossibleItems.Add(sphere);
        }

        foreach (var map in gameConfiguration.Maps)
        {
            if (map.DropItemGroups.All(group => group.GetId() != dropGroup.GetId()))
            {
                map.DropItemGroups.Add(dropGroup);
            }
        }
    }
}
