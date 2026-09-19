// <copyright file="KalimaSevenBossDropTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

/// <summary>
/// Tests the drops of the Kalima 7 boss with the drop generator which runs on the game server.
/// </summary>
[TestFixture]
public class KalimaSevenBossDropTest
{
    /// <summary>
    /// Tests that the Illusion of Kundun 7 drops its nine guaranteed items on every kill, including one
    /// random full-option item.
    /// </summary>
    [Test]
    public async ValueTask IllusionOfKundunSevenDropsNineGuaranteedItemsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, false).ConfigureAwait(false);
        using var context = provider.CreateNewConfigurationContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var boss = configuration.Monsters.Single(monster => monster.Number == 275);
        var fullOptionGroup = boss.DropItemGroups.Single(group => group.ItemType == SpecialItemType.FullExcellent);
        var generator = new DefaultDropGenerator(configuration, Rand.GetRandomizer());
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);

        Assert.That(fullOptionGroup.PossibleItems, Is.Not.Empty, "the full-option pool must not be empty");
        Assert.That(fullOptionGroup.PossibleItems.All(item => item.MaximumItemLevel >= 9), Is.True, "the pool must support level 9");

        for (var kill = 0; kill < 10; kill++)
        {
            var (drops, _) = await generator.GenerateItemDropsAsync(boss, 1, player).ConfigureAwait(false);
            var items = drops.ToList();
            var fullOptionItems = items.Where(item => item.Definition is not { Group: 14 }).ToList();
            var fullOptionItem = fullOptionItems.SingleOrDefault();
            Assert.Multiple(() =>
            {
                Assert.That(items, Has.Count.EqualTo(9), "items per kill");
                Assert.That(items.Count(item => item.Definition is { Group: 14, Number: 52 }), Is.EqualTo(3), "GM Gifts");
                Assert.That(items.Count(item => item is { Definition: { Group: 14, Number: 11 }, Level: 12 }), Is.EqualTo(3), "Box of Kundun +5");
                Assert.That(items.Count(item => item.Definition is { Group: 14, Number: 42 }), Is.EqualTo(1), "Jewel of Harmony");
                Assert.That(items.Count(item => item.Definition is { Group: 14, Number: 31 }), Is.EqualTo(1), "Jewel of Guardian");
                Assert.That(fullOptionItem, Is.Not.Null, "one full-option item per kill");
                Assert.That(fullOptionItem!.Level, Is.EqualTo(9), "full-option item level");
                Assert.That(fullOptionItem.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent), Is.True, "excellent lines");
                Assert.That(fullOptionItem.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.True, "luck");
                Assert.That(fullOptionItem.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Option), Is.True, "normal option");
            });
        }
    }
}
