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

    /// <summary>
    /// The time for which a built list is reused for all requesting players. Building the list
    /// iterates all players, so without this every player who opens the window would cause its
    /// own pass over all players. The client refreshes every few seconds, so a list which is
    /// slightly behind is not a problem.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(2);

    private readonly ConditionalWeakTable<Player, RequestState> _requestStates = new();
    private readonly object _cacheLock = new();
    private IReadOnlyList<ServerPlayerListEntry>? _cachedEntries;
    private DateTime _cacheExpiry;

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

        var entries = await this.GetOnlinePlayersAsync(player.GameContext).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IServerPlayerListViewPlugIn>(p => p.ShowServerPlayerListAsync(entries)).ConfigureAwait(false);
    }

    private static async ValueTask<IReadOnlyList<ServerPlayerListEntry>> BuildOnlinePlayerListAsync(IGameContext gameContext)
    {
        var entries = new ConcurrentBag<ServerPlayerListEntry>();
        await gameContext.ForEachPlayerAsync(
            p =>
            {
                if (TryCreateEntry(p, out var entry))
                {
                    entries.Add(entry);
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);

        return entries
            .OrderBy(entry => entry.MapId)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async ValueTask<IReadOnlyList<ServerPlayerListEntry>> GetOnlinePlayersAsync(IGameContext gameContext)
    {
        lock (this._cacheLock)
        {
            if (this._cachedEntries is { } cachedEntries && DateTime.UtcNow < this._cacheExpiry)
            {
                return cachedEntries;
            }
        }

        var entries = await BuildOnlinePlayerListAsync(gameContext).ConfigureAwait(false);
        lock (this._cacheLock)
        {
            this._cachedEntries = entries;
            this._cacheExpiry = DateTime.UtcNow + CacheDuration;
        }

        return entries;
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
