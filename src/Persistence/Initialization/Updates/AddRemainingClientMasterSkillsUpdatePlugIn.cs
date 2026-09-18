// <copyright file="AddRemainingClientMasterSkillsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the remaining master skill nodes which are present in the supported client master skill tree.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D8B6A9E4-2F71-4C63-9B5A-7E1D3C8F4A92")]
public sealed class AddRemainingClientMasterSkillsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Add Remaining Client Master Skill Nodes";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds the remaining master skill tree nodes which are present in the client but were missing on the server.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddRemainingClientMasterSkills;

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
        new SkillsInitializer(context, gameConfiguration).AddClientMasterSkillsAndDefinitions();
        return ValueTask.CompletedTask;
    }
}
