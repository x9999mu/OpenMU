// <copyright file="BackupInventoryTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlayerActions.Character;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests around <see cref="Player.BackupInventory"/>, which is created when a dialog with crafting
/// capabilities is opened. Its restore must only return the items which are still placed in the
/// dialog - never the progress which the player made in the meantime.
/// </summary>
[TestFixture]
public class BackupInventoryTests
{
    /// <summary>
    /// Verifies that items gained after the crafting dialog was closed survive the next logout.
    /// </summary>
    [Test]
    public async ValueTask ItemsGainedAfterClosedCraftingDialogSurviveLogoutAsync()
    {
        var player = await CreateTestPlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;

        await OpenCraftingDialogAsync(player).ConfigureAwait(false);
        await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);

        var harvested = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(20, harvested).ConfigureAwait(false);

        await RelogAsync(player, character).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Has.Exactly(1).SameAs(harvested));
    }

    /// <summary>
    /// Verifies that the item which the player put into the crafting window is returned to the
    /// inventory when the connection drops while the dialog is still open.
    /// </summary>
    [Test]
    public async ValueTask ItemPlacedInCraftingWindowIsReturnedOnDisconnectAsync()
    {
        var player = await CreateTestPlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        var item = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(21, item).ConfigureAwait(false);

        await OpenCraftingDialogAsync(player).ConfigureAwait(false);
        await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
        await player.TemporaryStorage!.AddItemAsync(item).ConfigureAwait(false);

        await RelogAsync(player, character).ConfigureAwait(false);

        Assert.That(player.Inventory!.GetItem(21), Is.SameAs(item));
    }

    /// <summary>
    /// Verifies that rearranging the inventory while the dialog is open is not rolled back either.
    /// </summary>
    [Test]
    public async ValueTask InventoryRearrangementWhileDialogWasOpenIsKeptAsync()
    {
        var player = await CreateTestPlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        var item = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(21, item).ConfigureAwait(false);

        await OpenCraftingDialogAsync(player).ConfigureAwait(false);
        await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
        await player.Inventory.AddItemAsync(30, item).ConfigureAwait(false);

        await RelogAsync(player, character).ConfigureAwait(false);

        Assert.That(player.Inventory!.GetItem(30), Is.SameAs(item));
        Assert.That(player.Inventory.GetItem(21), Is.Null);
    }

    /// <summary>
    /// Verifies that items consumed while the dialog was open are not resurrected by the restore.
    /// </summary>
    [Test]
    public async ValueTask ItemsConsumedWhileDialogWasOpenAreNotRestoredAsync()
    {
        var player = await CreateTestPlayerAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        var ingredient = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(22, ingredient).ConfigureAwait(false);

        await OpenCraftingDialogAsync(player).ConfigureAwait(false);

        // The crafting handlers delete consumed items from the persistence context.
        await player.Inventory.RemoveItemAsync(ingredient).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(ingredient).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);

        await RelogAsync(player, character).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Is.Empty);
    }

    /// <summary>
    /// Verifies that repeatedly switching between the characters of an account doesn't roll back
    /// the inventory of the character which is left. Each switch follows a closed crafting dialog,
    /// like a player who visits a crafting NPC and keeps playing afterwards.
    /// </summary>
    [Test]
    public async ValueTask RepeatedCharacterSwitchingKeepsInventoryOfBothCharactersAsync()
    {
        var player = await CreateTestPlayerAsync().ConfigureAwait(false);
        var firstCharacter = player.SelectedCharacter!;
        var secondCharacter = CreateSecondCharacter(player);
        Mock.Get(player.Account!).Setup(a => a.Characters).Returns(new List<Character> { firstCharacter, secondCharacter });

        // Progress of the first character, made after a crafting dialog was opened and closed.
        await OpenCraftingDialogAsync(player).ConfigureAwait(false);
        await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);
        var firstItem = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(20, firstItem).ConfigureAwait(false);

        // Progress of the second character, made the same way.
        await SwitchCharacterAsync(player, secondCharacter.Name).ConfigureAwait(false);
        await OpenCraftingDialogAsync(player).ConfigureAwait(false);
        await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);
        var secondItem = CreateItem(CreateDefinition());
        await player.Inventory!.AddItemAsync(21, secondItem).ConfigureAwait(false);

        // Switching back and forth, again with a closed crafting dialog in between.
        for (var i = 0; i < 3; i++)
        {
            await OpenCraftingDialogAsync(player).ConfigureAwait(false);
            await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);
            await SwitchCharacterAsync(player, firstCharacter.Name).ConfigureAwait(false);

            await OpenCraftingDialogAsync(player).ConfigureAwait(false);
            await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);
            await SwitchCharacterAsync(player, secondCharacter.Name).ConfigureAwait(false);
        }

        Assert.That(player.SelectedCharacter, Is.SameAs(secondCharacter));
        Assert.That(firstCharacter.Inventory!.Items, Has.Exactly(1).SameAs(firstItem));
        Assert.That(player.Inventory!.GetItem(21), Is.SameAs(secondItem));
    }

    private static Character CreateSecondCharacter(Player player)
    {
        var firstCharacter = player.SelectedCharacter!;
        var characterMock = new Mock<Character>();
        characterMock.SetupAllProperties();
        characterMock.Setup(c => c.LearnedSkills).Returns(new List<SkillEntry>());
        characterMock.Setup(c => c.Attributes).Returns(new List<StatAttribute>());
        characterMock.Setup(c => c.DropItemGroups).Returns(new List<DropItemGroup>());
        var inventoryMock = new Mock<ItemStorage>();
        inventoryMock.SetupAllProperties();
        inventoryMock.Setup(i => i.Items).Returns(new List<Item>());
        var character = characterMock.Object;
        character.Name = "SecondCharacter";
        character.CharacterSlot = 1;
        character.CurrentMap = firstCharacter.CurrentMap;
        character.CharacterClass = firstCharacter.CharacterClass;
        character.Inventory = inventoryMock.Object;
        return character;
    }

    private static async ValueTask SwitchCharacterAsync(Player player, string characterName)
    {
        await new LogoutAction().LogoutAsync(player, LogoutType.BackToCharacterSelection).ConfigureAwait(false);
        await new RequestCharacterListAction().RequestCharacterListAsync(player).ConfigureAwait(false);
        await new SelectCharacterAction().SelectCharacterAsync(player, characterName).ConfigureAwait(false);
        await player.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
        Assert.That(player.SelectedCharacter?.Name, Is.EqualTo(characterName));
    }

    private static async ValueTask<Player> CreateTestPlayerAsync()
    {
        var gameConfig = new Mock<GameConfiguration>();
        gameConfig.SetupAllProperties();
        gameConfig.Setup(c => c.Maps).Returns(new List<GameMapDefinition>());
        gameConfig.Setup(c => c.Items).Returns(new List<ItemDefinition>());
        gameConfig.Setup(c => c.Skills).Returns(new List<Skill>());
        gameConfig.Setup(c => c.PlugInConfigurations).Returns(new List<PlugInConfiguration>());
        gameConfig.Setup(c => c.CharacterClasses).Returns(new List<CharacterClass>());
        gameConfig.Setup(c => c.Attributes).Returns(new List<AttributeDefinition>());
        gameConfig.Setup(c => c.GlobalAttributeCombinations).Returns(new List<AttributeRelationship>());
        gameConfig.Setup(c => c.GlobalBaseAttributeValues).Returns(new List<ConstValueAttribute>
        {
            new(1, Stats.MoneyAmountRate),
        });
        var map = new Mock<GameMapDefinition>();
        map.SetupAllProperties();
        map.Setup(m => m.DropItemGroups).Returns(new List<DropItemGroup>());
        map.Setup(m => m.MonsterSpawns).Returns(new List<MonsterSpawnArea>());
        map.Object.TerrainData = new byte[ushort.MaxValue + 3];
        gameConfig.Object.RecoveryInterval = int.MaxValue;
        gameConfig.Object.Maps.Add(map.Object);

        var mapInitializer = new MapInitializer(gameConfig.Object, new NullLogger<MapInitializer>(), NullDropGenerator.Instance, null);
        var gameContext = new GameContext(
            gameConfig.Object,
            new InMemoryPersistenceContextProvider(),
            mapInitializer,
            new NullLoggerFactory(),
            new PlugInManager(null, new NullLoggerFactory(), null, null),
            NullDropGenerator.Instance,
            new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameContext.PlugInManager;
        mapInitializer.PathFinderPool = gameContext.PathFinderPool;
        return await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
    }

    private static async ValueTask OpenCraftingDialogAsync(Player player)
    {
        using var context = player.GameContext.PersistenceContextProvider.CreateNewConfigurationContext();
        var definition = context.CreateNew<MonsterDefinition>();
        definition.NpcWindow = NpcWindow.ChaosMachine;
        definition.ItemCraftings.Add(context.CreateNew<ItemCrafting>());
        var npc = new NonPlayerCharacter(null!, definition, null!);
        await new TalkNpcAction().TalkToNpcAsync(player, npc).ConfigureAwait(false);
        Assert.That(player.BackupInventory, Is.Not.Null, "Opening a crafting dialog must create the backup.");
    }

    private static async ValueTask RelogAsync(Player player, Character character)
    {
        await player.RemoveFromGameAsync().ConfigureAwait(false);
        await player.SetSelectedCharacterAsync(character).ConfigureAwait(false);
    }

    private static ItemDefinition CreateDefinition(byte width = 1, byte height = 1)
        => new() { Width = width, Height = height, Durability = 1 };

    private static Item CreateItem(ItemDefinition definition)
    {
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = definition;
        item.Object.Durability = 1;
        return item.Object;
    }
}
