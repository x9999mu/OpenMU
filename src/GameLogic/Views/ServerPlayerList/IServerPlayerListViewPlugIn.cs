// <copyright file="IServerPlayerListViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.ServerPlayerList;

/// <summary>
/// Interface of a view which shows the list of the players which are online on the game server.
/// </summary>
public interface IServerPlayerListViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the players which are currently online on the game server.
    /// </summary>
    /// <param name="players">The players which are online on the same game server.</param>
    ValueTask ShowServerPlayerListAsync(IReadOnlyList<ServerPlayerListEntry> players);
}
