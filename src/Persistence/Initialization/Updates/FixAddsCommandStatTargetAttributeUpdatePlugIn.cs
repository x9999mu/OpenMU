// <copyright file="FixAddsCommandStatTargetAttributeUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes the target attribute of the Dark Lord master skill 'Adds Command Stat'. It previously targeted
/// the base leadership stat, which is not a composable attribute. Characters which learned that skill
/// could not enter the world anymore, because the skill list tried to add a power up to it.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7D3E6B21-8C54-4A1F-9E72-5B0D8F4A6C33")]
public sealed class FixAddsCommandStatTargetAttributeUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Fix Adds Command Stat Target Attribute";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Changes the target attribute of the 'Adds Command Stat' master skill from Base Leadership to Total Leadership, so characters which learned it can enter the game again.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixAddsCommandStatTargetAttribute;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 19, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var skill = gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.AddsCommandStat);
        var targetAttribute = gameConfiguration.Attributes.FirstOrDefault(a => a.Id == Stats.TotalLeadership.Id);
        if (skill?.MasterDefinition is { } masterDefinition
            && targetAttribute is not null
            && masterDefinition.TargetAttribute?.Id != targetAttribute.Id)
        {
            masterDefinition.TargetAttribute = targetAttribute;
        }

        return ValueTask.CompletedTask;
    }
}
