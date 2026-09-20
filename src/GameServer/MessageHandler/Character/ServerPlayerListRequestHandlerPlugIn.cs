// <copyright file="ServerPlayerListRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Character;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.ServerPlayerList;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for server player list request packets (0xF3, 0x60 identifier).
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ServerPlayerListRequestHandlerPlugIn_Name), Description = nameof(PlugInResources.ServerPlayerListRequestHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("2944997F-B713-4D64-B89F-A76F9C861B5F")]
[BelongsToGroup(CharacterGroupHandlerPlugIn.GroupKey)]
internal class ServerPlayerListRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly ServerPlayerListRequestAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x60;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        await this._action.RequestServerPlayerListAsync(player).ConfigureAwait(false);
    }
}
