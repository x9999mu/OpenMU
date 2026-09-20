// <copyright file="ServerPlayerListRequestAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ServerPlayerList;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.Views.ServerPlayerList;

/// <summary>
/// Action which determines the list of the players which are online on the game server.
/// </summary>
public class ServerPlayerListRequestAction
{
    /// <summary>
    /// The minimum time between two requests of the same player. The client refreshes the list
    /// periodically while its window is open, so requests which arrive faster than this are ignored.
    /// </summary>
    private static readonly TimeSpan MinimumRequestInterval = TimeSpan.FromSeconds(1);

    private readonly ConditionalWeakTable<Player, RequestState> _requestStates = new();

    /// <summary>
    /// Requests the list of the players which are online on the same game server.
    /// </summary>
    /// <param name="player">The player which requests the list.</param>
    public async ValueTask RequestServerPlayerListAsync(Player player)
    {
        if (!this.IsRequestAllowed(player))
        {
            return;
        }

        var entries = new ConcurrentBag<ServerPlayerListEntry>();
        await player.GameContext.ForEachPlayerAsync(
            p =>
            {
                if (TryCreateEntry(p, out var entry))
                {
                    entries.Add(entry);
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);

        var orderedEntries = entries
            .OrderBy(entry => entry.MapId)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await player.InvokeViewPlugInAsync<IServerPlayerListViewPlugIn>(p => p.ShowServerPlayerListAsync(orderedEntries)).ConfigureAwait(false);
    }

    private static bool TryCreateEntry(Player player, out ServerPlayerListEntry entry)
    {
        entry = null!;

        // Invisible players (game masters which used the hide command, or duel spectators) and
        // offline players (offline leveling and bots) should not show up in the list.
        if (player.IsInvisible || player is OfflinePlayer)
        {
            return false;
        }

        if (player.SelectedCharacter is not { } character
            || string.IsNullOrEmpty(character.Name)
            || character.CharacterClass is not { } characterClass)
        {
            return false;
        }

        var level = (ushort)Math.Clamp(player.Level, 0, ushort.MaxValue);
        entry = new ServerPlayerListEntry(character.Name, level, characterClass.Number, player.CurrentMap?.MapId ?? 0);
        return true;
    }

    private bool IsRequestAllowed(Player player)
    {
        var state = this._requestStates.GetOrCreateValue(player);
        lock (state)
        {
            var now = DateTime.UtcNow;
            if (now - state.LastRequest < MinimumRequestInterval)
            {
                return false;
            }

            state.LastRequest = now;
            return true;
        }
    }

    private sealed class RequestState
    {
        public DateTime LastRequest { get; set; } = DateTime.MinValue;
    }
}
