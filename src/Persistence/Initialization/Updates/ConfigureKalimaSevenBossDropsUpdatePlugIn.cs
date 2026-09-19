// <copyright file="ConfigureKalimaSevenBossDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Configures the loot of the Illusion of Kundun 7.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7B4E1A62-9C3D-4F58-8E21-5A6D2F0B7C94")]
public sealed class ConfigureKalimaSevenBossDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure Kalima 7 Boss Drops";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Makes the Illusion of Kundun 7 drop three GM Gifts, three Box of Kundun +5, one Jewel of Harmony, one Jewel of Guardian and one random full-option item.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureKalimaSevenBossDrops;

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
        InstantServerConfiguration.ConfigureKalimaSevenBossDrops(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
