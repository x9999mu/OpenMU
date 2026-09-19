// <copyright file="AddSeedMasterToNoriaUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the Seed Master to Noria.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("CD78E500-700B-4998-8AE1-1591F70F1834")]
public sealed class AddSeedMasterToNoriaUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Add Seed Master to Noria";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds the Seed Master to Noria.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddSeedMasterToNoria;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var noria = gameConfiguration.Maps.First(map => map.Number == 3);
        if (noria.MonsterSpawns.Any(spawn => spawn.MonsterDefinition?.Number == 452))
        {
            return ValueTask.CompletedTask;
        }

        var seedMaster = gameConfiguration.Monsters.Single(monster => monster.Number == 452);
        var spawn = context.CreateNew<MonsterSpawnArea>();
        spawn.SetGuid(noria.Number, 16);
        spawn.GameMap = noria;
        spawn.MonsterDefinition = seedMaster;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.Quantity = 1;
        spawn.Direction = Direction.SouthWest;
        spawn.X1 = 171;
        spawn.X2 = 171;
        spawn.Y1 = 121;
        spawn.Y2 = 121;
        noria.MonsterSpawns.Add(spawn);

        return ValueTask.CompletedTask;
    }
}
