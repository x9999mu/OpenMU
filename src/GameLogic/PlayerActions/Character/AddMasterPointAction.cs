// <copyright file="AddMasterPointAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Character;

using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Properties;

/// <summary>
/// Action to add a master skill point to learn or increase the level of a master skill.
/// </summary>
public class AddMasterPointAction
{
    private const int MinimumSkillLevelOfRequiredSkill = 10;

    /// <summary>
    /// Adds the master point.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skillId">The skill identifier.</param>
    public async ValueTask AddMasterPointAsync(Player player, ushort skillId)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        if (player.SelectedCharacter is null)
        {
            player.Logger.LogWarning("No character selected, player {0}", player);
            return;
        }

        if (player.SelectedCharacter.MasterLevelUpPoints < 1)
        {
            player.Logger.LogWarning("No free master level up point, player {0}", player);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnoughLevelUpPointsAvailable)).ConfigureAwait(false);
            return;
        }

        var skill = player.GameContext.Configuration.Skills.FirstOrDefault(s => s.Number == skillId);
        if (skill is null)
        {
            player.Logger.LogWarning("Skill {0} does not exist, player {1}", skillId, player);
            await ShowRejectedMessageAsync(player, $"skill {skillId} does not exist").ConfigureAwait(false);
            return;
        }

        if (skill.MasterDefinition is null)
        {
            player.Logger.LogWarning("Not a master skill, skillId: {0}, player {1}", skill.Number, player);
            await ShowRejectedMessageAsync(player, $"skill {skill.Number} is not a master skill").ConfigureAwait(false);
            return;
        }

        var learnedSkill = player.SelectedCharacter.LearnedSkills.FirstOrDefault(ls => ls.Skill?.Number == skillId);
        if (learnedSkill is null)
        {
            player.Logger.LogDebug("Trying to add master skill, skillId: {0}, player {1}", skill.Number, player);
            if (this.GetUnfulfilledRequirement(player, skill) is { } unfulfilledRequirement)
            {
                player.Logger.LogWarning("Master skill {0} {1} not added, player {2}: {3}", skill.Number, skill.Name, player, unfulfilledRequirement);
                await ShowRejectedMessageAsync(player, unfulfilledRequirement).ConfigureAwait(false);
                return;
            }

            player.Logger.LogDebug("Adding master skill, skillId: {0}, player {1}", skill.Number, player);
            await player.SkillList!.AddLearnedSkillAsync(skill).ConfigureAwait(false);
            learnedSkill = player.SkillList?.GetSkill(skillId);
            if (learnedSkill is { })
            {
                await this.AddMasterPointToLearnedSkillAsync(player, learnedSkill).ConfigureAwait(false);
            }
            else
            {
                player.Logger.LogDebug($"Learned Skill {skillId} not found.");
            }
        }
        else
        {
            await this.AddMasterPointToLearnedSkillAsync(player, learnedSkill).ConfigureAwait(false);
        }
    }

    private static async ValueTask ShowRejectedMessageAsync(Player player, string reason)
    {
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MasterSkillLevelUpFailed), reason).ConfigureAwait(false);
    }

    private async ValueTask AddMasterPointToLearnedSkillAsync(Player player, SkillEntry learnedSkill)
    {
        learnedSkill.ThrowNotInitializedProperty(learnedSkill.Skill is null, nameof(learnedSkill.Skill));
        var requiredPoints = learnedSkill.Level == 0 ? learnedSkill.Skill.MasterDefinition!.MinimumLevel : 1;
        if (player.SelectedCharacter!.MasterLevelUpPoints >= requiredPoints && learnedSkill.Level < learnedSkill.Skill.MasterDefinition!.MaximumLevel)
        {
            player.Logger.LogDebug("Adding {0} points to skill, skillId: {1}, player {2}", requiredPoints, learnedSkill.Skill.Number, player);
            learnedSkill.Level += requiredPoints;
            learnedSkill.PowerUpDuration = null;
            learnedSkill.PowerUps = null;

            var currentSkill = learnedSkill;
            while (player.SkillList!.Skills.FirstOrDefault(s => s.Skill?.MasterDefinition?.ReplacedSkill == currentSkill.Skill) is { } childSkill)
            {
                // Because the learned skill might have been replaced by a child skill (active), we also need to nullify the child's powerups to force an update
                childSkill.PowerUpDuration = null;
                childSkill.PowerUps = null;
                currentSkill = childSkill;
            }

            player.SelectedCharacter.MasterLevelUpPoints -= requiredPoints;
            await player.InvokeViewPlugInAsync<IMasterSkillLevelChangedPlugIn>(p => p.MasterSkillLevelChangedAsync(learnedSkill)).ConfigureAwait(false);
        }
        else
        {
            player.Logger.LogDebug("Not enough master level up points to add master points, player {0}, available {1}, required {2}", player, player.SelectedCharacter.MasterLevelUpPoints, requiredPoints);
        }
    }

    private string? GetUnfulfilledRequirement(Player player, Skill skill)
    {
        if (player.SelectedCharacter!.MasterLevelUpPoints < skill.MasterDefinition!.MinimumLevel)
        {
            return $"not enough master level up points (available {player.SelectedCharacter.MasterLevelUpPoints}, required {skill.MasterDefinition.MinimumLevel})";
        }

        if (!skill.QualifiedCharacters.Contains(player.SelectedCharacter.CharacterClass!))
        {
            return $"the character class can't learn this skill";
        }

        if (!this.CheckRank(skill.MasterDefinition, player.SelectedCharacter))
        {
            return $"a skill of the previous rank must be at level {MinimumSkillLevelOfRequiredSkill}";
        }

        if (!this.CheckRequiredSkill(skill.MasterDefinition, player))
        {
            return $"the required skills must be at level {MinimumSkillLevelOfRequiredSkill}";
        }

        return null;
    }

    private bool CheckRank(MasterSkillDefinition definition, DataModel.Entities.Character character)
    {
        if (definition.Rank <= 1)
        {
            return true;
        }

        var learnedRequiredSkills = character.LearnedSkills
            .Where(l => l.Skill?.MasterDefinition?.Root != null
                && l.Skill.MasterDefinition.Root.Id == definition.Root?.Id
                && l.Skill.MasterDefinition.Rank == definition.Rank - 1);
        return learnedRequiredSkills?.Any(lrs => lrs.Level >= MinimumSkillLevelOfRequiredSkill) ?? false;
    }

    private bool CheckRequiredSkill(MasterSkillDefinition definition, Player player)
    {
        var result = true;
        if (definition.RequiredMasterSkills is not null && definition.RequiredMasterSkills.Any())
        {
            result = definition.RequiredMasterSkills.All(s =>
                player.SelectedCharacter!.LearnedSkills.Any(learned => learned.Skill == s && learned.Level >= MinimumSkillLevelOfRequiredSkill)
                || (s.MasterDefinition is null && player.SkillList!.ContainsSkill((ushort)s.Number)));
        }

        return result;
    }
}
