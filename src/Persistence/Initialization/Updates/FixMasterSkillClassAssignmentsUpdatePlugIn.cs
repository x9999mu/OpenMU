// <copyright file="FixMasterSkillClassAssignmentsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes the character classes of the master skills which were previously restricted to the lord emperor,
/// even though the client master skill tree contains them for other classes, too.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("3C93AE44-4369-46E2-9AD3-A877C7710CB6")]
public sealed class FixMasterSkillClassAssignmentsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Fix Master Skill Class Assignments";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Allows all character classes to learn the master skills which are part of their client master skill tree, e.g. Maximum Attack Power Increase, Restores Full SD and Increase Ignore Defense Rate.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMasterSkillClassAssignments;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 18, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new SkillsInitializer(context, gameConfiguration).AddClientMasterSkillsAndDefinitions();
        return ValueTask.CompletedTask;
    }
}
