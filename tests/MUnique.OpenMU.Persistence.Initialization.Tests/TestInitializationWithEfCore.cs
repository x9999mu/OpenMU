// <copyright file="TestInitializationWithEfCore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Bots;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// The main program class.
/// </summary>
[TestFixture]
internal class TestInitializationWithEfCore
{
    private const byte IcarusMapNumber = 10;
    private static readonly Guid FeatherDropGroupId = new(0x200, IcarusMapNumber, 1, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly Guid CrestDropGroupId = new(0x200, IcarusMapNumber, 2, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly (short Monster, double Gemstone, double Harmony, double Guardian)[] KalimaSevenJewelDropRates =
    [
        (331, 0.1000, 0.0500, 0.0100),
        (332, 0.1167, 0.0583, 0.0117),
        (333, 0.1333, 0.0667, 0.0133),
        (334, 0.1500, 0.0750, 0.0150),
        (335, 0.1667, 0.0833, 0.0167),
        (336, 0.1833, 0.0917, 0.0183),
        (337, 0.2000, 0.1000, 0.0200),
    ];
    private static readonly (short Monster, double Gemstone, double Harmony)[] IcarusJewelDropRates =
    [
        (69, 0.0200, 0.01000),
        (71, 0.0225, 0.01125),
        (70, 0.0250, 0.01250),
        (73, 0.0275, 0.01375),
        (74, 0.0300, 0.01500),
        (72, 0.0325, 0.01625),
        (75, 0.0350, 0.01750),
        (76, 0.0375, 0.01875),
        (77, 0.0400, 0.02000),
    ];

    /// <summary>
    /// Tests the data initialization using the entity framework core.
    /// </summary>
    [Test]
    [Ignore("This is not a real test which should run automatically.")]
    public async Task SetupDatabaseAndTestLoadingDataAsync()
    {
        var manager = new PersistenceContextProvider(new NullLoggerFactory(), null);
        using var update = await manager.ReCreateDatabaseAsync().ConfigureAwait(false);
        await this.TestDataInitializationAsync(new PersistenceContextProvider(new NullLoggerFactory(), null)).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests the data initialization using the in-memory persistence.
    /// </summary>
    [Test]
    public async Task TestDataInitializationInMemoryAsync()
    {
        await this.TestDataInitializationAsync(new InMemoryPersistenceContextProvider()).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests the data initialization using the in-memory persistence.
    /// </summary>
    [Test]
    public async Task TestSeason6DataAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        await this.AssertCastleSiegeUpdatePlugInAsync(contextProvider).ConfigureAwait(false);
        await this.TestIfItemsFitIntoInventoriesAsync(contextProvider).ConfigureAwait(false);
        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the normal-server update disables open PvP and restores standard Icarus.
    /// </summary>
    [Test]
    public async Task RestoreNormalServerUpdateRepairsPvpAndIcarusAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var icarus = configuration.Maps.Single(map => map.Number == IcarusMapNumber && map.Discriminator == 0);
        var gate = configuration.WarpList.Single(warp => warp.Index == 23).Gate!;
        var server = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).Single();
        Array.Fill(icarus.TerrainData!, (byte)4, 3, ushort.MaxValue);
        gate.X1 = 53;
        gate.Y1 = 74;
        gate.X2 = 56;
        gate.Y2 = 77;
        server.PvpEnabled = true;

        var update = new RestoreNormalServerUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var terrain = new GameMapTerrain(icarus);
        var pathFinder = new PathFinder(new FullGridNetwork(true)) { SearchLimit = 10_000 };
        var route = pathFinder.FindPath(new(14, 13), new(34, 238), terrain.AIgrid, false);
        Assert.Multiple(() =>
        {
            Assert.That((gate.X1, gate.Y1, gate.X2, gate.Y2), Is.EqualTo((14, 13, 16, 13)));
            Assert.That(terrain.WalkMap[14, 13], Is.True);
            Assert.That(terrain.WalkMap[16, 13], Is.True);
            Assert.That(route, Is.Not.Null, "the arrival must connect to the far end of Icarus");
            Assert.That(server.PvpEnabled, Is.False);
        });
    }

    private async Task AssertInstantServerConfigurationAsync(IPersistenceContextProvider contextProvider, bool pvpEnabled = false)
    {
        using var context = contextProvider.CreateNewConfigurationContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var servers = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).ToList();
        var permanentMonsterSpawns = configuration.Maps
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .ToList();
        var icarusSpawns = configuration.Maps.Single(map => map.Number == IcarusMapNumber && map.Discriminator == 0).MonsterSpawns
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .ToList();
        var nonIcarusSpawns = permanentMonsterSpawns.Except(icarusSpawns).ToList();
        var monsters = configuration.Monsters.Where(monster => monster.ObjectKind == NpcObjectKind.Monster).ToList();

        foreach (var npcNumber in new short[] { 230, 242, 243, 245, 246, 251, 253, 254, 259, 376, 377, 415, 416, 417, 545, 577 })
        {
            var store = configuration.Monsters.Single(monster => monster.Number == npcNumber).MerchantStore;
            Assert.That(store, Is.Not.Null, $"NPC {npcNumber}");
            Assert.DoesNotThrow(() => _ = new Storage(InventoryConstants.WarehouseSize, store!), $"NPC {npcNumber}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(configuration.ExperienceRate, Is.EqualTo(9999f));
            Assert.That(configuration.AreaSkillHitsPlayer, Is.True);
            Assert.That(configuration.ExcellentItemDropLevelDelta, Is.Zero);
            Assert.That(configuration.GlobalBaseAttributeValues.Single(attribute => attribute.Definition?.Id == Stats.MoneyAmountRate.Id).Value, Is.EqualTo(1_000f));
            Assert.That(configuration.MaximumMasterLevel, Is.EqualTo(400));
            Assert.That(configuration.MasterExperienceRate, Is.EqualTo(1_000f));
            Assert.That(servers, Is.Not.Empty);
            Assert.That(servers.Select(server => server.ExperienceRate), Is.All.EqualTo(1.0f));
            Assert.That(servers.Select(server => server.PvpEnabled), Is.All.EqualTo(pvpEnabled));
            Assert.That(configuration.CharacterClasses.SelectMany(characterClass => characterClass.StatAttributes).Where(attribute => attribute.Attribute == Stats.PointsPerLevelUp).All(attribute => attribute.BaseValue == 500f), Is.True);
            Assert.That(new[] { Stats.BaseStrength, Stats.BaseAgility, Stats.BaseVitality, Stats.BaseEnergy, Stats.BaseLeadership }.All(stat => configuration.Attributes.Single(attribute => attribute == stat).MaximumValue == 32_767), Is.True);
            Assert.That(configuration.CharacterClasses.Where(characterClass => characterClass.IsMasterClass).SelectMany(characterClass => characterClass.BaseAttributeValues).Where(attribute => attribute.Definition == Stats.MasterPointsPerLevelUp).All(attribute => attribute.Value == 5f), Is.True);
            Assert.That(configuration.PlugInConfigurations.Single(configuration => configuration.TypeId == typeof(EndClassChatCommandPlugIn).GUID).IsActive, Is.True);
            Assert.That(permanentMonsterSpawns, Is.Not.Empty);
            Assert.That(nonIcarusSpawns.All(spawn => spawn.Quantity >= 10), Is.True);
            Assert.That(icarusSpawns, Has.Count.EqualTo(64));
            Assert.That(icarusSpawns.All(spawn => spawn.Quantity == 3), Is.True);
            Assert.That(monsters.Where(monster => monster.Number != 275 && monster.Number is < 331 or > 337).All(monster => monster.NumberOfMaximumItemDrops == 2 && monster.RespawnDelay <= TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(monsters.Where(monster => monster.Number is >= 331 and <= 337).All(monster => monster.NumberOfMaximumItemDrops == 1), Is.True);
            Assert.That(monsters.Single(monster => monster.Number == 275).NumberOfMaximumItemDrops, Is.EqualTo(12));
            Assert.That(monsters.Single(monster => monster.Number == 275)[Stats.DefenseRatePvm], Is.EqualTo(10_000));
            Assert.That(configuration.MiniGameDefinitions.All(miniGame => miniGame.ArePlayerKillersAllowedToEnter), Is.True);
        });

        Assert.That(configuration.Maps.Where(map => map.Number != 10).SelectMany(map => map.DropItemGroups).All(group => group.ItemType == SpecialItemType.Money), Is.True);
        AssertIcarusAndKalimaSevenJewelDrops(configuration);
        Assert.That(configuration.Monsters.SelectMany(monster => monster.Quests).SelectMany(quest => quest.RequiredItems).Where(item => item.Item?.IsQuestItem == true).All(item => item.DropItemGroup is null), Is.True);

        this.AssertEquipmentProfile(configuration, 254, [0, 2, 3], 2, [(5, 0), (5, 2)]);
        this.AssertEquipmentProfile(configuration, 251, [4, 6, 7], 5, [(0, 5), (0, 6)]);
        this.AssertEquipmentProfile(configuration, 251, [12, 13], 15, [(0, 5), (5, 0)]);
        this.AssertEquipmentProfile(configuration, 251, [16, 17], 25, [(2, 8), (2, 9)]);
        this.AssertEquipmentProfile(configuration, 251, [24, 25], 59, [(0, 32), (0, 33)]);
        this.AssertEquipmentProfile(configuration, 243, [8, 10, 11], 10, [(4, 0), (4, 3)]);
        this.AssertEquipmentProfile(configuration, 416, [20, 22, 23], 40, [(5, 15), (5, 21), (5, 22)]);
        this.AssertSkillProfile(configuration, 254, [0, 2, 3, 12, 13]);
        this.AssertSkillProfile(configuration, 230, [4, 6, 7, 16, 17, 24, 25]);
        this.AssertSkillProfile(configuration, 242, [8, 10, 11]);
        this.AssertSkillProfile(configuration, 417, [20, 22, 23]);

        foreach (var npcNumber in new short[] { 253, 259, 376, 377, 415, 545, 577 })
        {
            var items = configuration.Monsters.Single(monster => monster.Number == npcNumber).MerchantStore!.Items;
            Assert.Multiple(() =>
            {
                Assert.That(items.Single(item => item.Definition is { Group: 14, Number: 3 }).Durability, Is.EqualTo(255));
                Assert.That(items.Single(item => item.Definition is { Group: 14, Number: 6 }).Durability, Is.EqualTo(255));
                Assert.That(items.Select(item => (item.Definition!.Group, item.Definition.Number)), Does.Contain(((byte)14, (short)8)));
                Assert.That(items.Select(item => (item.Definition!.Group, item.Definition.Number)), Does.Contain(((byte)4, (short)7)));
                Assert.That(items.Select(item => (item.Definition!.Group, item.Definition.Number)), Does.Contain(((byte)4, (short)15)));
                Assert.That(items.Select(item => (item.Definition!.Group, item.Definition.Number)), Does.Contain(((byte)14, (short)10)));
                Assert.That(items.Select(item => (item.Definition!.Group, item.Definition.Number)), Does.Contain(((byte)13, (short)29)));
            });
        }

        var potionGirlItems = configuration.Monsters.Single(monster => monster.Number == 253).MerchantStore!.Items;
        foreach (var (number, level) in new (short Number, byte Level)[]
                 {
                     (23, 0), (23, 1), (24, 0), (24, 1), (25, 0), (26, 0), (65, 0), (66, 0), (67, 0), (68, 0),
                 })
        {
            Assert.That(potionGirlItems.Count(item => item.Definition is { Group: 14 } definition && definition.Number == number && item.Level == level), Is.EqualTo(1));
        }

        foreach (var (group, number) in new (byte Group, short Number)[] { (2, 6), (4, 6), (5, 7) })
        {
            var craftingItem = potionGirlItems.Single(item => item.Definition is { } definition && definition.Group == group && definition.Number == number);
            Assert.Multiple(() =>
            {
                Assert.That(craftingItem.Level, Is.EqualTo(9));
                Assert.That(craftingItem.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(4));
                Assert.That(craftingItem.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.EqualTo(1));
            });
        }

        foreach (var shopItem in configuration.Monsters.SelectMany(monster => monster.MerchantStore?.Items ?? []))
        {
            if (shopItem.Definition?.PossibleItemOptions.SelectMany(options => options.PossibleOptions).Any(option => option.OptionType == ItemOptionTypes.Option) != true)
            {
                continue;
            }

            Assert.That(shopItem.Level, Is.EqualTo(9), $"{shopItem.Definition}");
            Assert.That(shopItem.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Option), Is.EqualTo(1), $"{shopItem.Definition}");
            Assert.That(shopItem.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(4), $"{shopItem.Definition}");
        }

        foreach (var (group, number, level) in new (byte Group, short Number, byte Level)[]
                 {
                     (13, 14, 0), (13, 14, 1), (13, 52, 0), (13, 53, 0),
                     (14, 13, 0), (14, 14, 0), (12, 15, 0), (14, 16, 0), (14, 22, 0),
                 })
        {
            Assert.That(
                potionGirlItems.Count(item => item.Definition is { } definition && definition.Group == group && definition.Number == number && item.Level == level),
                Is.EqualTo(1));
        }

        foreach (var number in new short[] { 30, 31, 136, 137, 141 })
        {
            foreach (var level in Enumerable.Range(0, 3).Select(level => (byte)level))
            {
                Assert.That(
                    potionGirlItems.Count(item => item.Definition is { Group: 12 } definition && definition.Number == number && item.Level == level),
                    Is.EqualTo(1));
            }
        }

        Assert.That(configuration.Items.Single(item => item is { Group: 14, Number: 3 }).Durability, Is.EqualTo(byte.MaxValue));
        Assert.That(configuration.Items.Single(item => item is { Group: 14, Number: 6 }).Durability, Is.EqualTo(byte.MaxValue));
        var kundunBox = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
        foreach (var level in Enumerable.Range(8, 5).Select(level => (byte)level))
        {
            var opening = kundunBox.DropItems.Single(group => group.SourceItemLevel == level);
            Assert.That(opening.Chance, Is.EqualTo(1.0));
            Assert.That(opening.ItemType, Is.EqualTo(level >= 11 ? SpecialItemType.FullExcellent : SpecialItemType.ExcellentWithLuck));
            if (level >= 11)
            {
                Assert.That(opening.MinimumLevel, Is.EqualTo(9));
                Assert.That(opening.MaximumLevel, Is.EqualTo(9));
            }
        }

        var jackpot = configuration.Items.Single(item => item is { Group: 14, Number: 52 }).DropItems.Single();
        Assert.Multiple(() =>
        {
            Assert.That(jackpot.ItemType, Is.EqualTo(SpecialItemType.FullExcellent));
            Assert.That(jackpot.Chance, Is.EqualTo(1.0));
            Assert.That(jackpot.MinimumLevel, Is.EqualTo(9));
            Assert.That(jackpot.MaximumLevel, Is.EqualTo(9));
            Assert.That(jackpot.PossibleItems, Is.SupersetOf(kundunBox.DropItems.Single(group => group.SourceItemLevel == 12).PossibleItems));
        });

        var kundunFourDirectItems = new (byte Group, short Number)[]
        {
            (0, 16), (0, 17), (0, 18), (0, 19), (0, 20), (0, 21), (0, 31), (0, 33), (0, 34),
            (2, 11), (2, 12), (2, 13), (2, 15), (3, 10), (4, 16), (4, 17), (4, 18), (4, 19),
            (4, 20), (5, 8), (5, 9), (5, 10), (5, 11), (5, 13), (5, 19), (6, 13), (6, 15), (6, 16),
        };
        var kundunFiveDirectItems = new (byte Group, short Number)[]
        {
            (0, 22), (0, 23), (0, 26), (0, 27), (0, 28), (0, 35), (2, 14), (4, 21), (5, 12),
            (5, 19), (5, 20), (5, 30), (5, 31),
        };
        static IEnumerable<ItemDefinition> EquipmentPool(GameConfiguration config, (byte Group, short Number)[] directItems, short[] armorSets)
            => directItems.Select(id => config.Items.Single(item => item.Group == id.Group && item.Number == id.Number))
                .Concat(config.Items.Where(item => item.Group is >= 7 and <= 11 && armorSets.Contains(item.Number)))
                .Distinct();

        Assert.That(
            kundunBox.DropItems.Single(group => group.SourceItemLevel == 11).PossibleItems,
            Is.EquivalentTo(EquipmentPool(configuration, kundunFourDirectItems, [17, 21, 18, 22, 19, 24, 20, 23, 27, 28, 42, 44, 60, 61])));
        Assert.That(
            kundunBox.DropItems.Single(group => group.SourceItemLevel == 12).PossibleItems,
            Is.EquivalentTo(EquipmentPool(configuration, kundunFiveDirectItems, [29, 30, 31, 32, 33, 43, 73])));
        Assert.That(
            jackpot.PossibleItems,
            Is.EquivalentTo(EquipmentPool(configuration, kundunFiveDirectItems, [29, 30, 31, 32, 33, 43, 73, 45, 46, 47, 48, 49, 50, 51, 52, 53])));

        foreach (var opening in new[] { kundunBox.DropItems.Single(group => group.SourceItemLevel == 11), kundunBox.DropItems.Single(group => group.SourceItemLevel == 12), jackpot })
        {
            Assert.That(opening.PossibleItems, Is.Not.Empty);
            Assert.That(opening.PossibleItems.All(item => item.PossibleItemOptions.SelectMany(options => options.PossibleOptions).Any(option => option.OptionType == ItemOptionTypes.Excellent)), Is.True, opening.Description);
            Assert.That(opening.PossibleItems.All(item => item.PossibleItemOptions.SelectMany(options => options.PossibleOptions).Any(option => option.OptionType == ItemOptionTypes.Luck)), Is.True, opening.Description);
            Assert.That(opening.PossibleItems.All(item => item.PossibleItemOptions.SelectMany(options => options.PossibleOptions).Any(option => option.OptionType == ItemOptionTypes.Option)), Is.True, opening.Description);
        }

        var chaosCraftings = configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings.ToList();
        Assert.That(chaosCraftings, Has.Count.EqualTo(29));
        Assert.Multiple(() =>
        {
            Assert.That(chaosCraftings.Single(crafting => crafting.Number == 8).ItemCraftingHandlerClassName, Is.EqualTo(typeof(InstantServerBloodCastleTicketCrafting).FullName));
            Assert.That(chaosCraftings.Single(crafting => crafting.Number == 2).ItemCraftingHandlerClassName, Is.EqualTo(typeof(InstantServerDevilSquareTicketCrafting).FullName));
            Assert.That(chaosCraftings.Single(crafting => crafting.Number == 37).ItemCraftingHandlerClassName, Is.EqualTo(typeof(InstantServerIllusionTempleTicketCrafting).FullName));
            Assert.That(chaosCraftings.Single(crafting => crafting.Number == 28).ItemCraftingHandlerClassName, Is.EqualTo(typeof(InstantServerFenrirUpgradeCrafting).FullName));
        });

        foreach (var crafting in chaosCraftings.Where(crafting => crafting.SimpleCraftingSettings is not null))
        {
            var settings = crafting.SimpleCraftingSettings!;
            Assert.Multiple(() =>
            {
                Assert.That(settings.SuccessPercent, Is.EqualTo(100), $"Crafting {crafting.Number}");
                Assert.That(settings.MaximumSuccessPercent, Is.EqualTo(100), $"Crafting {crafting.Number}");
                Assert.That(settings.NpcPriceDivisor, Is.Zero, $"Crafting {crafting.Number}");
                Assert.That(settings.RequiredItems.All(item => item.AddPercentage == 0 && item.NpcPriceDivisor == 0), Is.True, $"Crafting {crafting.Number}");
            });
        }

        foreach (var crafting in chaosCraftings.Where(crafting => crafting.Number is 7 or 11 or 24 or 38 or 39))
        {
            Assert.Multiple(() =>
            {
                Assert.That(crafting.ItemCraftingHandlerClassName, Is.EqualTo(typeof(InstantServerWingCrafting).FullName));
                Assert.That(crafting.SimpleCraftingSettings!.ResultItemLuckOptionChance, Is.EqualTo(100));
                Assert.That(crafting.SimpleCraftingSettings.ResultItemExcellentOptionChance, Is.EqualTo(90));
            });
        }

        var gachaGroups = Enumerable.Range(1, 6)
            .Select(tier => configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, (short)tier, 0, 0, 0, 0, 0, 0, 0, 0)))
            .ToList();
        Assert.That(gachaGroups.Select(group => group.Chance), Is.EqualTo(new[] { 0.02, 0.015, 0.01, 0.475, 0.475, 0.05 }));
        Assert.That(gachaGroups.All(group => group.PossibleItems.Count == 1), Is.True);
        Assert.That(configuration.DropItemGroups.Where(group => group.Monster is not null && group.ItemLevel is >= 8 and <= 12 && group.PossibleItems.Count == 1 && group.PossibleItems.Single() == kundunBox), Is.Empty);

        var bossNumbers = new HashSet<short> { 43, 44, 53, 54, 78, 79, 80, 81, 82, 83, 135, 161, 181, 189, 197, 267, 275, 295, 338, 361, 362, 363, 364, 440, 459 };

        // The Illusion of Kundun 7 (275) drops guaranteed jewels and gifts instead of the boss gacha groups;
        // see AssertIcarusAndKalimaSevenJewelDrops.
        foreach (var boss in monsters.Where(monster => bossNumbers.Contains(monster.Number) && monster.Number != 275))
        {
            Assert.That(boss.DropItemGroups.Intersect(gachaGroups), Is.EquivalentTo(gachaGroups.Skip(3)));
            Assert.That(boss.DropItemGroups.Where(group => group.Chance < 1.0), Is.EquivalentTo(gachaGroups.Skip(3)));
            Assert.That(boss.NumberOfMaximumItemDrops, Is.EqualTo(2));
        }

        // The Kalima 7 regular monsters have their own box and jewel drops; see AssertIcarusAndKalimaSevenJewelDrops.
        var kalimaSevenRegularNumbers = new HashSet<short> { 331, 332, 333, 334, 335, 336, 337 };
        var regularMonsters = permanentMonsterSpawns
            .Select(spawn => spawn.MonsterDefinition!)
            .Where(monster => !bossNumbers.Contains(monster.Number) && !kalimaSevenRegularNumbers.Contains(monster.Number))
            .Distinct();
        foreach (var monster in regularMonsters)
        {
            Assert.That(monster.DropItemGroups.Intersect(gachaGroups), Is.EquivalentTo(gachaGroups.Take(3)));
            Assert.That(monster.DropItemGroups.Single(group => group.GetId() == DropGroupId(4)).Chance, Is.EqualTo(0.01));
            Assert.That(monster.NumberOfMaximumItemDrops, Is.EqualTo(2));
        }

        var icarusGroupId = new Guid(0x200, 9_999, 10, 4, 0, 0, 0, 0, 0, 0, 0);
        var icarusGroup = configuration.DropItemGroups.Single(group => group.GetId() == icarusGroupId);
        var icarusMap = configuration.Maps.Single(map => map is { Number: 10, Discriminator: 0 });
        Assert.Multiple(() =>
        {
            Assert.That(icarusMap.DropItemGroups.Count(group => group.GetId() == icarusGroupId), Is.EqualTo(1));
            Assert.That(icarusGroup.Chance, Is.EqualTo(0.05));
            Assert.That(icarusGroup.ItemLevel, Is.EqualTo(11));
            Assert.That(icarusGroup.PossibleItems.Single(), Is.SameAs(kundunBox));
            Assert.That(monsters.SelectMany(monster => monster.DropItemGroups), Does.Not.Contain(icarusGroup));
        });

        var expectedIcarusStats = new (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[]
        {
            (69, 750_000, 18_000, 24_000, 8_000, 20_000, 12_000),
            (70, 950_000, 18_000, 24_000, 8_000, 20_000, 12_000),
            (71, 750_000, 18_000, 24_000, 8_000, 20_000, 12_000),
            (72, 2_050_000, 18_000, 24_000, 8_500, 20_700, 12_000),
            (73, 1_450_000, 18_000, 24_000, 8_000, 20_000, 12_000),
            (74, 1_725_000, 18_000, 24_000, 8_000, 20_000, 12_000),
            (75, 2_500_000, 19_500, 24_000, 9_900, 24_000, 12_200),
            (76, 3_650_000, 25_500, 28_800, 11_600, 25_200, 12_200),
            (77, 4_750_000, 28_500, 30_000, 12_000, 27_000, 14_000),
        };
        foreach (var expected in expectedIcarusStats)
        {
            var monster = monsters.Single(item => item.Number == expected.Number);
            Assert.Multiple(() =>
            {
                Assert.That(monster[Stats.MaximumHealth], Is.EqualTo(expected.Health));
                Assert.That(monster[Stats.MinimumPhysBaseDmg], Is.EqualTo(expected.MinimumDamage));
                Assert.That(monster[Stats.MaximumPhysBaseDmg], Is.EqualTo(expected.MaximumDamage));
                Assert.That(monster[Stats.DefenseBase], Is.EqualTo(expected.Defense));
                Assert.That(monster[Stats.AttackRatePvm], Is.EqualTo(expected.AttackRate));
                Assert.That(monster[Stats.DefenseRatePvm], Is.EqualTo(expected.DefenseRate));
            });
        }

        // The Illusion of Kundun 7 (275) is intentionally excluded from the boss defense rate floor,
        // so that it stays hittable with a regular attack rate; see ConfigureKalimaSevenBossDefenseRate.
        foreach (var boss in monsters.Where(monster => bossNumbers.Contains(monster.Number) && monster.Number != 275))
        {
            var tier = Math.Max(0, boss[Stats.Level] - 20);
            Assert.Multiple(() =>
            {
                Assert.That(boss[Stats.MaximumHealth], Is.GreaterThanOrEqualTo(Math.Clamp(tier * 500_000, 4_000_000, 60_000_000)));
                Assert.That(boss[Stats.MinimumPhysBaseDmg], Is.GreaterThanOrEqualTo(Math.Clamp(tier * 250, 8_000, 32_000)));
                Assert.That(boss[Stats.DefenseBase], Is.GreaterThanOrEqualTo(Math.Clamp(tier * 300, 8_000, 30_000)));
                Assert.That(boss[Stats.AttackRatePvm], Is.GreaterThanOrEqualTo(Math.Clamp(tier * 250, 12_000, 30_000)));
                Assert.That(boss[Stats.DefenseRatePvm], Is.GreaterThanOrEqualTo(Math.Clamp(tier * 200, 10_000, 25_000)));
            });
        }

        this.AssertContinuousBossEvent<GoldenInvasionPlugIn>(configuration, TimeOnly.MinValue);
        this.AssertContinuousBossEvent<RedDragonInvasionPlugIn>(configuration, new TimeOnly(0, 10));
        this.AssertContinuousBossEvent<WhiteWizardInvasionPlugIn>(configuration, new TimeOnly(0, 20));
    }

    private void AssertEquipmentProfile(GameConfiguration configuration, short npcNumber, byte[] classes, byte setNumber, (byte Group, byte Number)[] weapons)
    {
        var classSet = classes.ToHashSet();
        var storeItems = configuration.Monsters.Single(monster => monster.Number == npcNumber).MerchantStore!.Items;
        var expectedDefinitions = configuration.Items
            .Where(item => (item.Group is >= 7 and <= 11 && item.Number == setNumber || weapons.Contains((item.Group, (byte)item.Number)))
                           && item.QualifiedCharacters.Any(characterClass => classSet.Contains(characterClass.Number)))
            .ToList();
        foreach (var definition in expectedDefinitions)
        {
            var item = storeItems.FirstOrDefault(item => item.Definition == definition);
            Assert.That(item, Is.Not.Null, $"NPC {npcNumber}: {definition}");
            Assert.Multiple(() =>
            {
                Assert.That(item!.Level, Is.EqualTo(9));
                Assert.That(item.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent), Is.EqualTo(1));
                Assert.That(item.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.EqualTo(1));
                Assert.That(item.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(4));
                Assert.That(item.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent).ItemOption?.PowerUpDefinition?.TargetAttribute, Is.EqualTo(definition.Group >= 7 ? Stats.MaximumHealth : Stats.ExcellentDamageChance));
                Assert.That(item.HasSkill, Is.EqualTo(item.CanHaveSkill()));
            });
        }
    }

    private void AssertSkillProfile(GameConfiguration configuration, short npcNumber, byte[] classes)
    {
        var classSet = classes.ToHashSet();
        var storeItems = configuration.Monsters.Single(monster => monster.Number == npcNumber).MerchantStore!.Items;
        var expectedDefinitions = configuration.Items.Where(item => item.Group is 12 or 15 && item.ItemSlot is null && item.Skill is not null && item.QualifiedCharacters.Any(characterClass => classSet.Contains(characterClass.Number)));
        foreach (var definition in expectedDefinitions)
        {
            Assert.That(storeItems.Any(item => item.Definition == definition), Is.True, $"NPC {npcNumber}: {definition}");
        }

        if (classes.Contains((byte)8))
        {
            Assert.That(storeItems.Where(item => item.Definition is { Group: 12, Number: 11 }).Select(item => item.Level), Is.EquivalentTo(Enumerable.Range(0, 7).Select(level => (byte)level)));
        }
    }

    private void AssertContinuousBossEvent<TPlugIn>(GameConfiguration configuration, TimeOnly firstStart)
        where TPlugIn : SimpleInvasionPlugIn
    {
        var plugIn = configuration.PlugInConfigurations.Single(item => item.TypeId == typeof(TPlugIn).GUID);
        var eventConfiguration = plugIn.GetConfiguration<PeriodicInvasionConfiguration>(null);
        Assert.Multiple(() =>
        {
            Assert.That(plugIn.IsActive, Is.True);
            Assert.That(eventConfiguration, Is.Not.Null);
            Assert.That(eventConfiguration!.PreStartMessageDelay, Is.EqualTo(TimeSpan.Zero));
            Assert.That(eventConfiguration.TaskDuration, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(eventConfiguration.Timetable.First(), Is.EqualTo(firstStart));
            Assert.That(eventConfiguration.Timetable.Zip(eventConfiguration.Timetable.Skip(1), (first, second) => second - first).All(interval => interval == TimeSpan.FromMinutes(30)), Is.True);
        });
    }


    /// <summary>
    /// Tests that the instant-server update repairs an existing Season 6 configuration.
    /// </summary>
    [Test]
    public async Task TestInstantServerUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var server = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).Single();
            configuration.ExperienceRate = 1f;
            configuration.AreaSkillHitsPlayer = false;
            server.PvpEnabled = true;
            await new ConfigureInstantServerUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }
        await this.AssertInstantServerConfigurationAsync(contextProvider, pvpEnabled: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the master-progression update repairs existing configuration and enables the final-class command.
    /// </summary>
    [Test]
    public async Task TestConfigureMasterProgressionUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var masterClass = configuration.CharacterClasses.First(characterClass => characterClass.IsMasterClass);
            var masterPoints = masterClass.BaseAttributeValues.Single(attribute => attribute.Definition == Stats.MasterPointsPerLevelUp);
            masterClass.BaseAttributeValues.Remove(masterPoints);
            masterClass.BaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(1, masterPoints.Definition!));
            configuration.MaximumMasterLevel = 1;
            configuration.MasterExperienceRate = 1;
            configuration.PlugInConfigurations.Remove(configuration.PlugInConfigurations.Single(plugIn => plugIn.TypeId == typeof(EndClassChatCommandPlugIn).GUID));

            await new ConfigureMasterProgressionUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the completion update is idempotent and repairs persisted character point rates.
    /// </summary>
    [Test]
    public async Task TestCompleteInstantServerUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        const int normalFreePoints = 1234;
        const int heroFreePoints = 5678;
        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var pointsDefinition = configuration.Attributes.Single(attribute => attribute.Id == Stats.PointsPerLevelUp.Id);
            var heroDefinition = configuration.Attributes.Single(attribute => attribute.Id == Stats.GainHeroStatusQuestCompleted.Id);
            var account = context.CreateNew<Account>();
            account.LoginName = "instant-update";
            var normal = context.CreateNew<Character>();
            normal.Name = "Before117";
            normal.LevelUpPoints = normalFreePoints;
            normal.Attributes.Add(context.CreateNew<StatAttribute>(pointsDefinition, 5));
            account.Characters.Add(normal);
            var hero = context.CreateNew<Character>();
            hero.Name = "Hero117";
            hero.LevelUpPoints = heroFreePoints;
            hero.Attributes.Add(context.CreateNew<StatAttribute>(pointsDefinition, 6));
            hero.Attributes.Add(context.CreateNew<StatAttribute>(heroDefinition, 1));
            account.Characters.Add(hero);

            var update = new CompleteInstantServerUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var account = (await context.GetAsync<Account>().ConfigureAwait(false)).Single(item => item.LoginName == "instant-update");
            var normal = account.Characters.Single(character => character.Name == "Before117");
            var hero = account.Characters.Single(character => character.Name == "Hero117");
            Assert.Multiple(() =>
            {
                Assert.That(normal.Attributes.Single(attribute => attribute.Definition == Stats.PointsPerLevelUp).Value, Is.EqualTo(500));
                Assert.That(hero.Attributes.Single(attribute => attribute.Definition == Stats.PointsPerLevelUp).Value, Is.EqualTo(501));
                Assert.That(normal.LevelUpPoints, Is.EqualTo(normalFreePoints));
                Assert.That(hero.LevelUpPoints, Is.EqualTo(heroFreePoints));
                Assert.That(configuration.DropItemGroups.Count(group => group.GetId() == new Guid(0x200, 9_999, 1, 0, 0, 0, 0, 0, 0, 0, 0)), Is.EqualTo(1));
                Assert.That(configuration.Items.Single(item => item is { Group: 14, Number: 52 }).DropItems, Has.Exactly(1).Items);
            });
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the Zen update repairs the multiplier of an existing Season 6 configuration.
    /// </summary>
    [Test]
    public async Task TestIncreaseInstantServerMoneyDropUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var oldRate = configuration.GlobalBaseAttributeValues.Single(attribute => attribute.Definition?.Id == Stats.MoneyAmountRate.Id);
            configuration.GlobalBaseAttributeValues.Remove(oldRate);
            configuration.GlobalBaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(1f, oldRate.Definition));
            await new IncreaseInstantServerMoneyDropUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        using var verificationContext = contextProvider.CreateNewConfigurationContext();
        var updatedConfiguration = (await verificationContext.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        Assert.That(updatedConfiguration.GlobalBaseAttributeValues.Single(attribute => attribute.Definition?.Id == Stats.MoneyAmountRate.Id).Value, Is.EqualTo(1_000f));
    }

    /// <summary>
    /// Tests that the Luck update repairs existing instant-server shops and Box of Kundun groups.
    /// </summary>
    [Test]
    public async Task TestAddInstantServerLuckUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var shopItem = configuration.Monsters
                .SelectMany(monster => monster.MerchantStore?.Items ?? [])
                .First(item => item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck));
            foreach (var luck in shopItem.ItemOptions.Where(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck).ToList())
            {
                shopItem.ItemOptions.Remove(luck);
            }

            var kundunBox = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
            foreach (var group in kundunBox.DropItems.Where(group => group.SourceItemLevel is >= 8 and <= 10))
            {
                group.ItemType = SpecialItemType.Excellent;
            }

            var update = new AddInstantServerLuckUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the gameplay rebalance repairs existing x9999 data idempotently.
    /// </summary>
    [Test]
    public async Task TestRebalanceInstantServerUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var chaosCraftings = configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings;
            chaosCraftings.First(crafting => crafting.SimpleCraftingSettings is not null).SimpleCraftingSettings!.SuccessPercent = 5;
            chaosCraftings.Single(crafting => crafting.Number == 8).ItemCraftingHandlerClassName = typeof(BloodCastleTicketCrafting).FullName!;

            var shopItem = configuration.Monsters
                .SelectMany(monster => monster.MerchantStore?.Items ?? [])
                .First(item => item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Option));
            shopItem.Level = 0;
            shopItem.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).Level = 0;

            var icarus = configuration.Maps.Single(map => map is { Number: 10, Discriminator: 0 });
            icarus.MonsterSpawns.First(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster }).Quantity = 10;
            configuration.Monsters.Single(monster => monster.Number == 69).Attributes.Single(attribute => attribute.AttributeDefinition == Stats.MaximumHealth).Value = 1;
            var icarusGroup = configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, 10, 4, 0, 0, 0, 0, 0, 0, 0));
            icarus.DropItemGroups.Remove(icarusGroup);

            configuration.Monsters.Single(monster => monster.Number == 79).Attributes.Single(attribute => attribute.AttributeDefinition == Stats.MaximumHealth).Value = 1;
            var kundunFour = configuration.Items.Single(item => item is { Group: 14, Number: 11 }).DropItems.Single(group => group.SourceItemLevel == 11);
            kundunFour.ItemType = SpecialItemType.Excellent;
            kundunFour.PossibleItems.Clear();

            var update = new RebalanceInstantServerUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the Illusion of Kundun 7 loot update repairs its guaranteed eighteen boxes idempotently.
    /// </summary>
    [Test]
    public async Task TestIllusionOfKundunSevenLootUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var kundunSeven = configuration.Monsters.Single(monster => monster.Number == 275);
            kundunSeven.NumberOfMaximumItemDrops = 2;
            kundunSeven.DropItemGroups.Clear();

            var update = new ConfigureIllusionOfKundunSevenLootUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            var drops = kundunSeven.DropItemGroups.Where(group => group.Monster == kundunSeven).ToList();
            var kundunBox = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
            var gmGift = configuration.Items.Single(item => item is { Group: 14, Number: 52 });
            Assert.Multiple(() =>
            {
                Assert.That(kundunSeven.NumberOfMaximumItemDrops, Is.EqualTo(18));
                Assert.That(drops, Has.Count.EqualTo(18));
                Assert.That(drops, Is.All.Matches<DropItemGroup>(group => group is { Chance: 1.0, ItemType: SpecialItemType.RandomItem }));
                Assert.That(drops.Count(group => group is { ItemLevel: 11 } && group.PossibleItems.Single() == kundunBox), Is.EqualTo(6));
                Assert.That(drops.Count(group => group is { ItemLevel: 12 } && group.PossibleItems.Single() == kundunBox), Is.EqualTo(6));
                Assert.That(drops.Count(group => group is { ItemLevel: 0 } && group.PossibleItems.Single() == gmGift), Is.EqualTo(6));
            });
        }
    }

    /// <summary>
    /// Tests that the Kalima 7 regular drop update removes Zen and low-tier Kundun boxes idempotently.
    /// </summary>
    [Test]
    public async Task TestConfigureKalimaSevenRegularDropsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        await new RestrictInstantServerDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var update = new ConfigureKalimaSevenRegularDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var map = configuration.Maps.Single(map => map is { Number: 36, Discriminator: 0 });
        var boxOfKundun = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
        var boxFourGroup = configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, 36, 1, 0, 0, 0, 0, 0, 0, 0));
        var regularNumbers = new HashSet<short> { 331, 332, 333, 334, 335, 336, 337 };
        var regularMonsters = configuration.Monsters.Where(monster => regularNumbers.Contains(monster.Number)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(map.DropItemGroups, Has.None.Matches<DropItemGroup>(group => group.ItemType == SpecialItemType.Money));
            Assert.That(boxFourGroup.Chance, Is.EqualTo(0.1));
            Assert.That(boxFourGroup.ItemType, Is.EqualTo(SpecialItemType.RandomItem));
            Assert.That(boxFourGroup.ItemLevel, Is.EqualTo(11));
            Assert.That(boxFourGroup.PossibleItems, Is.EquivalentTo(new[] { boxOfKundun }));
            Assert.That(regularMonsters, Has.Count.EqualTo(7));
            Assert.That(regularMonsters, Is.All.Matches<MonsterDefinition>(monster => monster.NumberOfMaximumItemDrops == 1));
            Assert.That(regularMonsters, Is.All.Matches<MonsterDefinition>(monster => monster.DropItemGroups.Contains(boxFourGroup)));
            Assert.That(regularMonsters.SelectMany(monster => monster.DropItemGroups), Has.None.Matches<DropItemGroup>(group => group.PossibleItems.Contains(boxOfKundun) && group.ItemLevel is >= 8 and <= 10));
        });
    }

    /// <summary>
    /// Tests that the jewel drop update repairs the retired map-level jewel groups and the
    /// Illusion of Kundun 7 loot idempotently.
    /// </summary>
    [Test]
    public async Task TestRestructureIcarusAndKalimaSevenJewelDropsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var jewelItems = new[]
        {
            configuration.Items.Single(item => item is { Group: 14, Number: 41 }),
            configuration.Items.Single(item => item is { Group: 14, Number: 42 }),
            configuration.Items.Single(item => item is { Group: 14, Number: 31 }),
        };

        // The initial data already contains the final state, so the jewel groups are removed first to get
        // the state of a database which was still running the retired updates.
        var jewelGroups = configuration.DropItemGroups
            .Where(group => group.Monster is not null && group.PossibleItems.Count == 1 && jewelItems.Contains(group.PossibleItems.Single()))
            .ToList();
        foreach (var group in jewelGroups)
        {
            group.Monster!.DropItemGroups.Remove(group);
            configuration.DropItemGroups.Remove(group);
        }

        await new ConfigureKalimaSevenRegularDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new RetuneKalimaSevenRegularMonstersUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new IncreaseKalimaSevenBoxDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureIllusionOfKundunSevenLootUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new AddJewelDropsToIcarusAndKalima7UpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var update = new RestructureIcarusAndKalimaSevenJewelDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var dropGroupCount = configuration.DropItemGroups.Count;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.That(configuration.DropItemGroups, Has.Count.EqualTo(dropGroupCount), "the second application must not add groups");
        AssertIcarusAndKalimaSevenJewelDrops(configuration);
    }

    [Test]
    public async Task TestReduceKalimaSevenBossDefenseRateUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();

        // The initial data already contains the final state, so the retired Kalima 7 update is applied
        // to get the state of a database which was still running the high defense rate.
        await new ConfigureKalimaSevenUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(48_000), "precondition");

        var update = new ReduceKalimaSevenBossDefenseRateUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(10_000), "defense rate");
            Assert.That(boss[Stats.AttackRatePvm], Is.EqualTo(60_000), "the rest of the boss must stay untouched");
            Assert.That(boss[Stats.DefenseBase], Is.EqualTo(60_000), "the rest of the boss must stay untouched");
            Assert.That(boss[Stats.MaximumHealth], Is.EqualTo(120_000_000), "the rest of the boss must stay untouched");
        });
    }

    /// <summary>
    /// Asserts the final Icarus, Kalima 7 and Illusion of Kundun 7 jewel drop configuration.
    /// </summary>
    /// <param name="configuration">The game configuration.</param>
    private static void AssertIcarusAndKalimaSevenJewelDrops(GameConfiguration configuration)
    {
        var gemstone = configuration.Items.Single(item => item is { Group: 14, Number: 41 });
        var harmony = configuration.Items.Single(item => item is { Group: 14, Number: 42 });
        var guardian = configuration.Items.Single(item => item is { Group: 14, Number: 31 });
        var kundunBox = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
        var gmGift = configuration.Items.Single(item => item is { Group: 14, Number: 52 });
        var icarus = configuration.Maps.Single(map => map is { Number: IcarusMapNumber, Discriminator: 0 });
        var kalima7 = configuration.Maps.Single(map => map is { Number: 36, Discriminator: 0 });
        var kundun7 = configuration.Monsters.Single(monster => monster.Number == 275);
        var globalJewelDropGroupId = DropGroupId(4);
        var boxFourDropGroupId = DropGroupId(9_999, 36, 1);
        var boxFiveDropGroupId = DropGroupId(9_999, 36, 2);
        var gachaRegularDropGroupIds = new short[] { 1, 2, 3 }.Select(number => DropGroupId(9_999, number)).ToList();
        var retiredMapDropGroupIds = new[]
        {
            DropGroupId(IcarusMapNumber, 3),
            DropGroupId(IcarusMapNumber, 4),
            DropGroupId(36, 1),
            DropGroupId(36, 2),
        };

        Assert.Multiple(() =>
        {
            Assert.That(configuration.DropItemGroups.Where(group => retiredMapDropGroupIds.Contains(group.GetId())), Is.Empty, "retired map-level jewel groups");
            Assert.That(kalima7.DropItemGroups, Is.Empty, "Kalima 7 owns no map-level drop group");
            Assert.That(icarus.DropItemGroups.Select(group => group.GetId()), Does.Contain(DropGroupId(1)), "Icarus money drop");
            Assert.That(icarus.DropItemGroups.Select(group => group.GetId()), Does.Contain(DropGroupId(9_999, IcarusMapNumber, 4)), "Icarus box drop");
        });

        foreach (var (monsterNumber, gemstoneChance, harmonyChance, guardianChance) in KalimaSevenJewelDropRates)
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == monsterNumber);
            AssertJewelDropGroup(configuration, monster, gemstone, JewelDropGroupId(monsterNumber, 0), gemstoneChance);
            AssertJewelDropGroup(configuration, monster, harmony, JewelDropGroupId(monsterNumber, 1), harmonyChance);
            AssertJewelDropGroup(configuration, monster, guardian, JewelDropGroupId(monsterNumber, 2), guardianChance);
            var boxFour = monster.DropItemGroups.Single(group => group.GetId() == boxFourDropGroupId);
            var boxFive = monster.DropItemGroups.Single(group => group.GetId() == boxFiveDropGroupId);
            Assert.Multiple(() =>
            {
                Assert.That(monster.NumberOfMaximumItemDrops, Is.EqualTo(1), $"{monster.Designation}: max drops");
                Assert.That(monster.DropItemGroups.Select(group => group.GetId()), Does.Contain(globalJewelDropGroupId), $"{monster.Designation}: jewel drop group");
                Assert.That(monster.DropItemGroups.Where(group => gachaRegularDropGroupIds.Contains(group.GetId())), Is.Empty, $"{monster.Designation}: gacha boxes");
                Assert.That(boxFour.Chance, Is.EqualTo(0.3817), $"{monster.Designation}: box +4 chance");
                Assert.That(boxFour.ItemLevel, Is.EqualTo(11), $"{monster.Designation}: box +4 level");
                Assert.That(boxFour.PossibleItems, Is.EquivalentTo(new[] { kundunBox }), $"{monster.Designation}: box +4 item");
                Assert.That(boxFive.Chance, Is.EqualTo(0.3053), $"{monster.Designation}: box +5 chance");
                Assert.That(boxFive.ItemLevel, Is.EqualTo(12), $"{monster.Designation}: box +5 level");
            });
        }

        // Monster 76 has no spawn area, so the instant-server configuration never attached the regular groups to it.
        var icarusSpawnedMonsterNumbers = icarus.MonsterSpawns.Select(spawn => spawn.MonsterDefinition!.Number).ToHashSet();
        foreach (var (monsterNumber, gemstoneChance, harmonyChance) in IcarusJewelDropRates)
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == monsterNumber);
            AssertJewelDropGroup(configuration, monster, gemstone, JewelDropGroupId(monsterNumber, 0), gemstoneChance);
            AssertJewelDropGroup(configuration, monster, harmony, JewelDropGroupId(monsterNumber, 1), harmonyChance);
            Assert.Multiple(() =>
            {
                Assert.That(monster.NumberOfMaximumItemDrops, Is.EqualTo(2), $"{monster.Designation}: max drops");
                if (icarusSpawnedMonsterNumbers.Contains(monsterNumber))
                {
                    Assert.That(monster.DropItemGroups.Select(group => group.GetId()), Does.Contain(globalJewelDropGroupId), $"{monster.Designation}: jewel drop group");
                    Assert.That(monster.DropItemGroups.Select(group => group.GetId()), Is.SupersetOf(gachaRegularDropGroupIds), $"{monster.Designation}: gacha boxes");
                }

                Assert.That(configuration.DropItemGroups.Where(group => group.GetId() == JewelDropGroupId(monsterNumber, 2)), Is.Empty, $"{monster.Designation}: no Jewel of Guardian");
            });
        }

        var kundunDropGroups = kundun7.DropItemGroups.ToList();
        var retiredKundunDropGroupIds = Enumerable.Range(11, 6)
            .Concat(Enumerable.Range(21, 6))
            .Concat(Enumerable.Range(34, 3))
            .Select(value => DropGroupId(9_999, kundun7.Number, (byte)value))
            .ToList();
        var gachaBossDropGroupIds = new short[] { 4, 5, 6 }.Select(number => DropGroupId(9_999, number)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(kundun7.NumberOfMaximumItemDrops, Is.EqualTo(12), "Kundun 7: max drops");
            Assert.That(kundunDropGroups, Has.Count.EqualTo(12), "Kundun 7: guaranteed drops");
            Assert.That(kundunDropGroups, Is.All.Matches<DropItemGroup>(group => group is { Chance: 1.0 } && group.Monster == kundun7), "Kundun 7: guaranteed groups");
            Assert.That(configuration.DropItemGroups.Where(group => retiredKundunDropGroupIds.Contains(group.GetId())), Is.Empty, "Kundun 7: retired box groups");
            Assert.That(kundunDropGroups.Where(group => gachaBossDropGroupIds.Contains(group.GetId())), Is.Empty, "Kundun 7: boss gacha groups");
            Assert.That(kundunDropGroups.Count(group => group.PossibleItems.Single() == gmGift), Is.EqualTo(3), "Kundun 7: GM Gifts");
            Assert.That(kundunDropGroups.Count(group => group.PossibleItems.Single() == harmony), Is.EqualTo(6), "Kundun 7: Jewels of Harmony");
            Assert.That(kundunDropGroups.Count(group => group.PossibleItems.Single() == guardian), Is.EqualTo(3), "Kundun 7: Jewels of Guardian");
            Assert.That(kundunDropGroups.Where(group => group.PossibleItems.Single() == gmGift), Is.All.Matches<DropItemGroup>(group => group is { ItemType: SpecialItemType.RandomItem, ItemLevel: 0 }), "Kundun 7: GM Gift groups");
            Assert.That(kundunDropGroups.Where(group => group.PossibleItems.Single() != gmGift), Is.All.Matches<DropItemGroup>(group => group is { ItemType: SpecialItemType.Jewel, ItemLevel: null }), "Kundun 7: jewel groups");
        });
    }

    /// <summary>
    /// Creates the identifier of the drop item group of a single number.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <returns>The identifier.</returns>
    private static Guid DropGroupId(short number)
        => new(0x200, number, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Creates the identifier of the drop item group of a parent and child number.
    /// </summary>
    /// <param name="parentNumber">The parent number.</param>
    /// <param name="number">The child number.</param>
    /// <returns>The identifier.</returns>
    private static Guid DropGroupId(short parentNumber, short number)
        => new(0x200, parentNumber, number, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Creates the identifier of the drop item group of three numbers.
    /// </summary>
    /// <param name="parentNumber">The parent number.</param>
    /// <param name="number">The child number.</param>
    /// <param name="subNumber">The sub number.</param>
    /// <returns>The identifier.</returns>
    private static Guid DropGroupId(short parentNumber, short number, byte subNumber)
        => new(0x200, parentNumber, number, subNumber, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Gets the identifier of the jewel drop group of a monster.
    /// </summary>
    /// <param name="monsterNumber">The number of the monster.</param>
    /// <param name="itemIndex">The index of the item within the per-monster jewel groups.</param>
    /// <returns>The identifier of the drop item group.</returns>
    private static Guid JewelDropGroupId(short monsterNumber, byte itemIndex)
        => new(0x200, 9_999, monsterNumber, (byte)(10 + itemIndex), 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Asserts a single per-monster jewel drop group.
    /// </summary>
    /// <param name="configuration">The game configuration.</param>
    /// <param name="monster">The monster which drops the jewel.</param>
    /// <param name="item">The dropped item.</param>
    /// <param name="id">The identifier of the drop item group.</param>
    /// <param name="chance">The expected chance per kill.</param>
    private static void AssertJewelDropGroup(GameConfiguration configuration, MonsterDefinition monster, ItemDefinition item, Guid id, double chance)
    {
        var groups = configuration.DropItemGroups.Where(group => group.GetId() == id).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Count.EqualTo(1), $"{monster.Designation}: aggregate entries of group {id}");
            Assert.That(monster.DropItemGroups.Count(group => group.GetId() == id), Is.EqualTo(1), $"{monster.Designation}: links of group {id}");
            Assert.That(groups[0].Monster, Is.SameAs(monster), $"{monster.Designation}: owner of group {id}");
            Assert.That(groups[0].PossibleItems, Is.EquivalentTo(new[] { item }), $"{monster.Designation}: items of group {id}");
            Assert.That(groups[0].ItemType, Is.EqualTo(SpecialItemType.Jewel), $"{monster.Designation}: item type of group {id}");
            Assert.That(groups[0].Chance, Is.EqualTo(chance), $"{monster.Designation}: chance of group {id}");
            Assert.That(groups[0].MinimumMonsterLevel, Is.Null, $"{monster.Designation}: minimum level of group {id}");
            Assert.That(groups[0].MaximumMonsterLevel, Is.Null, $"{monster.Designation}: maximum level of group {id}");
            Assert.That(groups[0].ItemLevel, Is.Null, $"{monster.Designation}: item level of group {id}");
        });
    }

    /// <summary>
    /// Tests that the inventory extension item update leaves exactly one single-use item behind.
    /// </summary>
    [Test]
    public async Task TestConfigureInventoryExtensionItemUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new ConfigureInventoryExtensionItemUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var itemIdentifier = ItemConstants.InventoryExtension;
        var extensionItems = configuration.Items
            .Where(item => item.Group == itemIdentifier.Group && item.Number == itemIdentifier.Number)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(extensionItems, Has.Count.EqualTo(1), "the item which unlocks an inventory extension must exist exactly once");
            Assert.That(extensionItems[0].Durability, Is.EqualTo(1), "it must last exactly one use, because it gets consumed");
        });
    }

    /// <summary>
    /// Tests that Potion Girl Amy sells the inventory extension item for the configured price.
    /// </summary>
    [Test]
    public async Task TestInventoryExtensionItemIsSoldByPotionGirlAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new AddInventoryExtensionItemToPotionGirlStoreUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var potionGirl = configuration.Monsters.Single(monster => monster.Number == 253);
        var storeItem = potionGirl.MerchantStore!.Items.Single(item => item.Definition!.Group == 14 && item.Definition.Number == 90);
        Assert.Multiple(() =>
        {
            Assert.That(storeItem.Durability, Is.EqualTo(1), "one purchase must unlock exactly one extension");
            Assert.That(new ItemPriceCalculator().CalculateFinalBuyingPrice(storeItem), Is.EqualTo(500_000_000));
        });
    }

    /// <summary>
    /// Tests that the bot feature update creates, enables and preserves one configuration idempotently.
    /// </summary>
    [Test]
    public async Task TestEnableBotFeatureUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var typeId = typeof(BotFeaturePlugIn).GUID;
        gameConfiguration.PlugInConfigurations.Remove(gameConfiguration.PlugInConfigurations.Single(configuration => configuration.TypeId == typeId));

        var update = new EnableBotFeatureUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        var plugInConfiguration = gameConfiguration.PlugInConfigurations.Single(configuration => configuration.TypeId == typeId);
        var configuration = plugInConfiguration.GetConfiguration<BotConfiguration>(null);
        Assert.Multiple(() =>
        {
            Assert.That(plugInConfiguration.IsActive, Is.True);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration!.Enabled, Is.True);
            Assert.That(configuration.NumberOfAccounts, Is.EqualTo(2));
            Assert.That(configuration.MaxCharactersPerAccount, Is.EqualTo(1));
            Assert.That(configuration.BotCapacityPercent, Is.EqualTo(20));
            Assert.That(configuration.PresenceRotation, Is.False);
            Assert.That(configuration.StartAsFreshCharacters, Is.True);
        });
    }

    /// <summary>
    /// Tests that the Potion Girl store repair produces a loadable, non-duplicated store idempotently.
    /// </summary>
    [Test]
    public async Task TestRepairPotionGirlStoreUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var potionGirl = configuration.Monsters.Single(monster => monster.Number == 253);
        var update = new RepairPotionGirlStoreUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var storeItems = potionGirl.MerchantStore!.Items;
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => _ = new Storage(InventoryConstants.WarehouseSize, potionGirl.MerchantStore));
            Assert.That(storeItems.Count(item => item.Definition is { Group: 12, Number: 137 } && item.Level == 0), Is.EqualTo(1));
            Assert.That(storeItems.Count(item => item.Definition is { Group: 14, Number: 90 }), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Tests that the GM Gift jewelry update adds one low-rate full-excellent opening idempotently.
    /// </summary>
    [Test]
    public async Task TestGmGiftJewelryUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new ConfigureGmGiftJewelryUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var gift = configuration.Items.Single(item => item is { Group: 14, Number: 52 });
        var equipment = gift.DropItems.Single(group => group.GetId() == new Guid(0x201, 14, 52, 0, 0, 0, 0, 0, 0, 0, 0));
        var jewelry = gift.DropItems.Single(group => group.GetId() == new Guid(0x201, 14, 52, 1, 0, 0, 0, 0, 0, 0, 0));
        Assert.Multiple(() =>
        {
            Assert.That(gift.DropItems, Has.Count.EqualTo(2));
            Assert.That(equipment.Chance, Is.EqualTo(0.99));
            Assert.That(jewelry.ItemType, Is.EqualTo(SpecialItemType.FullExcellent));
            Assert.That(jewelry.Chance, Is.EqualTo(0.01));
            Assert.That(jewelry.MinimumLevel, Is.EqualTo(4));
            Assert.That(jewelry.MaximumLevel, Is.EqualTo(4));
            Assert.That(jewelry.PossibleItems.Select(item => (item.Group, item.Number)), Is.EquivalentTo(new[]
            {
                ((byte)13, (short)8), ((byte)13, (short)9), ((byte)13, (short)12), ((byte)13, (short)13),
                ((byte)13, (short)21), ((byte)13, (short)22), ((byte)13, (short)23), ((byte)13, (short)24),
                ((byte)13, (short)25), ((byte)13, (short)26), ((byte)13, (short)27), ((byte)13, (short)28),
            }));
        });
    }

    /// <summary>
    /// Tests the randomized spawns, local player respawn, and combat scaling of the Kalima 7 update.
    /// </summary>
    [Test]
    public async Task TestConfigureKalimaSevenUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new ConfigureKalimaSevenUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var map = configuration.Maps.Single(map => map is { Number: 36, Discriminator: 0 });
        var regularNumbers = new HashSet<short> { 331, 332, 333, 334, 335, 336, 337 };
        var regularSpawns = map.MonsterSpawns.Where(spawn => spawn.MonsterDefinition is { } monster && regularNumbers.Contains(monster.Number)).ToList();
        var bossSpawn = map.MonsterSpawns.Single(spawn => spawn.MonsterDefinition?.Number == 275);
        var entrance = map.ExitGates.Single(gate => gate.IsSpawnGate);
        Assert.Multiple(() =>
        {
            Assert.That(regularSpawns, Has.Count.EqualTo(60));
            Assert.That(regularSpawns, Is.All.Matches<MonsterSpawnArea>(spawn => spawn is { X1: 28, X2: 121, Y1: 6, Y2: 109, Quantity: 1, Direction: Direction.Undefined }));
            Assert.That(bossSpawn, Has.Property(nameof(MonsterSpawnArea.X1)).EqualTo(26));
            Assert.That(bossSpawn, Has.Property(nameof(MonsterSpawnArea.X2)).EqualTo(26));
            Assert.That(bossSpawn, Has.Property(nameof(MonsterSpawnArea.Y1)).EqualTo(76));
            Assert.That(bossSpawn, Has.Property(nameof(MonsterSpawnArea.Y2)).EqualTo(76));
            Assert.That(bossSpawn.Quantity, Is.EqualTo(1));
            Assert.That(map.SafezoneMap, Is.SameAs(map));
            Assert.That(entrance, Has.Property(nameof(ExitGate.X1)).EqualTo(10));
            Assert.That(entrance, Has.Property(nameof(ExitGate.X2)).EqualTo(17));
            Assert.That(entrance, Has.Property(nameof(ExitGate.Y1)).EqualTo(16));
            Assert.That(entrance, Has.Property(nameof(ExitGate.Y2)).EqualTo(22));
        });

        var expectedRegularStats = new (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[]
        {
            (331, 820_000, 71_200, 75_200, 5_840, 18_700, 6_930),
            (332, 880_000, 75_100, 79_100, 6_150, 19_360, 7_260),
            (333, 973_000, 80_500, 84_500, 6_600, 20_240, 7_590),
            (334, 1_100_000, 87_000, 91_500, 7_200, 21_120, 8_140),
            (335, 1_250_000, 95_100, 99_600, 7_830, 22_330, 8_910),
            (336, 1_450_000, 104_000, 108_500, 8_650, 23_760, 9_680),
            (337, 1_700_000, 116_800, 121_300, 9_920, 26_180, 10_670),
        };
        foreach (var expected in expectedRegularStats)
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == expected.Number);
            Assert.Multiple(() =>
            {
                Assert.That(monster.RespawnDelay, Is.EqualTo(TimeSpan.FromSeconds(5)));
                Assert.That(monster[Stats.MaximumHealth], Is.EqualTo(expected.Health));
                Assert.That(monster[Stats.MinimumPhysBaseDmg], Is.EqualTo(expected.MinimumDamage));
                Assert.That(monster[Stats.MaximumPhysBaseDmg], Is.EqualTo(expected.MaximumDamage));
                Assert.That(monster[Stats.DefenseBase], Is.EqualTo(expected.Defense));
                Assert.That(monster[Stats.AttackRatePvm], Is.EqualTo(expected.AttackRate));
                Assert.That(monster[Stats.DefenseRatePvm], Is.EqualTo(expected.DefenseRate));
            });
        }

        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        Assert.Multiple(() =>
        {
            Assert.That(boss[Stats.MaximumHealth], Is.EqualTo(120_000_000));
            Assert.That(boss[Stats.MinimumPhysBaseDmg], Is.EqualTo(60_000));
            Assert.That(boss[Stats.MaximumPhysBaseDmg], Is.EqualTo(72_000));
            Assert.That(boss[Stats.DefenseBase], Is.EqualTo(60_000));
            Assert.That(boss[Stats.AttackRatePvm], Is.EqualTo(60_000));
            Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(48_000));
        });
    }

    /// <summary>
    /// Tests that the Kalima 7 difficulty update triples only regular monster attributes idempotently.
    /// </summary>
    [Test]
    public async Task TestIncreaseKalimaSevenDifficultyUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        await new ConfigureKalimaSevenUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var update = new IncreaseKalimaSevenDifficultyUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        foreach (var expected in new (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[]
        {
            (331, 820_000, 71_200, 75_200, 5_840, 18_700, 6_930),
            (332, 880_000, 75_100, 79_100, 6_150, 19_360, 7_260),
            (333, 973_000, 80_500, 84_500, 6_600, 20_240, 7_590),
            (334, 1_100_000, 87_000, 91_500, 7_200, 21_120, 8_140),
            (335, 1_250_000, 95_100, 99_600, 7_830, 22_330, 8_910),
            (336, 1_450_000, 104_000, 108_500, 8_650, 23_760, 9_680),
            (337, 1_700_000, 116_800, 121_300, 9_920, 26_180, 10_670),
        })
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == expected.Number);
            Assert.Multiple(() =>
            {
                Assert.That(monster[Stats.MaximumHealth], Is.EqualTo(expected.Health * 3));
                Assert.That(monster[Stats.MinimumPhysBaseDmg], Is.EqualTo(expected.MinimumDamage * 3));
                Assert.That(monster[Stats.MaximumPhysBaseDmg], Is.EqualTo(expected.MaximumDamage * 3));
                Assert.That(monster[Stats.DefenseBase], Is.EqualTo(expected.Defense * 3));
                Assert.That(monster[Stats.AttackRatePvm], Is.EqualTo(expected.AttackRate * 3));
                Assert.That(monster[Stats.DefenseRatePvm], Is.EqualTo(expected.DefenseRate * 3));
            });
        }

        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        Assert.Multiple(() =>
        {
            Assert.That(boss[Stats.MaximumHealth], Is.EqualTo(120_000_000));
            Assert.That(boss[Stats.MinimumPhysBaseDmg], Is.EqualTo(60_000));
            Assert.That(boss[Stats.MaximumPhysBaseDmg], Is.EqualTo(72_000));
            Assert.That(boss[Stats.DefenseBase], Is.EqualTo(60_000));
            Assert.That(boss[Stats.AttackRatePvm], Is.EqualTo(60_000));
            Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(48_000));
        });
    }

    /// <summary>
    /// Tests that the Kalima 7 retune reduces regular monster attributes and raises the box chance idempotently.
    /// </summary>
    [Test]
    public async Task TestRetuneKalimaSevenRegularMonstersUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        await new RestrictInstantServerDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureKalimaSevenUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new IncreaseKalimaSevenDifficultyUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureKalimaSevenRegularDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var update = new RetuneKalimaSevenRegularMonstersUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        foreach (var expected in new (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[]
        {
            (331, 820_000, 71_200, 75_200, 5_840, 18_700, 6_930),
            (332, 880_000, 75_100, 79_100, 6_150, 19_360, 7_260),
            (333, 973_000, 80_500, 84_500, 6_600, 20_240, 7_590),
            (334, 1_100_000, 87_000, 91_500, 7_200, 21_120, 8_140),
            (335, 1_250_000, 95_100, 99_600, 7_830, 22_330, 8_910),
            (336, 1_450_000, 104_000, 108_500, 8_650, 23_760, 9_680),
            (337, 1_700_000, 116_800, 121_300, 9_920, 26_180, 10_670),
        })
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == expected.Number);
            Assert.Multiple(() =>
            {
                Assert.That(monster[Stats.MaximumHealth], Is.EqualTo(expected.Health * 2.1f));
                Assert.That(monster[Stats.MinimumPhysBaseDmg], Is.EqualTo(expected.MinimumDamage * 2.1f));
                Assert.That(monster[Stats.MaximumPhysBaseDmg], Is.EqualTo(expected.MaximumDamage * 2.1f));
                Assert.That(monster[Stats.DefenseBase], Is.EqualTo(expected.Defense * 2.1f));
                Assert.That(monster[Stats.AttackRatePvm], Is.EqualTo(expected.AttackRate * 2.1f));
                Assert.That(monster[Stats.DefenseRatePvm], Is.EqualTo(expected.DefenseRate * 2.1f));
            });
        }

        var boxFourGroup = configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, 36, 1, 0, 0, 0, 0, 0, 0, 0));
        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        Assert.Multiple(() =>
        {
            Assert.That(boxFourGroup.Chance, Is.EqualTo(0.2));
            Assert.That(boss[Stats.MaximumHealth], Is.EqualTo(120_000_000));
            Assert.That(boss[Stats.MinimumPhysBaseDmg], Is.EqualTo(60_000));
            Assert.That(boss[Stats.MaximumPhysBaseDmg], Is.EqualTo(72_000));
            Assert.That(boss[Stats.DefenseBase], Is.EqualTo(60_000));
            Assert.That(boss[Stats.AttackRatePvm], Is.EqualTo(60_000));
            Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(48_000));
        });
    }

    /// <summary>
    /// Tests that the follow-up Kalima 7 strength reduction is idempotent and leaves Kundun unchanged.
    /// </summary>
    [Test]
    public async Task TestReduceKalimaSevenRegularMonsterStrengthUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        await new RestrictInstantServerDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureKalimaSevenRegularDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureKalimaSevenUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new IncreaseKalimaSevenDifficultyUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new RetuneKalimaSevenRegularMonstersUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var update = new ReduceKalimaSevenRegularMonsterStrengthUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        foreach (var expected in new (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[]
        {
            (331, 820_000, 71_200, 75_200, 5_840, 18_700, 6_930),
            (332, 880_000, 75_100, 79_100, 6_150, 19_360, 7_260),
            (333, 973_000, 80_500, 84_500, 6_600, 20_240, 7_590),
            (334, 1_100_000, 87_000, 91_500, 7_200, 21_120, 8_140),
            (335, 1_250_000, 95_100, 99_600, 7_830, 22_330, 8_910),
            (336, 1_450_000, 104_000, 108_500, 8_650, 23_760, 9_680),
            (337, 1_700_000, 116_800, 121_300, 9_920, 26_180, 10_670),
        })
        {
            var monster = configuration.Monsters.Single(monster => monster.Number == expected.Number);
            Assert.Multiple(() =>
            {
                Assert.That(monster[Stats.MaximumHealth], Is.EqualTo(expected.Health * 1.47f));
                Assert.That(monster[Stats.MinimumPhysBaseDmg], Is.EqualTo(expected.MinimumDamage * 1.47f));
                Assert.That(monster[Stats.MaximumPhysBaseDmg], Is.EqualTo(expected.MaximumDamage * 1.47f));
                Assert.That(monster[Stats.DefenseBase], Is.EqualTo(expected.Defense * 1.47f));
                Assert.That(monster[Stats.AttackRatePvm], Is.EqualTo(expected.AttackRate * 1.47f));
                Assert.That(monster[Stats.DefenseRatePvm], Is.EqualTo(expected.DefenseRate * 1.47f));
            });
        }

        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        Assert.Multiple(() =>
        {
            Assert.That(boss[Stats.MaximumHealth], Is.EqualTo(120_000_000));
            Assert.That(boss[Stats.MinimumPhysBaseDmg], Is.EqualTo(60_000));
            Assert.That(boss[Stats.MaximumPhysBaseDmg], Is.EqualTo(72_000));
            Assert.That(boss[Stats.DefenseBase], Is.EqualTo(60_000));
            Assert.That(boss[Stats.AttackRatePvm], Is.EqualTo(60_000));
            Assert.That(boss[Stats.DefenseRatePvm], Is.EqualTo(48_000));
        });
    }


    /// <summary>
    /// Tests that the follow-up Kalima 7 box drops update raises Box +4 chance and adds Box +5 idempotently.
    /// </summary>
    [Test]
    public async Task TestIncreaseKalimaSevenBoxDropsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        await new ConfigureKalimaSevenUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new IncreaseKalimaSevenDifficultyUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new RestrictInstantServerDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ConfigureKalimaSevenRegularDropsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new RetuneKalimaSevenRegularMonstersUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new ReduceKalimaSevenRegularMonsterStrengthUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var update = new IncreaseKalimaSevenBoxDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var boxOfKundun = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
        var regularNumbers = new HashSet<short> { 331, 332, 333, 334, 335, 336, 337 };
        var regularMonsters = configuration.Monsters.Where(monster => regularNumbers.Contains(monster.Number)).ToList();

        var boxFourGroup = configuration.DropItemGroups.Single(group => group.ItemLevel == 11 && group.Chance == 0.5);
        var boxFiveGroup = configuration.DropItemGroups.Single(group => group.ItemLevel == 12 && group.Chance == 0.4);

        Assert.Multiple(() =>
        {
            Assert.That(boxFourGroup.Chance, Is.EqualTo(0.5));
            Assert.That(boxFiveGroup.Chance, Is.EqualTo(0.4));
            Assert.That(boxFiveGroup.ItemLevel, Is.EqualTo(12));
            Assert.That(boxFiveGroup.PossibleItems.Single(), Is.EqualTo(boxOfKundun));
            Assert.That(regularMonsters, Has.All.Matches<MonsterDefinition>(monster => monster.DropItemGroups.Contains(boxFourGroup) && monster.DropItemGroups.Contains(boxFiveGroup)));
        });
    }

    /// <summary>
    /// Tests that Rhea sells one Ancient item with the +4 option required for Seed Extraction.
    /// </summary>
    [Test]
    public async Task TestAddAncientSeedExtractionMaterialToRheaStoreUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new AddAncientSeedExtractionMaterialToRheaStoreUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var items = configuration.Monsters.Single(monster => monster.Number == 416).MerchantStore!.Items
            .Where(item => item.Definition is { Group: 7, Number: 40 }
                           && item.ItemSetGroups.Any(group => group.ItemSetGroup?.Name == "Semeden" && group.AncientSetDiscriminator == 2))
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].ItemOptions.Single(option => option.ItemOption?.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Tests that Sphere (4) and Sphere (5) can drop from monsters.
    /// </summary>
    [Test]
    public async Task TestEnableSphereFourAndFiveMonsterDropsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new EnableSphereFourAndFiveMonsterDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var sphere4 = configuration.Items.Single(item => item is { Group: 12, Number: 73 });
        var sphere5 = configuration.Items.Single(item => item is { Group: 12, Number: 74 });
        var sphere4Groups = configuration.DropItemGroups
            .Where(group => group.PossibleItems.Any(item => item is { Group: 12, Number: 73 }))
            .ToList();
        var sphere5Groups = configuration.DropItemGroups
            .Where(group => group.PossibleItems.Any(item => item is { Group: 12, Number: 74 }))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sphere4.DropLevel, Is.EqualTo(142));
            Assert.That(sphere4.DropsFromMonsters, Is.True);
            Assert.That(sphere5.DropLevel, Is.EqualTo(145));
            Assert.That(sphere5.DropsFromMonsters, Is.True);

            Assert.That(sphere4Groups, Has.Count.EqualTo(1));
            Assert.That(sphere4Groups[0].Chance, Is.EqualTo(0.02));
            Assert.That(sphere4Groups[0].MinimumMonsterLevel, Is.EqualTo(142));
            Assert.That(sphere4Groups[0].PossibleItems, Is.EquivalentTo(new[] { sphere4 }));

            Assert.That(sphere5Groups, Has.Count.EqualTo(1));
            Assert.That(sphere5Groups[0].Chance, Is.EqualTo(0.02));
            Assert.That(sphere5Groups[0].MinimumMonsterLevel, Is.EqualTo(145));
            Assert.That(sphere5Groups[0].PossibleItems, Is.EquivalentTo(new[] { sphere5 }));

            Assert.That(
                configuration.Maps.All(map => map.DropItemGroups.Count(group => group.PossibleItems.Any(item => item is { Group: 12, Number: 73 })) == 1),
                Is.True);
            Assert.That(
                configuration.Maps.All(map => map.DropItemGroups.Count(group => group.PossibleItems.Any(item => item is { Group: 12, Number: 74 })) == 1),
                Is.True);
        });
    }

    /// <summary>
    /// Tests that the client master skill nodes are added idempotently.
    /// </summary>
    [Test]
    public async Task TestAddClientMasterSkillsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var update = new AddRemainingClientMasterSkillsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        short[] skillNumbers =
        [
            364, 371, 372, 506, 536, 538, 539,
            549, 550, 593, 594, 595, 596, 597, 598,
            602, 609, 610, 611, 612, 613, 614, 615, 616,
            315, 316, 317, 318, 319, 320, 322, 324, 341,
            366, 367, 368, 369, 370, 375, 377, 407, 410,
            412, 443, 446, 447, 473, 476, 478, 505, 507,
        ];

        Assert.Multiple(() =>
        {
            foreach (var skillNumber in skillNumbers)
            {
                var skills = configuration.Skills.Where(skill => skill.Number == skillNumber).ToList();
                Assert.That(skills, Has.Count.EqualTo(1), $"Expected exactly one skill with number {skillNumber}.");
                Assert.That(skills[0].MasterDefinition, Is.Not.Null, $"Expected a master definition for skill {skillNumber}.");
            }

            Assert.That(configuration.Skills.Single(skill => skill.Number == 595).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.TotalEnergy));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 598).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.TotalStrength));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 610).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.CriticalDamageChance));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 616).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.ShieldAfterMonsterKillMultiplier));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 315).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.DefenseFinal));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 317).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.TotalEnergy));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 320).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.TotalStrength));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 367).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.FullyRecoverManaAfterHitChance));
            Assert.That(configuration.Skills.Single(skill => skill.Number == 443).MasterDefinition!.TargetAttribute, Is.EqualTo(Stats.MaximumPhysBaseDmg));
        });
    }

    /// <summary>
    /// Tests that the Lumen event-ticket update replaces her shop inventory idempotently.
    /// </summary>
    [Test]
    public async Task TestConfigureLumenEventTicketsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var legacyComponent = context.CreateNew<Item>();
        legacyComponent.Definition = configuration.Items.Single(item => item is { Group: 13, Number: 17 });
        legacyComponent.Durability = 1;
        legacyComponent.Level = 1;
        legacyComponent.ItemSlot = 24;
        configuration.Monsters.Single(monster => monster.Number == 255).MerchantStore!.Items.Add(legacyComponent);
        var update = new ConfigureLumenEventTicketsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var expectedItems = Enumerable.Range(1, 8).Select(level => ((byte)13, (short)18, (byte)level))
            .Concat(Enumerable.Range(1, 7).Select(level => ((byte)14, (short)19, (byte)level)))
            .Concat(Enumerable.Range(1, 6).Select(level => ((byte)13, (short)51, (byte)level)))
            .Append(((byte)14, (short)28, (byte)7))
            .Append(((byte)14, (short)9, (byte)0))
            .Append(((byte)13, (short)29, (byte)0));
        var shopItems = configuration.Monsters.Single(monster => monster.Number == 255).MerchantStore!.Items;
        Assert.Multiple(() =>
        {
            Assert.That(shopItems, Has.Count.EqualTo(24));
            Assert.That(shopItems.Select(item => (item.Definition!.Group, item.Definition.Number, item.Level)), Is.EquivalentTo(expectedItems));
            Assert.That(shopItems.Single(item => item.Definition is { Group: 14, Number: 9 }).Durability, Is.EqualTo(1));
            Assert.DoesNotThrow(() => _ = new Storage(InventoryConstants.WarehouseSize, configuration.Monsters.Single(monster => monster.Number == 255).MerchantStore!));
        });
    }

    /// <summary>
    /// Tests that the restricted-drop update repairs existing maps, monsters, and Potion Girl stock idempotently.
    /// </summary>
    [Test]
    public async Task TestRestrictInstantServerDropsUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var randomItems = configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            configuration.Maps.First().DropItemGroups.Add(randomItems);
            configuration.Monsters.First(monster => monster.ObjectKind == NpcObjectKind.Monster).DropItemGroups.Add(randomItems);
            configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0)).Chance = 1.0;
            var update = new RestrictInstantServerDropsUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the Potion Girl crafting-stock update rebuilds her store idempotently.
    /// </summary>
    [Test]
    public async Task TestExpandPotionGirlCraftingStockUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            configuration.Monsters.Single(monster => monster.Number == 253).MerchantStore!.Items.Clear();
            var update = new ExpandPotionGirlCraftingStockUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the instant wing-crafting update repairs existing settings idempotently.
    /// </summary>
    [Test]
    public async Task TestConfigureInstantWingCraftingUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using (var context = contextProvider.CreateNewContext())
        {
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
            var crafting = configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings.Single(item => item.Number == 39);
            crafting.ItemCraftingHandlerClassName = typeof(ThirdWingsCrafting).FullName!;
            crafting.SimpleCraftingSettings!.SuccessPercent = 1;
            crafting.SimpleCraftingSettings.MaximumSuccessPercent = 40;
            crafting.SimpleCraftingSettings.ResultItemLuckOptionChance = 5;
            crafting.SimpleCraftingSettings.ResultItemExcellentOptionChance = 0;

            var update = new ConfigureInstantWingCraftingUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        }

        await this.AssertInstantServerConfigurationAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that applying the update for Crest of Monarch in Season 6 is idempotent.
    /// </summary>
    [Test]
    public async Task TestSeason6CrestOfMonarchUpdatePlugInAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var map = gameConfiguration.Maps.First(m => m.Number == IcarusMapNumber && m.Discriminator == 0);

        if (gameConfiguration.DropItemGroups.FirstOrDefault(group => group.GetId() == CrestDropGroupId) is { } existingCrestGroup)
        {
            map.DropItemGroups.Remove(existingCrestGroup);
            gameConfiguration.DropItemGroups.Remove(existingCrestGroup);
        }

        var update = new AddCrestOfMonarchDropGroupUpdateSeason6();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        var groups = gameConfiguration.DropItemGroups.Where(group => group.GetId() == CrestDropGroupId).ToList();
        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(map.DropItemGroups.Count(group => group.GetId() == CrestDropGroupId), Is.EqualTo(1));
        Assert.That(groups[0].Chance, Is.EqualTo(0.001));
        Assert.That(groups[0].MinimumMonsterLevel, Is.EqualTo((byte)82));
        Assert.That(groups[0].ItemLevel, Is.EqualTo((byte)1));
        Assert.That(groups[0].PossibleItems, Has.Count.EqualTo(1));
        Assert.That(groups[0].PossibleItems.Single().Group, Is.EqualTo((byte)13));
        Assert.That(groups[0].PossibleItems.Single().Number, Is.EqualTo((short)14));
    }

    /// <summary>
    /// Tests the data initialization using the in-memory persistence.
    /// </summary>
    [Test]
    public async Task Test075DataAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new Version075.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        await this.TestIfItemsFitIntoInventoriesAsync(contextProvider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests the data initialization using the in-memory persistence.
    /// </summary>
    [Test]
    public async Task Test095dDataAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new Version095d.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        await this.TestIfItemsFitIntoInventoriesAsync(contextProvider).ConfigureAwait(false);
    }

    private async Task TestDataInitializationAsync(IPersistenceContextProvider contextProvider)
    {
        var initialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(3, true).ConfigureAwait(false);

        // Loading game configuration
        using var context = contextProvider.CreateNewConfigurationContext();
        var gameConfiguraton = (await context.GetAsync<DataModel.Configuration.GameConfiguration>().ConfigureAwait(false)).FirstOrDefault();
        Assert.That(gameConfiguraton, Is.Not.Null);

        await this.AssertCastleSiegeDataAsync(contextProvider).ConfigureAwait(false);

        // Testing loading of an account
        using var accountContext = contextProvider.CreateNewPlayerContext(gameConfiguraton!);
        var account1 = await accountContext.GetAccountByLoginNameAsync("test1", "test1").ConfigureAwait(false);
        Assert.That(account1, Is.Not.Null);
        Assert.That(account1!.LoginName, Is.EqualTo("test1"));
    }

    private async Task TestIfItemsFitIntoInventoriesAsync(IPersistenceContextProvider contextProvider)
    {
        using var configContext = contextProvider.CreateNewConfigurationContext();
        var config = (await configContext.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();

        using var context = contextProvider.CreateNewPlayerContext(config);
        var characters = (await context.GetAccountsOrderedByLoginNameAsync(0, 100).ConfigureAwait(false)).SelectMany(a => a.Characters).ToList();
        Assert.That(characters, Is.Not.Empty);
        byte inventorySize = (byte)(InventoryConstants.EquippableSlotsCount + 64);
        foreach (var character in characters)
        {
            try
            {
                var storage = character.Inventory!;
                var inventory = new Storage(inventorySize, InventoryConstants.EquippableSlotsCount, 0, storage);
                Assert.That(inventory.Items.Count(), Is.EqualTo(storage.Items.Count));
            }
            catch (Exception ex)
            {
                Assert.Warn($"{ex.Message} Character: {character.Name}");
            }
        }
    }

    private async Task AssertCastleSiegeDataAsync(IPersistenceContextProvider contextProvider)
    {
        using var configurationContext = contextProvider.CreateNewConfigurationContext();
        var gameConfiguration = (await configurationContext.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var configuration = gameConfiguration.CastleSiegeConfiguration;
        Assert.That(configuration, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(configuration!.Enabled, Is.True);
            Assert.That(configuration.CrownHoldTimeSeconds, Is.EqualTo(30));
            Assert.That(configuration.RegisterMinLevel, Is.EqualTo(200));
            Assert.That(configuration.RegisterMinMembers, Is.EqualTo(20));
            Assert.That(configuration.ParticipantRewardMinSeconds, Is.EqualTo(60));
            Assert.That(configuration.MaxAttackingGuilds, Is.EqualTo(3));
            Assert.That(configuration.GateBuyPrice, Is.EqualTo(9_500_000));
            Assert.That(configuration.StatueBuyPrice, Is.EqualTo(4_500_000));
            Assert.That(configuration.GateRepairCostPerHealthPoint, Is.EqualTo(5));
            Assert.That(configuration.StatueRepairCostPerHealthPoint, Is.EqualTo(3));
            Assert.That(configuration.RepairCostPerUpgradeLevel, Is.EqualTo(1_000_000));
            Assert.That(configuration.CastleSiegeMapDefinition?.Number, Is.EqualTo(30));
            Assert.That(configuration.LandOfTrialsMapDefinition?.Number, Is.EqualTo(31));
            Assert.That(configuration.SignOfLordItemDefinition?.Group, Is.EqualTo(14));
            Assert.That(configuration.SignOfLordItemDefinition?.Number, Is.EqualTo(21));
            Assert.That(configuration.SignOfLordItemDefinition?.MaximumItemLevel, Is.GreaterThanOrEqualTo(3));
            Assert.That(configuration.SignOfLordItemLevel, Is.EqualTo(3));
            Assert.That(configuration.DefenseRespawnArea, Is.Not.Null);
            Assert.That(configuration.AttackRespawnArea, Is.Not.Null);
        });
        Assert.That(
            gameConfiguration.MagicEffects
                .Where(effect => Enum.IsDefined(typeof(CastleSiegeMagicEffectNumber), effect.Number))
                .Select(effect => effect.Number),
            Is.EquivalentTo(Enum.GetValues<CastleSiegeMagicEffectNumber>().Select(number => (short)number)));

        var expectedSchedule = new (CastleSiegeState State, DayOfWeek Day, byte Hour, byte Minute)[]
        {
            (CastleSiegeState.Idle1, DayOfWeek.Sunday, 0, 0),
            (CastleSiegeState.RegisterGuild, DayOfWeek.Monday, 0, 0),
            (CastleSiegeState.Idle2, DayOfWeek.Tuesday, 0, 0),
            (CastleSiegeState.RegisterMark, DayOfWeek.Wednesday, 0, 0),
            (CastleSiegeState.Idle3, DayOfWeek.Thursday, 0, 0),
            (CastleSiegeState.Notify, DayOfWeek.Friday, 0, 0),
            (CastleSiegeState.Ready, DayOfWeek.Saturday, 18, 0),
            (CastleSiegeState.Start, DayOfWeek.Saturday, 20, 0),
            (CastleSiegeState.End, DayOfWeek.Saturday, 22, 0),
            (CastleSiegeState.EndCycle, DayOfWeek.Saturday, 22, 5),
        };
        var actualSchedule = configuration!.StateSchedule
            .Select(entry => (entry.State, entry.DayOfWeek, entry.Hour, entry.Minute));
        Assert.That(actualSchedule, Is.EquivalentTo(expectedSchedule));

        var expectedNpcs = new (short Number, byte Instance, bool Persisted, CastleSiegeJoinSide Side, byte X, byte Y, Direction Direction)[]
        {
            (216, 1, false, CastleSiegeJoinSide.Attack1, 176, 212, Direction.SouthWest),
            (217, 1, false, CastleSiegeJoinSide.Attack1, 167, 194, Direction.NorthWest),
            (218, 1, false, CastleSiegeJoinSide.Attack1, 184, 195, Direction.NorthWest),
            (219, 1, false, CastleSiegeJoinSide.Defense, 93, 208, Direction.SouthWest),
            (219, 2, false, CastleSiegeJoinSide.Defense, 81, 165, Direction.SouthWest),
            (219, 3, false, CastleSiegeJoinSide.Defense, 107, 165, Direction.SouthWest),
            (219, 4, false, CastleSiegeJoinSide.Defense, 67, 118, Direction.SouthWest),
            (219, 5, false, CastleSiegeJoinSide.Defense, 93, 118, Direction.SouthWest),
            (219, 6, false, CastleSiegeJoinSide.Defense, 119, 118, Direction.SouthWest),
            (221, 1, false, CastleSiegeJoinSide.Attack1, 63, 19, Direction.NorthEast),
            (221, 2, false, CastleSiegeJoinSide.Attack1, 119, 19, Direction.NorthEast),
            (222, 1, false, CastleSiegeJoinSide.Defense, 80, 188, Direction.SouthWest),
            (222, 2, false, CastleSiegeJoinSide.Defense, 105, 188, Direction.SouthWest),
            (277, 1, true, CastleSiegeJoinSide.Defense, 93, 204, Direction.SouthWest),
            (277, 2, true, CastleSiegeJoinSide.Defense, 81, 161, Direction.SouthWest),
            (277, 3, true, CastleSiegeJoinSide.Defense, 107, 161, Direction.SouthWest),
            (277, 4, true, CastleSiegeJoinSide.Defense, 67, 114, Direction.SouthWest),
            (277, 5, true, CastleSiegeJoinSide.Defense, 93, 114, Direction.SouthWest),
            (277, 6, true, CastleSiegeJoinSide.Defense, 119, 114, Direction.SouthWest),
            (283, 1, true, CastleSiegeJoinSide.Defense, 94, 227, Direction.SouthWest),
            (283, 2, true, CastleSiegeJoinSide.Defense, 94, 182, Direction.SouthWest),
            (283, 3, true, CastleSiegeJoinSide.Defense, 82, 130, Direction.SouthWest),
            (283, 4, true, CastleSiegeJoinSide.Defense, 107, 130, Direction.SouthWest),
        };
        var actualNpcs = configuration.NpcDefinitions.Select(
            definition =>
                (definition.MonsterDefinition!.Number,
                 definition.InstanceId,
                 definition.IsPersistedToDatabase,
                 definition.DefaultSide,
                 definition.SpawnX,
                 definition.SpawnY,
                 definition.Direction));
        Assert.That(actualNpcs, Is.EquivalentTo(expectedNpcs));
        Assert.That(
            gameConfiguration.Monsters.Select(monster => monster.Number),
            Is.SupersetOf(new short[] { 216, 217, 218, 219, 220, 221, 222, 223, 224, 277, 283 }));

        this.AssertUpgrades(configuration.GateDefenseUpgrades, [(0, 0, 0, 100), (1, 2, 3_000_000, 180), (2, 3, 3_000_000, 300), (3, 4, 3_000_000, 520)]);
        this.AssertUpgrades(configuration.StatueDefenseUpgrades, [(0, 0, 0, 80), (1, 3, 3_000_000, 180), (2, 5, 3_000_000, 340), (3, 7, 3_000_000, 550)]);
        this.AssertUpgrades(configuration.GateLifeUpgrades, [(0, 0, 0, 1_900_000), (1, 2, 1_000_000, 2_500_000), (2, 3, 1_000_000, 3_500_000), (3, 4, 1_000_000, 5_200_000)]);
        this.AssertUpgrades(configuration.StatueLifeUpgrades, [(0, 0, 0, 1_500_000), (1, 3, 1_000_000, 2_200_000), (2, 5, 1_000_000, 3_400_000), (3, 7, 1_000_000, 5_000_000)]);
        this.AssertUpgrades(configuration.StatueRegenUpgrades, [(0, 0, 0, 0), (1, 3, 5_000_000, 1), (2, 5, 5_000_000, 2), (3, 7, 5_000_000, 3)]);

        this.AssertZones(configuration.AttackMachineZones, [(62, 103, 72, 112), (88, 104, 124, 111), (116, 105, 124, 112), (73, 86, 105, 103)]);
        this.AssertZones(configuration.DefenseMachineZones, [(61, 88, 93, 108), (92, 89, 127, 111), (84, 52, 102, 66)]);
        this.AssertZone(configuration.DefenseRespawnArea!, (74, 144, 115, 154));
        this.AssertZone(configuration.AttackRespawnArea!, (35, 11, 144, 48));

        using (var dataContext = contextProvider.CreateNewContext(gameConfiguration))
        {
            var data = (await dataContext.GetAsync<CastleSiegeData>().ConfigureAwait(false)).Single();
            Assert.Multiple(() =>
            {
                Assert.That(data.OwnerGuildId, Is.Null);
                Assert.That(data.IsOccupied, Is.False);
                Assert.That(data.TaxChaos, Is.Zero);
                Assert.That(data.TaxStore, Is.Zero);
                Assert.That(data.TaxHunt, Is.Zero);
                Assert.That(data.IsHuntZoneEnabled, Is.False);
                Assert.That(data.TributeMoney, Is.Zero);
                Assert.That(data.NpcStates, Has.Count.EqualTo(10));
                Assert.That(data.NpcStates.Count(state => state.MonsterNumber == 277 && state.CurrentHp == 1_900_000), Is.EqualTo(6));
                Assert.That(data.NpcStates.Count(state => state.MonsterNumber == 283 && state.CurrentHp == 1_500_000), Is.EqualTo(4));
            });

            Assert.That(await dataContext.GetAsync<CastleSiegeGuildRegistration>().ConfigureAwait(false), Is.Empty);

            data.TaxStore = 1;
            data.NpcStates.First().CurrentHp--;
            Assert.That(await dataContext.SaveChangesAsync().ConfigureAwait(false), Is.True);
        }

        using var reloadedContext = contextProvider.CreateNewContext(gameConfiguration);
        var reloadedData = (await reloadedContext.GetAsync<CastleSiegeData>().ConfigureAwait(false)).Single();
        Assert.That(reloadedData.TaxStore, Is.EqualTo(1));
        Assert.That(reloadedData.NpcStates, Has.One.Matches<CastleSiegeNpcState>(state => state.CurrentHp is 1_899_999 or 1_499_999));
    }

    private async Task AssertCastleSiegeUpdatePlugInAsync(InMemoryPersistenceContextProvider contextProvider)
    {
        using var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var existingConfiguration = gameConfiguration.CastleSiegeConfiguration!;
        var existingData = (await context.GetAsync<CastleSiegeData>().ConfigureAwait(false)).Single();
        gameConfiguration.CastleSiegeConfiguration = null;
        Assert.That(await context.DeleteAsync(existingConfiguration).ConfigureAwait(false), Is.True);
        Assert.That(await context.DeleteAsync(existingData).ConfigureAwait(false), Is.True);

        var update = new AddCastleSiegeDataUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(gameConfiguration.CastleSiegeConfiguration, Is.Not.Null);
        Assert.That(await context.GetAsync<CastleSiegeData>().ConfigureAwait(false), Has.Exactly(1).Items);

        var configuration = gameConfiguration.CastleSiegeConfiguration!;
        var signOfLord = gameConfiguration.Items.Single(item => item.Group == 14 && item.Number == 21);
        configuration.SignOfLordItemDefinition = null;
        configuration.SignOfLordItemLevel = 0;
        signOfLord.MaximumItemLevel = 0;

        var registrationUpdate = new ConfigureCastleSiegeRegistrationUpdatePlugIn();
        await registrationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await registrationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.SignOfLordItemDefinition, Is.SameAs(signOfLord));
            Assert.That(configuration.SignOfLordItemLevel, Is.EqualTo(3));
            Assert.That(signOfLord.MaximumItemLevel, Is.EqualTo(3));
        });

        var customSignOfLord = gameConfiguration.Items.First(item => item != signOfLord);
        configuration.SignOfLordItemDefinition = customSignOfLord;
        configuration.SignOfLordItemLevel = 1;
        await registrationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(configuration.SignOfLordItemDefinition, Is.SameAs(customSignOfLord));
            Assert.That(configuration.SignOfLordItemLevel, Is.EqualTo(1));
        });

        gameConfiguration.Items.Remove(signOfLord);
        configuration.SignOfLordItemDefinition = null;
        configuration.SignOfLordItemLevel = 0;
        await registrationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(configuration.SignOfLordItemDefinition, Is.Null);
            Assert.That(configuration.SignOfLordItemLevel, Is.Zero);
        });

        foreach (var participantEffect in gameConfiguration.MagicEffects
                     .Where(effect => Enum.IsDefined(typeof(CastleSiegeMagicEffectNumber), effect.Number))
                     .ToList())
        {
            gameConfiguration.MagicEffects.Remove(participantEffect);
        }

        var participationUpdate = new ConfigureCastleSiegeParticipationUpdatePlugIn();
        await participationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await participationUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.That(
            gameConfiguration.MagicEffects
                .Where(effect => Enum.IsDefined(typeof(CastleSiegeMagicEffectNumber), effect.Number))
                .Select(effect => effect.Number),
            Is.EquivalentTo(Enum.GetValues<CastleSiegeMagicEffectNumber>().Select(number => (short)number)));

        var senior = gameConfiguration.Monsters.Single(monster => monster.Number == 223);
        Assert.That(senior.NpcWindow, Is.EqualTo(NpcWindow.CastleSeniorNPC));
        senior.NpcWindow = NpcWindow.Undefined;
        var economyUpdate = new ConfigureCastleSiegeEconomyUpdatePlugIn();
        await economyUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await economyUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.That(senior.NpcWindow, Is.EqualTo(NpcWindow.CastleSeniorNPC));

        var lifeStone = gameConfiguration.Monsters.Single(monster => monster.Number == 278);
        var maximumHealth = lifeStone.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == Stats.MaximumHealth.Id);
        maximumHealth.Value = 12_345;
        var defense = lifeStone.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == Stats.DefenseBase.Id);
        lifeStone.Attributes.Remove(defense);
        var lifeStoneUpdate = new ConfigureCastleSiegeLifeStoneUpdatePlugIn();
        await lifeStoneUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await lifeStoneUpdate.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(maximumHealth.Value, Is.EqualTo(12_345));
            Assert.That(
                lifeStone.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == Stats.DefenseBase.Id).Value,
                Is.Zero);
        });

        gameConfiguration.Items.Add(signOfLord);
        configuration.SignOfLordItemDefinition = signOfLord;
        configuration.SignOfLordItemLevel = 3;
    }

    private void AssertUpgrades(
        IEnumerable<CastleSiegeUpgradeDefinition> actual,
        IEnumerable<(byte Level, int JewelCount, int Zen, int Value)> expected)
    {
        Assert.That(
            actual.Select(upgrade => (upgrade.Level, upgrade.RequiredJewelOfGuardianCount, upgrade.RequiredZen, upgrade.Value)),
            Is.EquivalentTo(expected));
    }

    private void AssertZones(
        IEnumerable<CastleSiegeZoneDefinition> actual,
        IEnumerable<(byte X1, byte Y1, byte X2, byte Y2)> expected)
    {
        Assert.That(actual.Select(zone => (zone.X1, zone.Y1, zone.X2, zone.Y2)), Is.EquivalentTo(expected));
    }

    private void AssertZone(CastleSiegeZoneDefinition actual, (byte X1, byte Y1, byte X2, byte Y2) expected)
    {
        Assert.That((actual.X1, actual.Y1, actual.X2, actual.Y2), Is.EqualTo(expected));
    }
}
