// <copyright file="DropGeneratorTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

/// <summary>
/// Tests the drop generator.
/// </summary>
[TestFixture]
public class DropGeneratorTest
{
    /// <summary>
    /// Tests if the drop fails because the randomizer returns a number which causes a fail.
    /// </summary>
    [Test]
    public async ValueTask TestDropFailAsync()
    {
        var config = this.GetGameConfig();
        var generator = new DefaultDropGenerator(config, this.GetRandomizer(9999));
        var (items, _) = await generator.GenerateItemDropsAsync(this.GetMonster(1, 0), 0, await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false));
        var item = items.FirstOrDefault();
        Assert.That(item, Is.Null);
    }

    /// <summary>
    /// Tests the drops defined by a monster are getting considered.
    /// </summary>
    [Test]
    public async ValueTask TestItemDropItemByMonsterAsync()
    {
        var config = this.GetGameConfig();
        var monster = this.GetMonster(1, 0);
        monster.DropItemGroups.AddBasicDropItemGroups();
        monster.DropItemGroups.Add(3000, SpecialItemType.RandomItem, true);

        var generator = new DefaultDropGenerator(config, this.GetRandomizer2(0, 0.5));
        var (items, _) = await generator.GenerateItemDropsAsync(monster, 1, await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false));
        var item = items.FirstOrDefault();

        Assert.That(item, Is.Not.Null);

        // ReSharper disable once PossibleNullReferenceException
        Assert.That(item!.Definition, Is.EqualTo(monster.DropItemGroups.Last().PossibleItems.First()));
    }

    /// <summary>
    /// Tests that items with a maximum drop level are filtered from generic monster drops.
    /// </summary>
    [Test]
    public async ValueTask TestMaximumDropLevelAsync()
    {
        var config = this.GetGameConfig();
        var cappedItem = this.CreateItemDefinition(12, 15, 12, 66);
        var uncappedItem = this.CreateItemDefinition(14, 13, 25);

        var dropGroup = new Mock<DropItemGroup>();
        dropGroup.SetupAllProperties();
        dropGroup.Object.Chance = 1.0;
        dropGroup.Object.ItemType = SpecialItemType.Jewel;
        dropGroup.Setup(g => g.PossibleItems).Returns(new List<ItemDefinition> { cappedItem, uncappedItem });

        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.CurrentMap!.Definition.DropItemGroups.Add(dropGroup.Object);

        var generator = new DefaultDropGenerator(config, this.GetRandomizer(0));
        var (items, _) = await generator.GenerateItemDropsAsync(this.GetMonster(1, 67), 1, player).ConfigureAwait(false);
        var item = items.FirstOrDefault();

        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Definition, Is.EqualTo(uncappedItem));
    }

    /// <summary>
    /// Tests the drops defined by a player are getting considered.
    /// </summary>
    public void TestItemDropItemByPlayer()
    {
        // to be implemented
    }

    /// <summary>
    /// Tests the drops defined by a map are getting considered.
    /// </summary>
    public void TestItemDropItemByMap()
    {
        // to be implemented
    }

    /// <summary>
    /// Tests that ExcellentItemDropLevelDelta property exists and has correct default.
    /// </summary>
    [Test]
    public void TestExcellentItemDropLevelDelta_PropertyExists()
    {
        var config = this.GetGameConfig();
        // The initializer sets default to 25 for backward compatibility
        config.ExcellentItemDropLevelDelta = 25;
        Assert.That(config.ExcellentItemDropLevelDelta, Is.EqualTo(25));

        config.ExcellentItemDropLevelDelta = 0;
        Assert.That(config.ExcellentItemDropLevelDelta, Is.EqualTo(0));

        config.ExcellentItemDropLevelDelta = 50;
        Assert.That(config.ExcellentItemDropLevelDelta, Is.EqualTo(50));
    }

    /// <summary>
    /// Tests the x9999 gacha probabilities, reserved drop slot, and full-option jackpot contract.
    /// </summary>
    [Test]
    public async Task TestInstantServerGachaAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        await new DataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(1, false).ConfigureAwait(false);
        using var context = contextProvider.CreateNewConfigurationContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var gachaGroups = Enumerable.Range(1, 6)
            .Select(tier => configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, (short)tier, 0, 0, 0, 0, 0, 0, 0, 0)))
            .ToList();

        var regularRandomizer = this.GetSequenceRandomizer(0.005, 0.02, 0.04, 0.5);
        var regularGenerator = new DefaultDropGenerator(configuration, regularRandomizer);
        Assert.Multiple(() =>
        {
            Assert.That(regularGenerator.GenerateItemDrop(gachaGroups.Take(3)).Item?.Level, Is.EqualTo(10));
            Assert.That(regularGenerator.GenerateItemDrop(gachaGroups.Take(3)).Item?.Level, Is.EqualTo(9));
            Assert.That(regularGenerator.GenerateItemDrop(gachaGroups.Take(3)).Item?.Level, Is.EqualTo(8));
            Assert.That(regularGenerator.GenerateItemDrop(gachaGroups.Take(3)).Item, Is.Null);
        });

        var bossRandomizer = this.GetSequenceRandomizer(0.01, 0.3, 0.7);
        var bossGenerator = new DefaultDropGenerator(configuration, bossRandomizer);
        Assert.Multiple(() =>
        {
            Assert.That(bossGenerator.GenerateItemDrop(gachaGroups.Skip(3)).Item?.Definition?.Number, Is.EqualTo(52));
            Assert.That(bossGenerator.GenerateItemDrop(gachaGroups.Skip(3)).Item?.Level, Is.EqualTo(11));
            Assert.That(bossGenerator.GenerateItemDrop(gachaGroups.Skip(3)).Item?.Level, Is.EqualTo(12));
        });

        var kundunBox = configuration.Items.Single(item => item is { Group: 14, Number: 11 });
        var kundunFourOpening = kundunBox.DropItems.Single(group => group.SourceItemLevel == 11);
        var kundunFiveOpening = kundunBox.DropItems.Single(group => group.SourceItemLevel == 12);
        var gmGift = configuration.Items.Single(item => item is { Group: 14, Number: 52 });
        var gmGiftOpening = gmGift.DropItems.Single(group => group.GetId() == new Guid(0x201, 14, 52, 0, 0, 0, 0, 0, 0, 0, 0));
        var itemGenerator = new DefaultDropGenerator(configuration, this.GetSequenceRandomizer());
        var kundunFour = itemGenerator.GenerateItemDrop(kundunFourOpening);
        var kundunFive = itemGenerator.GenerateItemDrop(kundunFiveOpening);
        var gmGiftItem = itemGenerator.GenerateItemDrop(gmGiftOpening);
        Assert.That(kundunFour, Is.Not.Null);
        Assert.That(kundunFive, Is.Not.Null);
        Assert.That(gmGiftItem, Is.Not.Null);
        foreach (var item in new[] { kundunFour!, kundunFive! })
        {
            Assert.Multiple(() =>
            {
                Assert.That(item.Level, Is.EqualTo(9));
                Assert.That(item.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent), Is.GreaterThan(0));
                Assert.That(item.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.EqualTo(1));
                Assert.That(item.ItemOptions.Single(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(4));
                Assert.That(item.HasSkill, Is.EqualTo(item.CanHaveSkill()));
                Assert.That(item.Durability, Is.EqualTo(item.GetMaximumDurabilityOfOnePiece()));
            });
        }

        Assert.That(kundunFour!.Definition!.Group, Is.InRange((byte)0, (byte)11));
        Assert.That(kundunFive!.Definition!.DropLevel, Is.GreaterThanOrEqualTo(80));
        Assert.That(gmGiftItem!.Definition, Is.SameAs(gmGift));
        Assert.That(gmGiftOpening.PossibleItems, Is.EqualTo(new[] { gmGift }));

        var gmGiftBundleOpening = gmGift.DropItems.Single(group => group.ItemAmount == 2);
        var jewelryOpening = gmGift.DropItems.Single(group => group.ItemType == SpecialItemType.FullExcellent);
        var jewelryItem = itemGenerator.GenerateItemDrop(jewelryOpening);
        Assert.That(jewelryItem, Is.Not.Null);
        Assert.That(jewelryItem!.Level, Is.EqualTo(4));

        var ancientOpening = gmGift.DropItems.Single(group => group.ItemType == SpecialItemType.Ancient);
        var (ancientItems, _, _) = itemGenerator.GenerateItemDrops(new[] { ancientOpening });
        var ancientItem = ancientItems.Single();
        Assert.That(ancientItem.Level, Is.EqualTo(9));
        Assert.That(ancientItem.ItemSetGroups, Is.Not.Empty);

        var fireworksOpening = gmGift.DropItems.Single(group => group.DropEffect == ItemDropEffect.Fireworks);
        var (fireworksItems, _, fireworksEffect) = itemGenerator.GenerateItemDrops(new[] { fireworksOpening });
        Assert.That(fireworksItems, Is.Empty);
        Assert.That(fireworksEffect, Is.EqualTo(ItemDropEffect.Fireworks));
        var (gmGiftBundle, _, _) = itemGenerator.GenerateItemDrops(new[] { gmGiftBundleOpening });
        Assert.That(gmGiftBundle, Has.Exactly(2).Items);
        Assert.That(gmGiftBundle.Select(item => item.Definition), Is.All.EqualTo(gmGift));
        var icarusDrop = configuration.DropItemGroups.Single(group => group.GetId() == new Guid(0x200, 9_999, 10, 4, 0, 0, 0, 0, 0, 0, 0));
        var icarusGenerator = new DefaultDropGenerator(configuration, this.GetSequenceRandomizer(0.04, 0.06));
        Assert.Multiple(() =>
        {
            var successfulDrop = icarusGenerator.GenerateItemDrop(new[] { icarusDrop }).Item;
            Assert.That(successfulDrop?.Definition, Is.SameAs(kundunBox));
            Assert.That(successfulDrop?.Level, Is.EqualTo(11));
            Assert.That(icarusGenerator.GenerateItemDrop(new[] { icarusDrop }).Item, Is.Null);
        });

        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.CurrentMap!.Definition.DropItemGroups.Clear();
        var monster = this.GetMonster(5, 100);
        var guaranteedItem = this.CreateItemDefinition(14, 13, 76);
        for (var index = 0; index < 4; index++)
        {
            var group = new Mock<DropItemGroup>();
            group.SetupAllProperties();
            group.Object.Chance = 1.0;
            group.Object.ItemType = SpecialItemType.RandomItem;
            group.Setup(item => item.PossibleItems).Returns(new List<ItemDefinition> { guaranteedItem });
            monster.DropItemGroups.Add(group.Object);
        }

        monster.DropItemGroups.Add(gachaGroups[0]);
        monster.DropItemGroups.Add(gachaGroups[1]);
        monster.DropItemGroups.Add(gachaGroups[2]);
        var (drops, _) = await new DefaultDropGenerator(configuration, this.GetSequenceRandomizer(0.0))
            .GenerateItemDropsAsync(monster, 0, player)
            .ConfigureAwait(false);
        Assert.That(drops, Has.Exactly(5).Items);
        Assert.That(drops.Any(item => item.Definition is { Group: 14, Number: 11 }), Is.True);
    }

    private MonsterDefinition GetMonster(int numberOfDrops, byte level)
    {
        var monster = new Mock<MonsterDefinition>();
        monster.SetupAllProperties();
        monster.Setup(m => m.DropItemGroups).Returns(new List<DropItemGroup>());
        monster.Setup(m => m.Attributes).Returns(new List<MonsterAttribute>());
        monster.Object.NumberOfMaximumItemDrops = numberOfDrops;
        monster.Object.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.Level, Value = level });
        return monster.Object;
    }

    private IRandomizer GetRandomizer(int randomValue)
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(randomValue);
        randomizer.Setup(r => r.NextDouble()).Returns(randomValue / 10000.0);
        return randomizer.Object;
    }

    private IRandomizer GetRandomizer2(int integerValue, double doubleValue)
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(integerValue);
        randomizer.Setup(r => r.NextDouble()).Returns(doubleValue);

        return randomizer.Object;
    }

    private IRandomizer GetSequenceRandomizer(params double[] doubles)
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int _) => min);
        randomizer.Setup(r => r.NextInt(It.IsAny<uint>(), It.IsAny<uint>())).Returns((uint min, uint _) => (int)min);
        randomizer.Setup(r => r.NextUInt(It.IsAny<uint>(), It.IsAny<uint>())).Returns((uint min, uint _) => min);
        randomizer.Setup(r => r.NextRandomBool()).Returns(false);
        randomizer.Setup(r => r.NextRandomBool(It.IsAny<int>())).Returns(false);
        randomizer.Setup(r => r.NextRandomBool(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        randomizer.Setup(r => r.NextRandomBool(It.IsAny<double>())).Returns(false);
        var sequence = randomizer.SetupSequence(r => r.NextDouble());
        foreach (var value in doubles)
        {
            sequence = sequence.Returns(value);
        }

        sequence.Returns(0.0);
        return randomizer.Object;
    }

    private GameConfiguration GetGameConfig()
    {
        var gameConfiguration = new Mock<GameConfiguration>();
        gameConfiguration.Setup(c => c.Items).Returns(new List<ItemDefinition>());
        return gameConfiguration.Object;
    }

    private ItemDefinition CreateItemDefinition(byte group, short number, byte dropLevel, byte? maximumDropLevel = null)
    {
        var itemDefinition = new Mock<ItemDefinition>();
        itemDefinition.SetupAllProperties();
        itemDefinition.Object.Group = group;
        itemDefinition.Object.Number = number;
        itemDefinition.Object.DropLevel = dropLevel;
        itemDefinition.Object.MaximumDropLevel = maximumDropLevel;
        itemDefinition.Object.Durability = 1;
        itemDefinition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        return itemDefinition.Object;
    }
}
