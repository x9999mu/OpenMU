// <copyright file="EnableBotFeatureUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Bots;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds and enables the server-side bot feature with a conservative initial population.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2FA4248F-3A02-49B9-8F59-ADB40E447B4D")]
public sealed class EnableBotFeatureUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Enable Bot Feature";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Adds and enables server-side bots with a conservative initial population.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.EnableBotFeature;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var typeId = typeof(BotFeaturePlugIn).GUID;
        var plugInConfiguration = gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeId);
        if (plugInConfiguration is null)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        var configuration = plugInConfiguration.GetConfiguration<BotConfiguration>(null) ?? new BotConfiguration();
        if (!configuration.Enabled)
        {
            configuration.NumberOfAccounts = 2;
            configuration.MaxCharactersPerAccount = 1;
            configuration.BotCapacityPercent = 20;
            configuration.PresenceRotation = false;
            configuration.StartAsFreshCharacters = true;
            configuration.Enabled = true;
        }
        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(configuration, null);

        return ValueTask.CompletedTask;
    }
}
