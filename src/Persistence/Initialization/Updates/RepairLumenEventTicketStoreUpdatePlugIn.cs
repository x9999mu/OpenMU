// <copyright file="RepairLumenEventTicketStoreUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Repairs Lumen the Barmaid's event-ticket store so its multi-cell tickets fit into the merchant inventory.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C84E1A5F-7B93-4D26-8F0E-2A6D9C3B5E71")]
public sealed class RepairLumenEventTicketStoreUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The update name.
    /// </summary>
    internal const string PlugInName = "Repair Lumen Event Ticket Store";

    /// <summary>
    /// The update description.
    /// </summary>
    internal const string PlugInDescription = "Repacks Lumen's event tickets to fit the merchant store.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RepairLumenEventTicketStore;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
        => ConfigureLumenEventTicketsUpdatePlugIn.ConfigureStoreAsync(context, gameConfiguration);
}
