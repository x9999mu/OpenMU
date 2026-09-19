// <copyright file="ConfigureRemainingSocketOptionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Raises the remaining socket options which are negligible compared to the values of this
/// server: the flat damage, defense, recovery and health values are multiplied by 25.
/// The client shows the same values; they are patched in
/// <c>Data/Local/&lt;Language&gt;/SocketItem_&lt;Language&gt;.bmd</c> with <c>tools/patch_socket_option.py</c>.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("1B6F0D74-3C9A-4E52-8F18-6D2A5E9B7C41")]
public class ConfigureRemainingSocketOptionsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Increase the remaining socket options";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Multiplies the flat damage, defense, recovery and health values of the socket options by 25, matching the client data.";

    /// <summary>
    /// The new values by socket sub option type and option number.
    /// </summary>
    private static readonly Dictionary<(SocketSubOptionType SubOption, int Number), float[]> NewValues = new()
    {
        { (SocketSubOptionType.Ice, 2), [925f, 1000f, 1125f, 1250f, 1500f] },      // skill damage
        { (SocketSubOptionType.Ice, 3), [625f, 675f, 750f, 875f, 1000f] },          // attack rate
        { (SocketSubOptionType.Water, 1), [750f, 825f, 900f, 975f, 1050f] },        // defense
        { (SocketSubOptionType.Lightning, 0), [375f, 500f, 625f, 750f, 1000f] },    // excellent damage
        { (SocketSubOptionType.Earth, 0), [750f, 800f, 850f, 900f, 950f] },         // maximum health
        { (SocketSubOptionType.Wind, 0), [200f, 250f, 325f, 400f, 500f] },          // health recovery
        { (SocketSubOptionType.Wind, 3), [175f, 350f, 525f, 700f, 875f] },          // mana recovery
        { (SocketSubOptionType.Wind, 4), [625f, 750f, 875f, 1000f, 1250f] },        // maximum ability
        { (SocketSubOptionType.Wind, 5), [75f, 125f, 175f, 250f, 375f] },           // ability recovery
    };

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureRemainingSocketOptions;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 20, 9, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var socketOptions = gameConfiguration.ItemOptions
            .SelectMany(definition => definition.PossibleOptions)
            .Where(option => option.OptionType == ItemOptionTypes.SocketOption)
            .ToList();

        foreach (var option in socketOptions)
        {
            if (!NewValues.TryGetValue(((SocketSubOptionType)option.SubOptionType, option.Number), out var values))
            {
                continue;
            }

            foreach (var optionOfLevel in option.LevelDependentOptions)
            {
                if (optionOfLevel.Level < 1 || optionOfLevel.Level > values.Length)
                {
                    continue;
                }

                if (optionOfLevel.PowerUpDefinition?.Boost?.ConstantValue is { } constantValue)
                {
                    constantValue.Value = values[optionOfLevel.Level - 1];
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}
