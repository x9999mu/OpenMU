// <copyright file="ServerPlayerListEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.ServerPlayerList;

/// <summary>
/// An entry of the list of players which are currently online on the game server.
/// </summary>
/// <param name="Name">The name of the character.</param>
/// <param name="Level">The level of the character.</param>
/// <param name="ClassId">The number of the character class.</param>
/// <param name="MapId">The number of the map on which the character currently is, or <c>0</c> if it's not on a map.</param>
public sealed record ServerPlayerListEntry(string Name, ushort Level, byte ClassId, ushort MapId);
