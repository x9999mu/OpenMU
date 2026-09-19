// <copyright file="ConfigureKalimaSevenBossAncientDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the Kalima 7 boss equipment reward from full-excellent equipment to Ancient Set equipment.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D6C2A8F4-1E73-4B95-9A0D-6F84C2B7E531")]
public sealed class ConfigureKalimaSevenBossAncientDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Configure Kalima 7 Ancient Boss Drop";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Changes the Illusion of Kundun 7 equipment reward to a random Ancient Set item.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureKalimaSevenBossAncientDrops;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 19, 23, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        InstantServerConfiguration.ConfigureKalimaSevenBossDrops(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
