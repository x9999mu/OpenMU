// <copyright file="AddRageFighterAncientSetsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the ancient sets of the Rage Fighter's Sacred items, which already exist in the
/// game client but were missing on the server.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C1514A69-DDFF-4E1E-86F1-FB57B27CD03A")]
public sealed class AddRageFighterAncientSetsPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Add Rage Fighter ancient sets";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds the ancient sets 'Vega' and 'Chamer' of the Rage Fighter's Sacred glove and armor pieces.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddRageFighterAncientSets;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new AncientSets(context, gameConfiguration).AddRageFighterAncientSets();
        return default;
    }
}
