// <copyright file="RepairSocketSphereLevelsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Repairs the levels of already mounted socket options. Previously, the mounting crafting stored the
/// level of the seed (which encodes the kind of the option) as the level of the socket option, instead
/// of the level of the sphere. As a result, the client could not calculate the option value, e.g. it
/// showed a defense increase of 30 (level 1) even for items with a sphere of level 5.
/// The original sphere level can't be recovered, so all existing socket options are set to the maximum
/// level of 5.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C4A85F31-6E29-4D7B-8B10-2F6D9E3C7A58")]
public sealed class RepairSocketSphereLevelsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Repair Socket Sphere Levels";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Sets the level of already mounted socket options to the maximum of 5, because the previously stored level was the seed option kind instead of the sphere level.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RepairSocketSphereLevels;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 19, 22, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var socketOptionTypeId = ItemOptionTypes.SocketOption.Id;
        var items = await context.GetAsync<Item>().ConfigureAwait(false);
        foreach (var item in items)
        {
            foreach (var optionLink in item.ItemOptions.Where(link => link.ItemOption?.OptionType?.Id == socketOptionTypeId))
            {
                optionLink.Level = 5;
            }
        }
    }
}
