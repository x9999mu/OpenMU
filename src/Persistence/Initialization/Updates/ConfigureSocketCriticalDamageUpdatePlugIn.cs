// <copyright file="ConfigureSocketCriticalDamageUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Raises the flat critical damage bonus of the lightning socket option.
/// The bonus is added as flat damage on critical hits, so the original values were negligible
/// compared to the damage which is dealt on this server. The client shows the same values; they
/// are patched in <c>Data/Local/&lt;Language&gt;/SocketItem_&lt;Language&gt;.bmd</c> with
/// <c>tools/patch_socket_option.py</c>.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("9F2B7C41-6D58-4B3E-9A17-2E5C8D4F1B06")]
public class ConfigureSocketCriticalDamageUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Increase the critical damage of the lightning socket option";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Sets the flat critical damage bonus of the lightning socket option to 1000/2000/3000/4000/5000, matching the client data.";

    /// <summary>
    /// The sub option number of the critical damage bonus inside the lightning option group.
    /// </summary>
    private const int CriticalDamageSubOptionNumber = 2;

    private static readonly float[] Values = [1000f, 2000f, 3000f, 4000f, 5000f];

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ConfigureSocketCriticalDamage;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 20, 6, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var criticalDamageOption = gameConfiguration.ItemOptions
            .SelectMany(definition => definition.PossibleOptions)
            .FirstOrDefault(option => option.OptionType == ItemOptionTypes.SocketOption
                                      && option.SubOptionType == (int)SocketSubOptionType.Lightning
                                      && option.Number == CriticalDamageSubOptionNumber);
        if (criticalDamageOption is null)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var optionOfLevel in criticalDamageOption.LevelDependentOptions)
        {
            if (optionOfLevel.Level < 1 || optionOfLevel.Level > Values.Length)
            {
                continue;
            }

            if (optionOfLevel.PowerUpDefinition?.Boost?.ConstantValue is { } constantValue)
            {
                constantValue.Value = Values[optionOfLevel.Level - 1];
            }
        }

        return ValueTask.CompletedTask;
    }
}
