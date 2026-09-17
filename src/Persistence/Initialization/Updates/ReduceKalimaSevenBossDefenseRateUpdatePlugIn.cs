// <copyright file="ReduceKalimaSevenBossDefenseRateUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Lowers the defense rate of the Illusion of Kundun 7, so that it can be hit by regular attack rates.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("3C7D1E92-8A4B-4F60-9D25-6B1E7C40A8D3")]
public sealed class ReduceKalimaSevenBossDefenseRateUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Reduce Kalima 7 Boss Defense Rate";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Lowers the defense rate (PvM) of the Illusion of Kundun 7 from 48000 to 10000. An attack rate of 20000 to 30000 therefore hits it with a chance of 50 to 67 percent instead of the minimum of 3 percent.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ReduceKalimaSevenBossDefenseRate;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        InstantServerConfiguration.ConfigureKalimaSevenBossDefenseRate(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
