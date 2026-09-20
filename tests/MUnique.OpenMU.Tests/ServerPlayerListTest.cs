// <copyright file="ServerPlayerListTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.ServerPlayerList;
using MUnique.OpenMU.GameLogic.Views.ServerPlayerList;

/// <summary>
/// Tests for the <see cref="ServerPlayerListRequestAction"/>.
/// </summary>
public class ServerPlayerListTest
{
    /// <summary>
    /// Tests that the players of the game server are reported with their level, class and map.
    /// </summary>
    [Test]
    public async ValueTask ReportsPlayersWithLevelClassAndMapAsync()
    {
        // arrange
        var gameContext = GameContextTestHelper.CreateGameContext();
        var secondMap = await AddSecondMapAsync(gameContext).ConfigureAwait(false);

        var requester = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        ConfigurePlayer(requester, "Requester", 10, 1, await gameContext.GetMapAsync(0).ConfigureAwait(false));
        await gameContext.AddPlayerAsync(requester).ConfigureAwait(false);

        var otherPlayer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        ConfigurePlayer(otherPlayer, "OtherPlayer", 350, 2, secondMap);
        await gameContext.AddPlayerAsync(otherPlayer).ConfigureAwait(false);

        var viewPlugIn = Mock.Get(requester.ViewPlugIns.GetPlugIn<IServerPlayerListViewPlugIn>()!);
        IReadOnlyList<ServerPlayerListEntry>? reportedPlayers = null;
        viewPlugIn
            .Setup(p => p.ShowServerPlayerListAsync(It.IsAny<IReadOnlyList<ServerPlayerListEntry>>()))
            .Callback<IReadOnlyList<ServerPlayerListEntry>>(players => reportedPlayers = players)
            .Returns(new ValueTask());

        // act
        await new ServerPlayerListRequestAction().RequestServerPlayerListAsync(requester).ConfigureAwait(false);

        // assert
        Assert.That(reportedPlayers, Is.Not.Null);
        Assert.That(
            reportedPlayers,
            Is.EquivalentTo(new[]
            {
                new ServerPlayerListEntry("Requester", 10, 1, 0),
                new ServerPlayerListEntry("OtherPlayer", 350, 2, 1),
            }));
    }

    /// <summary>
    /// Tests that invisible players and offline players are not reported.
    /// </summary>
    [Test]
    public async ValueTask ExcludesInvisibleAndOfflinePlayersAsync()
    {
        // arrange
        var gameContext = GameContextTestHelper.CreateGameContext();
        var map = await gameContext.GetMapAsync(0).ConfigureAwait(false);

        var requester = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        ConfigurePlayer(requester, "Requester", 10, 1, map);
        await gameContext.AddPlayerAsync(requester).ConfigureAwait(false);

        var invisiblePlayer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        ConfigurePlayer(invisiblePlayer, "Invisible", 20, 2, map);
        invisiblePlayer.Attributes!.AddElement(new ConstValueAttribute(1, Stats.IsInvisible), Stats.IsInvisible);
        await gameContext.AddPlayerAsync(invisiblePlayer).ConfigureAwait(false);

        var offlinePlayer = await PlayerTestHelper.CreateOfflineLevelingPlayerAsync(gameContext).ConfigureAwait(false);
        offlinePlayer.SelectedCharacter!.Name = "Offline";
        offlinePlayer.CurrentMap = map;
        await gameContext.AddPlayerAsync(offlinePlayer).ConfigureAwait(false);

        var viewPlugIn = Mock.Get(requester.ViewPlugIns.GetPlugIn<IServerPlayerListViewPlugIn>()!);
        IReadOnlyList<ServerPlayerListEntry>? reportedPlayers = null;
        viewPlugIn
            .Setup(p => p.ShowServerPlayerListAsync(It.IsAny<IReadOnlyList<ServerPlayerListEntry>>()))
            .Callback<IReadOnlyList<ServerPlayerListEntry>>(players => reportedPlayers = players)
            .Returns(new ValueTask());

        // act
        await new ServerPlayerListRequestAction().RequestServerPlayerListAsync(requester).ConfigureAwait(false);

        // assert
        Assert.That(reportedPlayers, Is.Not.Null);
        Assert.That(
            string.Join(", ", reportedPlayers!.Select(entry => entry.Name)),
            Is.EqualTo("Requester"),
            "Invisible players and offline players should not be reported.");
    }

    /// <summary>
    /// Tests that requests which arrive faster than the minimum interval are ignored.
    /// </summary>
    [Test]
    public async ValueTask IgnoresTooFrequentRequestsAsync()
    {
        // arrange
        var gameContext = GameContextTestHelper.CreateGameContext();
        var requester = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        ConfigurePlayer(requester, "Requester", 10, 1, await gameContext.GetMapAsync(0).ConfigureAwait(false));
        await gameContext.AddPlayerAsync(requester).ConfigureAwait(false);

        var viewPlugIn = Mock.Get(requester.ViewPlugIns.GetPlugIn<IServerPlayerListViewPlugIn>()!);
        var action = new ServerPlayerListRequestAction();

        // act
        await action.RequestServerPlayerListAsync(requester).ConfigureAwait(false);
        await action.RequestServerPlayerListAsync(requester).ConfigureAwait(false);

        // assert
        viewPlugIn.Verify(
            p => p.ShowServerPlayerListAsync(It.IsAny<IReadOnlyList<ServerPlayerListEntry>>()),
            Times.Once);
    }

    private static void ConfigurePlayer(Player player, string name, ushort level, byte classId, GameMap? map)
    {
        player.SelectedCharacter!.Name = name;
        player.SelectedCharacter.CharacterClass!.Number = classId;
        player.Attributes![Stats.Level] = level;
        player.CurrentMap = map;
    }

    private static async ValueTask<GameMap?> AddSecondMapAsync(IGameContext gameContext)
    {
        using (var persistenceContext = gameContext.PersistenceContextProvider.CreateNewContext())
        {
            var mapDefinition = persistenceContext.CreateNew<MUnique.OpenMU.Persistence.BasicModel.GameMapDefinition>();
            mapDefinition.Number = 1;
            mapDefinition.TerrainData = new byte[ushort.MaxValue + 3];
            gameContext.Configuration.Maps.Add(mapDefinition);
        }

        return await gameContext.GetMapAsync(1).ConfigureAwait(false);
    }
}
