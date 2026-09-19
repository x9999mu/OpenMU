// <copyright file="ConfigureFireSocketOptionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Raises the damage options of the fire socket options, because the original values are
/// negligible compared to the damage which is dealt on this server:
/// the level based option uses the divisors 5/4/3/2/1 (damage = level / divisor) and the flat
/// damage options are scaled by 30.
/// The client shows the same values; they are patched in
/// <c>Data/Local/&lt;Language&gt;/SocketItem_&lt;Language&gt;.bmd</c> with <c>tools/patch_socket_option.py</c>.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("4C7E1A93-52B8-4D6F-8E31-7A9D0C5B2F84")]
public class ConfigureFireSocketOptionsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Increase the damage options of the fire socket options";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Uses the divisors 5/4/3/2/1 for the level based fire option and scales the flat damage options by 30, matching the client data.";

    /// <summary>
    /// The new multipliers of the level based option (damage = level / divisor).
    /// </summary>
    private static readonly float[] LevelBasedDivisors = [1f / 5f, 1f / 4f, 1f / 3f, 1f / 2f, 1f];

    /// <summary>
    /// The new values of the flat damage options, by their sub option number.
    /// </summary>
    private static readonly Dictionary<int, float[]> FlatOptions = new()
    {
        { 2, [900f, 960f, 1050f, 1200f, 1500f] },
        { 3, [600f, 660f, 750f, 900f, 1050f] },
        { 4, [600f, 660f, 750f, 900f, 1050f] },
    };

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureFireSocketOptions;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 20, 8, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var fireOptions = gameConfiguration.ItemOptions
            .Where(definition => definition.PossibleOptions.Any(option => option.OptionType == ItemOptionTypes.SocketOption
                                                                          && option.SubOptionType == (int)SocketSubOptionType.Fire))
            .SelectMany(definition => definition.PossibleOptions)
            .Where(option => option.OptionType == ItemOptionTypes.SocketOption
                             && option.SubOptionType == (int)SocketSubOptionType.Fire)
            .ToList();

        foreach (var option in fireOptions)
        {
            var values = GetNewValues(option);
            if (values is null)
            {
                continue;
            }

            foreach (var optionOfLevel in option.LevelDependentOptions)
            {
                if (optionOfLevel.Level < 1 || optionOfLevel.Level > values.Length)
                {
                    continue;
                }

                if (optionOfLevel.PowerUpDefinition?.Boost is { } boost)
                {
                    if (boost.ConstantValue is { } constantValue)
                    {
                        constantValue.Value = values[optionOfLevel.Level - 1];
                    }

                    foreach (var relationship in boost.RelatedValues)
                    {
                        relationship.InputOperand = values[optionOfLevel.Level - 1];
                    }
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private static float[]? GetNewValues(IncreasableItemOption option)
    {
        if (FlatOptions.TryGetValue(option.Number, out var flatValues))
        {
            return flatValues;
        }

        // The level based option is the first one; it multiplies the character level.
        return option.Number == 0 ? LevelBasedDivisors : null;
    }
}
