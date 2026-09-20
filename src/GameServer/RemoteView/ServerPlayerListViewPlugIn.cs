// <copyright file="ServerPlayerListViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.ServerPlayerList;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="IServerPlayerListViewPlugIn"/> which is forwarding everything
/// to the game client with specific data packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ServerPlayerListViewPlugIn_Name), Description = nameof(PlugInResources.ServerPlayerListViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("F1A9D799-89D0-43C6-8199-51BB382B82A3")]
public class ServerPlayerListViewPlugIn : IServerPlayerListViewPlugIn
{
    /// <summary>
    /// The maximum number of players which fit into a single packet. The header and the fields before
    /// the player list take 7 bytes, and a packet of the C1 header type must not exceed 255 bytes.
    /// </summary>
    private static readonly int MaximumPlayersPerChunk = (255 - 7) / ServerPlayerListRef.ServerPlayerRef.Length;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerPlayerListViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ServerPlayerListViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowServerPlayerListAsync(IReadOnlyList<ServerPlayerListEntry> players)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var totalChunks = Math.Max(1, (players.Count + MaximumPlayersPerChunk - 1) / MaximumPlayersPerChunk);
        for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            var offset = chunkIndex * MaximumPlayersPerChunk;
            var count = Math.Min(MaximumPlayersPerChunk, players.Count - offset);

            int Write()
            {
                var size = ServerPlayerListRef.GetRequiredSize(count);
                var span = connection.Output.GetSpan(size)[..size];
                var packet = new ServerPlayerListRef(span)
                {
                    ChunkIndex = (byte)chunkIndex,
                    TotalChunks = (byte)totalChunks,
                    Count = (byte)count,
                };

                for (var i = 0; i < count; i++)
                {
                    var entry = players[offset + i];
                    var playerBlock = packet[i];
                    playerBlock.Name = entry.Name;
                    playerBlock.Level = entry.Level;
                    playerBlock.ClassId = entry.ClassId;
                    playerBlock.MapId = entry.MapId;
                }

                return size;
            }

            await connection.SendAsync(Write).ConfigureAwait(false);
        }
    }
}
