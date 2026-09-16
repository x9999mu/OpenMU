// <copyright file="InstantServerConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the high-rate, instant-server settings used by the Season 6 realm.
/// </summary>
internal static class InstantServerConfiguration
{
    /// <summary>
    /// Gets the global experience multiplier.
    /// </summary>
    internal const float ExperienceRate = 9999f;

    /// <summary>
    /// Gets the points granted per level.
    /// </summary>
    internal const float PointsPerLevel = 500f;

    /// <summary>
    /// Gets the maximum master level.
    /// </summary>
    internal const short MaximumMasterLevel = 400;

    /// <summary>
    /// Gets the master experience multiplier.
    /// </summary>
    internal const float MasterExperienceRate = 1_000f;

    /// <summary>
    /// Gets the master points granted per level.
    /// </summary>
    internal const float MasterPointsPerLevel = 5f;

    /// <summary>
    /// Gets the minimum automatic spawn quantity.
    /// </summary>
    internal const short MonsterPackSize = 10;

    /// <summary>
    /// Gets the picked-up Zen multiplier.
    /// </summary>
    internal const float MoneyAmountRate = 1_000f;

    /// <summary>
    /// Gets the regular-monster jewel drop chance.
    /// </summary>
    internal const double JewelDropChance = 0.01;

    /// <summary>
    /// Gets the success rate of every wing and cape crafting.
    /// </summary>
    internal const byte WingCraftingSuccessRate = 100;

    /// <summary>
    /// Gets the independent chance for each supported special wing option.
    /// </summary>
    internal const byte WingSpecialOptionChance = 90;

    /// <summary>
    /// Gets the regular monster respawn delay.
    /// </summary>
    internal static readonly TimeSpan MonsterRespawnDelay = TimeSpan.FromSeconds(5);

    private static readonly byte[] DarkWizardClasses = [0, 2, 3];
    private static readonly byte[] DarkKnightClasses = [4, 6, 7];
    private static readonly byte[] FairyElfClasses = [8, 10, 11];
    private static readonly byte[] MagicGladiatorClasses = [12, 13];
    private static readonly byte[] DarkLordClasses = [16, 17];
    private static readonly byte[] SummonerClasses = [20, 22, 23];
    private static readonly byte[] RageFighterClasses = [24, 25];
    private static readonly short[] SpecialistMerchantNumbers = [230, 242, 243, 245, 246, 251, 254, 416, 417];
    private static readonly short[] GeneralGoodsMerchantNumbers = [253, 259, 376, 377, 415, 545, 577];
    private static readonly HashSet<short> BossMonsterNumbers =
    [
        43, 44, 53, 54, 78, 79, 80, 81, 82, 83, 135, 161, 181, 189, 197, 267, 275, 295, 338, 361, 362, 363, 364, 440, 459,
    ];

    private static readonly (byte Group, short Number)[] KundunFourDirectItems =
    [
        (0, 16), (0, 17), (0, 18), (0, 19), (0, 20), (0, 21), (0, 31), (0, 33), (0, 34),
        (2, 11), (2, 12), (2, 13), (2, 15), (3, 10), (4, 16), (4, 17), (4, 18), (4, 19),
        (4, 20), (5, 8), (5, 9), (5, 10), (5, 11), (5, 13), (5, 19), (6, 13), (6, 15), (6, 16),
    ];
    private static readonly short[] KundunFourArmorSets = [17, 21, 18, 22, 19, 24, 20, 23, 27, 28, 42, 44, 60, 61];
    private static readonly (byte Group, short Number)[] KundunFiveDirectItems =
    [
        (0, 22), (0, 23), (0, 26), (0, 27), (0, 28), (0, 35), (2, 14), (4, 21), (5, 12),
        (5, 19), (5, 20), (5, 30), (5, 31),
    ];
    private static readonly short[] KundunFiveArmorSets = [29, 30, 31, 32, 33, 43, 73];
    private static readonly short[] GmGiftArmorSets = [29, 30, 31, 32, 33, 43, 73, 45, 46, 47, 48, 49, 50, 51, 52, 53];
    private static readonly (short Number, float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate)[] IcarusMonsterStats =
    [
        (69, 750_000, 18_000, 24_000, 8_000, 20_000, 12_000),
        (70, 950_000, 18_000, 24_000, 8_000, 20_000, 12_000),
        (71, 750_000, 18_000, 24_000, 8_000, 20_000, 12_000),
        (72, 2_050_000, 18_000, 24_000, 8_500, 20_700, 12_000),
        (73, 1_450_000, 18_000, 24_000, 8_000, 20_000, 12_000),
        (74, 1_725_000, 18_000, 24_000, 8_000, 20_000, 12_000),
        (75, 2_500_000, 19_500, 24_000, 9_900, 24_000, 12_200),
        (76, 3_650_000, 25_500, 28_800, 11_600, 25_200, 12_200),
        (77, 4_750_000, 28_500, 30_000, 12_000, 27_000, 14_000),
    ];

    /// <summary>
    /// Gets the merchants whose stores are rebuilt by this configuration.
    /// </summary>
    internal static IReadOnlyCollection<short> RebuiltMerchantNumbers { get; } = SpecialistMerchantNumbers.Concat(GeneralGoodsMerchantNumbers).ToArray();

    /// <summary>
    /// Applies settings which are stored on the game configuration.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        gameConfiguration.ExperienceRate = ExperienceRate;
        gameConfiguration.AreaSkillHitsPlayer = true;
        gameConfiguration.ExcellentItemDropLevelDelta = 0;
        ConfigureMasterProgression(context, gameConfiguration);
        ConfigureMoneyAmountRate(context, gameConfiguration);
        ConfigureDropRates(gameConfiguration);
        ConfigureLevelUpPoints(gameConfiguration);
        ConfigureMonsterPacks(gameConfiguration);
        ConfigurePvp(gameConfiguration);
        ConfigurePotionStacks(gameConfiguration);
        ConfigureMerchantStores(context, gameConfiguration);
        ConfigureGameplayBalance(context, gameConfiguration);
    }

    /// <summary>
    /// Configures the built-in boss invasions as a non-overlapping thirty-minute circuit.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureBossEvents(GameConfiguration gameConfiguration)
    {
        ConfigureBossEvent<GoldenInvasionPlugIn>(gameConfiguration, TimeSpan.Zero);
        ConfigureBossEvent<RedDragonInvasionPlugIn>(gameConfiguration, TimeSpan.FromMinutes(10));
        ConfigureBossEvent<WhiteWizardInvasionPlugIn>(gameConfiguration, TimeSpan.FromMinutes(20));
    }

    /// <summary>
    /// Raises the amount of Zen credited from every money drop.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureMoneyAmountRate(IContext context, GameConfiguration gameConfiguration)
    {
        var moneyRate = gameConfiguration.GlobalBaseAttributeValues
            .FirstOrDefault(attribute => attribute.Definition?.Id == Stats.MoneyAmountRate.Id);
        if (moneyRate?.Value == MoneyAmountRate)
        {
            return;
        }

        if (moneyRate is not null)
        {
            gameConfiguration.GlobalBaseAttributeValues.Remove(moneyRate);
        }

        var definition = gameConfiguration.Attributes.First(attribute => attribute.Id == Stats.MoneyAmountRate.Id);
        gameConfiguration.GlobalBaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(MoneyAmountRate, definition, AggregateType.AddRaw));
    }

    /// <summary>
    /// Repairs the points-per-level class templates.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureLevelUpPoints(GameConfiguration gameConfiguration)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            if (characterClass.StatAttributes.FirstOrDefault(attribute => attribute.Attribute == Stats.PointsPerLevelUp) is { } pointsPerLevel)
            {
                pointsPerLevel.BaseValue = PointsPerLevel;
            }
        }

        foreach (var stat in new[] { Stats.BaseStrength, Stats.BaseAgility, Stats.BaseVitality, Stats.BaseEnergy, Stats.BaseLeadership })
        {
            if (gameConfiguration.Attributes.FirstOrDefault(attribute => attribute == stat) is { } persistentStat)
            {
                persistentStat.MaximumValue = 32_767;
            }
        }
    }

    /// <summary>
    /// Configures master-level limits, experience, and points per level.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureMasterProgression(IContext context, GameConfiguration gameConfiguration)
    {
        gameConfiguration.MaximumMasterLevel = MaximumMasterLevel;
        gameConfiguration.MasterExperienceRate = MasterExperienceRate;
        var masterPointsDefinition = gameConfiguration.Attributes.Single(attribute => attribute.Id == Stats.MasterPointsPerLevelUp.Id);
        foreach (var characterClass in gameConfiguration.CharacterClasses.Where(characterClass => characterClass.IsMasterClass))
        {
            var masterPoints = characterClass.BaseAttributeValues.FirstOrDefault(attribute => attribute.Definition?.Id == Stats.MasterPointsPerLevelUp.Id);
            if (masterPoints?.Value == MasterPointsPerLevel)
            {
                continue;
            }

            if (masterPoints is not null)
            {
                characterClass.BaseAttributeValues.Remove(masterPoints);
            }

            characterClass.BaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(MasterPointsPerLevel, masterPointsDefinition, AggregateType.AddRaw));
        }
    }

    /// <summary>
    /// Guarantees Luck on instant-server shop equipment and Box of Kundun rewards.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureGuaranteedLuck(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in gameConfiguration.Monsters
                     .Where(monster => RebuiltMerchantNumbers.Contains(monster.Number))
                     .SelectMany(monster => monster.MerchantStore?.Items ?? []))
        {
            var luck = item.Definition?.PossibleItemOptions
                .SelectMany(definition => definition.PossibleOptions)
                .FirstOrDefault(option => option.OptionType == ItemOptionTypes.Luck);
            if (luck is not null && item.ItemOptions.All(link => link.ItemOption != luck))
            {
                var link = context.CreateNew<ItemOptionLink>();
                link.ItemOption = luck;
                item.ItemOptions.Add(link);
            }
        }

        var kundunBox = GetItemDefinition(gameConfiguration, 14, 11);
        foreach (var group in kundunBox.DropItems.Where(group => group.SourceItemLevel is >= 8 and <= 10))
        {
            group.ItemType = SpecialItemType.ExcellentWithLuck;
        }
    }

    /// <summary>
    /// Configures every Chaos Machine mix with a fixed instant-server success rate.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureChaosMachineCraftings(GameConfiguration gameConfiguration)
    {
        var craftings = gameConfiguration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings;
        foreach (var crafting in craftings)
        {
            crafting.ItemCraftingHandlerClassName = crafting.Number switch
            {
                8 => typeof(InstantServerBloodCastleTicketCrafting).FullName!,
                2 => typeof(InstantServerDevilSquareTicketCrafting).FullName!,
                37 => typeof(InstantServerIllusionTempleTicketCrafting).FullName!,
                28 => typeof(InstantServerFenrirUpgradeCrafting).FullName!,
                7 or 11 or 24 or 38 or 39 => typeof(InstantServerWingCrafting).FullName!,
                _ => crafting.ItemCraftingHandlerClassName,
            };

            if (crafting.SimpleCraftingSettings is not { } settings)
            {
                continue;
            }

            settings.SuccessPercent = 100;
            settings.MaximumSuccessPercent = 100;
            settings.NpcPriceDivisor = 0;
            settings.SuccessPercentageAdditionForLuck = 0;
            settings.SuccessPercentageAdditionForExcellentItem = 0;
            settings.SuccessPercentageAdditionForAncientItem = 0;
            settings.SuccessPercentageAdditionForGuardianItem = 0;
            settings.SuccessPercentageAdditionForSocketItem = 0;
            foreach (var requiredItem in settings.RequiredItems)
            {
                requiredItem.AddPercentage = 0;
                requiredItem.NpcPriceDivisor = 0;
            }

            if (crafting.Number is 7 or 11 or 24 or 38 or 39)
            {
                settings.ResultItemLuckOptionChance = 100;
                settings.ResultItemExcellentOptionChance = WingSpecialOptionChance;
                settings.ResultItemMaxExcOptionCount = 4;
            }
        }
    }

    /// <summary>
    /// Rebuilds Potion Girl Amy's store with the instant-server utility and crafting stock.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigurePotionGirlStore(IContext context, GameConfiguration gameConfiguration)
    {
        ConfigureSpecialistStore(context, gameConfiguration, 253, packer =>
        {
            AddGeneralGoods(context, gameConfiguration, packer);
            AddClassChangeAndWingItems(context, gameConfiguration, packer);
            AddInventoryExtensionItem(context, gameConfiguration, packer);
        });
    }

    /// <summary>
    /// Adds the item which unlocks an inventory extension to the store.
    /// Its price is hard-coded in the item price calculator.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="packer">The store packer.</param>
    private static void AddInventoryExtensionItem(IContext context, GameConfiguration gameConfiguration, MerchantStorePacker packer)
    {
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 14, 90)));
    }

    /// <summary>
    /// Applies the x9999 crafting, equipment, loot, and monster balance settings.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureGameplayBalance(IContext context, GameConfiguration gameConfiguration)
    {
        ConfigureGacha(context, gameConfiguration);
        ConfigureGuaranteedLuck(context, gameConfiguration);
        ConfigureChaosMachineCraftings(gameConfiguration);
        ConfigureShopEquipment(context, gameConfiguration);
        ConfigureIcarusBoxDrop(context, gameConfiguration);
        ConfigureIcarusDifficulty(gameConfiguration);
        ConfigureBossDifficulty(gameConfiguration);
    }

    /// <summary>
    /// Configures guaranteed Box of Kundun and GM Gift rewards for Illusion of Kundun 7.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureIllusionOfKundunSevenDrops(IContext context, GameConfiguration gameConfiguration)
    {
        const short monsterNumber = 275;
        var boss = gameConfiguration.Monsters.Single(monster => monster.Number == monsterNumber);
        var genericBossGroups = new[]
        {
            GuidHelper.CreateGuid<DropItemGroup>(9_999, 4),
            GuidHelper.CreateGuid<DropItemGroup>(9_999, 5),
            GuidHelper.CreateGuid<DropItemGroup>(9_999, 6),
        };
        foreach (var group in boss.DropItemGroups.Where(group => genericBossGroups.Contains(group.GetId())).ToList())
        {
            boss.DropItemGroups.Remove(group);
        }

        var kundunBox = GetItemDefinition(gameConfiguration, 14, 11);
        var gmGift = GetItemDefinition(gameConfiguration, 14, 52);
        var drops = new[]
        {
            (Item: kundunBox, Level: (byte)11, Tier: 1, Name: "Box of Kundun +4"),
            (Item: kundunBox, Level: (byte)12, Tier: 2, Name: "Box of Kundun +5"),
            (Item: gmGift, Level: (byte)0, Tier: 3, Name: "GM Gift"),
        };
        const int copiesPerDrop = 6;
        boss.NumberOfMaximumItemDrops = (byte)(drops.Length * copiesPerDrop);

        foreach (var drop in drops)
        {
            for (var copy = 1; copy <= copiesPerDrop; copy++)
            {
                var id = GuidHelper.CreateGuid<DropItemGroup>(9_999, monsterNumber, (byte)((drop.Tier * 10) + copy));
                var group = gameConfiguration.DropItemGroups.FirstOrDefault(group => group.GetId() == id);
                if (group is null)
                {
                    group = context.CreateNew<DropItemGroup>();
                    group.SetGuid(id);
                    gameConfiguration.DropItemGroups.Add(group);
                }

                group.Description = $"Illusion of Kundun 7: {drop.Name} #{copy}";
                group.Chance = 1.0;
                group.ItemType = SpecialItemType.RandomItem;
                group.ItemLevel = drop.Level;
                group.MinimumMonsterLevel = null;
                group.MaximumMonsterLevel = null;
                group.Monster = boss;
                group.PossibleItems.Clear();
                group.PossibleItems.Add(drop.Item);
                if (!boss.DropItemGroups.Contains(group))
                {
                    boss.DropItemGroups.Add(group);
                }
            }
        }
    }

    /// <summary>
    /// Adds the low-rate full-excellent jewelry opening to GM Gift.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ConfigureGmGiftJewelry(IContext context, GameConfiguration gameConfiguration)
    {
        var gift = GetItemDefinition(gameConfiguration, 14, 52);
        var jewelryId = GuidHelper.CreateGuid<ItemDropItemGroup>(gift.Group, gift.Number, 1);
        var jewelry = gift.DropItems.FirstOrDefault(group => group.GetId() == jewelryId);
        if (jewelry is null)
        {
            jewelry = context.CreateNew<ItemDropItemGroup>();
            jewelry.SetGuid(jewelryId);
            gift.DropItems.Add(jewelry);
        }

        jewelry.SourceItemLevel = 0;
        jewelry.ItemType = SpecialItemType.FullExcellent;
        jewelry.Chance = 0.01;
        jewelry.MinimumLevel = 4;
        jewelry.MaximumLevel = 4;
        jewelry.Description = "Full Excellent Jewelry Gacha (GM Gift)";
        jewelry.DropEffect = ItemDropEffect.FanfareSound;
        jewelry.PossibleItems.Clear();
        foreach (var item in gameConfiguration.Items.Where(item => item.Group == 13 && item.Number is 8 or 9 or 12 or 13 or >= 21 and <= 28))
        {
            jewelry.PossibleItems.Add(item);
        }

        gift.DropItems.Single(group => group.GetId() == GuidHelper.CreateGuid<ItemDropItemGroup>(gift.Group, gift.Number, 0)).Chance = 0.99;
    }

    private static void ConfigureShopEquipment(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in gameConfiguration.Monsters.SelectMany(monster => monster.MerchantStore?.Items ?? []))
        {
            var option = item.Definition?.PossibleItemOptions
                .SelectMany(definition => definition.PossibleOptions)
                .FirstOrDefault(possibleOption => possibleOption.OptionType == ItemOptionTypes.Option);
            if (option is null)
            {
                continue;
            }

            item.Level = 9;
            var links = item.ItemOptions.Where(link => link.ItemOption?.OptionType == ItemOptionTypes.Option).ToList();
            var link = links.FirstOrDefault();
            if (link is null)
            {
                link = context.CreateNew<ItemOptionLink>();
                link.ItemOption = option;
                item.ItemOptions.Add(link);
            }

            link.Level = 4;
            foreach (var duplicate in links.Skip(1))
            {
                item.ItemOptions.Remove(duplicate);
            }
        }
    }

    private static void ConfigureIcarusBoxDrop(IContext context, GameConfiguration gameConfiguration)
    {
        var id = GuidHelper.CreateGuid<DropItemGroup>(9_999, 10, 4);
        var group = gameConfiguration.DropItemGroups.FirstOrDefault(item => item.GetId() == id);
        if (group is null)
        {
            group = context.CreateNew<DropItemGroup>();
            group.SetGuid(id);
            gameConfiguration.DropItemGroups.Add(group);
        }

        group.Description = "Icarus Box of Kundun +4";
        group.Chance = 0.05;
        group.ItemType = SpecialItemType.RandomItem;
        group.ItemLevel = 11;
        group.MinimumMonsterLevel = null;
        group.MaximumMonsterLevel = null;
        group.Monster = null;
        group.PossibleItems.Clear();
        group.PossibleItems.Add(GetItemDefinition(gameConfiguration, 14, 11));

        var icarus = gameConfiguration.Maps.Single(map => map is { Number: 10, Discriminator: 0 });
        if (!icarus.DropItemGroups.Contains(group))
        {
            icarus.DropItemGroups.Add(group);
        }
    }

    private static void ConfigureIcarusDifficulty(GameConfiguration gameConfiguration)
    {
        var icarus = gameConfiguration.Maps.Single(map => map is { Number: 10, Discriminator: 0 });
        foreach (var spawn in icarus.MonsterSpawns.Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster }))
        {
            spawn.Quantity = 3;
        }

        foreach (var stats in IcarusMonsterStats)
        {
            var monster = gameConfiguration.Monsters.Single(monster => monster.Number == stats.Number);
            SetMonsterAttribute(monster, Stats.MaximumHealth, stats.Health);
            SetMonsterAttribute(monster, Stats.MinimumPhysBaseDmg, stats.MinimumDamage);
            SetMonsterAttribute(monster, Stats.MaximumPhysBaseDmg, stats.MaximumDamage);
            SetMonsterAttribute(monster, Stats.DefenseBase, stats.Defense);
            SetMonsterAttribute(monster, Stats.AttackRatePvm, stats.AttackRate);
            SetMonsterAttribute(monster, Stats.DefenseRatePvm, stats.DefenseRate);
        }
    }

    private static void ConfigureBossDifficulty(GameConfiguration gameConfiguration)
    {
        foreach (var boss in gameConfiguration.Monsters.Where(monster => BossMonsterNumbers.Contains(monster.Number)))
        {
            var tier = Math.Max(0, GetMonsterAttribute(boss, Stats.Level) - 20);
            var minimumDamage = Math.Clamp(tier * 250, 8_000, 32_000);
            SetMonsterAttributeAtLeast(boss, Stats.MaximumHealth, Math.Clamp(tier * 500_000, 4_000_000, 60_000_000));
            SetMonsterAttributeAtLeast(boss, Stats.MinimumPhysBaseDmg, minimumDamage);
            SetMonsterAttributeAtLeast(boss, Stats.MaximumPhysBaseDmg, Math.Max(minimumDamage + 6_000, GetMonsterAttribute(boss, Stats.MinimumPhysBaseDmg) + 1));
            SetMonsterAttributeAtLeast(boss, Stats.DefenseBase, Math.Clamp(tier * 300, 8_000, 30_000));
            SetMonsterAttributeAtLeast(boss, Stats.AttackRatePvm, Math.Clamp(tier * 250, 12_000, 30_000));
            SetMonsterAttributeAtLeast(boss, Stats.DefenseRatePvm, Math.Clamp(tier * 200, 10_000, 25_000));
        }
    }

    private static float GetMonsterAttribute(MonsterDefinition monster, AttributeDefinition definition)
        => monster.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == definition.Id).Value;

    private static void SetMonsterAttribute(MonsterDefinition monster, AttributeDefinition definition, float value)
        => monster.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == definition.Id).Value = value;

    private static void SetMonsterAttributeAtLeast(MonsterDefinition monster, AttributeDefinition definition, float value)
    {
        var attribute = monster.Attributes.Single(item => item.AttributeDefinition?.Id == definition.Id);
        attribute.Value = Math.Max(attribute.Value, value);
    }

    private static void ConfigureBossEvent<TPlugIn>(GameConfiguration gameConfiguration, TimeSpan offset)
        where TPlugIn : SimpleInvasionPlugIn, new()
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeof(TPlugIn).GUID);
        if (plugInConfiguration is null)
        {
            return;
        }

        var configuration = (PeriodicInvasionConfiguration)new TPlugIn().CreateDefaultConfig();
        configuration.PreStartMessageDelay = TimeSpan.Zero;
        configuration.TaskDuration = TimeSpan.FromMinutes(10);
        configuration.Timetable = PeriodicTaskConfiguration.GenerateTimeSequence(
                TimeSpan.FromMinutes(30),
                TimeOnly.FromTimeSpan(offset))
            .ToList();
        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(configuration, null);
    }

    private static void ConfigureDropRates(GameConfiguration gameConfiguration)
    {
        var moneyGroup = gameConfiguration.DropItemGroups.Single(group => group.GetId() == GuidHelper.CreateGuid<DropItemGroup>(1));
        var jewelGroup = gameConfiguration.DropItemGroups.Single(group => group.GetId() == GuidHelper.CreateGuid<DropItemGroup>(4));
        moneyGroup.Chance = 1.0;
        jewelGroup.Chance = JewelDropChance;

        foreach (var map in gameConfiguration.Maps)
        {
            map.DropItemGroups.Clear();
            map.DropItemGroups.Add(moneyGroup);
        }

        foreach (var requiredItem in gameConfiguration.Monsters
                     .SelectMany(monster => monster.Quests)
                     .SelectMany(quest => quest.RequiredItems)
                     .Where(requiredItem => requiredItem.Item?.IsQuestItem == true))
        {
            requiredItem.DropItemGroup = null;
        }

        foreach (var monster in gameConfiguration.Monsters.Where(monster => monster.ObjectKind == NpcObjectKind.Monster))
        {
            monster.DropItemGroups.Clear();
            monster.NumberOfMaximumItemDrops = 2;
            monster.RespawnDelay = MonsterRespawnDelay;
        }
    }

    private static void ConfigureMonsterPacks(GameConfiguration gameConfiguration)
    {
        var permanentMonsterSpawns = gameConfiguration.Maps
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster });

        foreach (var spawn in permanentMonsterSpawns)
        {
            spawn.Quantity = Math.Max(MonsterPackSize, spawn.Quantity);
            if (spawn.X1 == spawn.X2 && spawn.Y1 == spawn.Y2)
            {
                spawn.X1 = (byte)Math.Max(0, spawn.X1 - 2);
                spawn.X2 = (byte)Math.Min(byte.MaxValue, spawn.X2 + 2);
                spawn.Y1 = (byte)Math.Max(0, spawn.Y1 - 2);
                spawn.Y2 = (byte)Math.Min(byte.MaxValue, spawn.Y2 + 2);
            }
        }
    }

    private static void ConfigurePvp(GameConfiguration gameConfiguration)
    {
        foreach (var miniGame in gameConfiguration.MiniGameDefinitions)
        {
            miniGame.ArePlayerKillersAllowedToEnter = true;
        }
    }

    private static void ConfigurePotionStacks(GameConfiguration gameConfiguration)
    {
        GetItemDefinition(gameConfiguration, 14, 3).Durability = byte.MaxValue;
        GetItemDefinition(gameConfiguration, 14, 6).Durability = byte.MaxValue;
    }

    private static void ConfigureMerchantStores(IContext context, GameConfiguration gameConfiguration)
    {
        ConfigureSpecialistStore(context, gameConfiguration, 254, packer =>
        {
            AddEquipmentProfile(context, gameConfiguration, packer, DarkWizardClasses, 2, [(5, 0), (5, 2)]);
            AddSkillItems(context, gameConfiguration, packer, DarkWizardClasses.Concat(MagicGladiatorClasses));
        });
        ConfigureSpecialistStore(context, gameConfiguration, 251, packer =>
        {
            AddEquipmentProfile(context, gameConfiguration, packer, DarkKnightClasses, 5, [(0, 5), (0, 6)]);
            AddEquipmentProfile(context, gameConfiguration, packer, MagicGladiatorClasses, 15, [(0, 5), (5, 0)]);
            AddEquipmentProfile(context, gameConfiguration, packer, DarkLordClasses, 25, [(2, 8), (2, 9)]);
            AddEquipmentProfile(context, gameConfiguration, packer, RageFighterClasses, 59, [(0, 32), (0, 33)]);
        });
        ConfigureSpecialistStore(context, gameConfiguration, 230, packer =>
            AddSkillItems(context, gameConfiguration, packer, DarkKnightClasses.Concat(DarkLordClasses).Concat(RageFighterClasses)));
        ConfigureSpecialistStore(context, gameConfiguration, 242, packer => AddSkillItems(context, gameConfiguration, packer, FairyElfClasses));
        ConfigureSpecialistStore(context, gameConfiguration, 243, packer =>
            AddEquipmentProfile(context, gameConfiguration, packer, FairyElfClasses, 10, [(4, 0), (4, 3)]));
        ConfigureSpecialistStore(context, gameConfiguration, 416, packer =>
            AddEquipmentProfile(context, gameConfiguration, packer, SummonerClasses, 40, [(5, 15), (5, 21), (5, 22)]));
        ConfigureSpecialistStore(context, gameConfiguration, 417, packer => AddSkillItems(context, gameConfiguration, packer, SummonerClasses));
        ConfigureSpecialistStore(context, gameConfiguration, 245, packer =>
        {
            AddEquipmentProfile(context, gameConfiguration, packer, DarkWizardClasses, 2, [(5, 0), (5, 2)]);
            AddSkillItems(context, gameConfiguration, packer, DarkWizardClasses.Concat(MagicGladiatorClasses));
        });
        ConfigureSpecialistStore(context, gameConfiguration, 246, packer =>
        {
            // ponytail: the client exposes only 120 merchant slots; complete armor sets remain in their reachable home-town stores.
            AddWeaponProfile(
                context,
                gameConfiguration,
                packer,
                DarkKnightClasses.Concat(FairyElfClasses).Concat(MagicGladiatorClasses).Concat(DarkLordClasses).Concat(RageFighterClasses),
                [(0, 5), (0, 6), (4, 0), (4, 3), (5, 0), (2, 8), (2, 9), (0, 32), (0, 33)]);
        });

        foreach (var npcNumber in GeneralGoodsMerchantNumbers.Where(number => number != 253))
        {
            ConfigureSpecialistStore(context, gameConfiguration, npcNumber, packer => AddGeneralGoods(context, gameConfiguration, packer));
        }

        ConfigurePotionGirlStore(context, gameConfiguration);
    }

    private static void ConfigureSpecialistStore(IContext context, GameConfiguration gameConfiguration, short npcNumber, Action<MerchantStorePacker> populate)
    {
        var monster = gameConfiguration.Monsters.First(monster => monster.Number == npcNumber && monster.MerchantStore is not null);
        var store = monster.MerchantStore!;
        store.Items.Clear();
        var packer = new MerchantStorePacker(store, monster);
        populate(packer);
        packer.Complete();
    }

    private static void AddEquipmentProfile(
        IContext context,
        GameConfiguration gameConfiguration,
        MerchantStorePacker packer,
        IEnumerable<byte> classNumbers,
        byte armorSetNumber,
        IEnumerable<(byte Group, byte Number)> weapons)
    {
        var classes = classNumbers.ToHashSet();
        var itemHelper = new ItemHelper(context, gameConfiguration);
        foreach (var group in new[] { ItemGroups.Helm, ItemGroups.Armor, ItemGroups.Pants, ItemGroups.Gloves, ItemGroups.Boots })
        {
            var definition = gameConfiguration.Items.FirstOrDefault(item => item.Group == (byte)group
                                                                            && item.Number == armorSetNumber
                                                                            && item.QualifiedCharacters.Any(characterClass => classes.Contains(characterClass.Number)));
            if (definition is null)
            {
                continue;
            }

            var item = itemHelper.CreateSetItem(0, armorSetNumber, group, Stats.MaximumHealth, level: 7);
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            packer.Add(item);
        }

        foreach (var (group, number) in weapons)
        {
            var definition = GetItemDefinition(gameConfiguration, group, number);
            if (!definition.QualifiedCharacters.Any(characterClass => classes.Contains(characterClass.Number)))
            {
                continue;
            }

            var item = itemHelper.CreateWeapon(0, (ItemGroups)group, number, 7, 0, false, definition.Skill is not null, Stats.ExcellentDamageChance);
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            packer.Add(item);
        }
    }

    private static void AddWeaponProfile(
        IContext context,
        GameConfiguration gameConfiguration,
        MerchantStorePacker packer,
        IEnumerable<byte> classNumbers,
        IEnumerable<(byte Group, byte Number)> weapons)
    {
        var classes = classNumbers.ToHashSet();
        var itemHelper = new ItemHelper(context, gameConfiguration);
        foreach (var (group, number) in weapons.Distinct())
        {
            var definition = GetItemDefinition(gameConfiguration, group, number);
            if (!definition.QualifiedCharacters.Any(characterClass => classes.Contains(characterClass.Number)))
            {
                continue;
            }

            var item = itemHelper.CreateWeapon(0, (ItemGroups)group, number, 7, 0, false, definition.Skill is not null, Stats.ExcellentDamageChance);
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            packer.Add(item);
        }
    }

    private static void AddSkillItems(IContext context, GameConfiguration gameConfiguration, MerchantStorePacker packer, IEnumerable<byte> classNumbers)
    {
        var classes = classNumbers.ToHashSet();
        var definitions = gameConfiguration.Items
            .Where(item => item.Group is 12 or 15
                           && item.ItemSlot is null
                           && item.Skill is not null
                           && item.QualifiedCharacters.Any(characterClass => classes.Contains(characterClass.Number)))
            .OrderBy(item => item.Group)
            .ThenBy(item => item.Number)
            .ToList();

        foreach (var definition in definitions)
        {
            if (definition is { Group: 12, Number: 11 })
            {
                for (byte level = 0; level <= 6; level++)
                {
                    packer.Add(CreateStoreItem(context, definition, level: level));
                }
            }
            else
            {
                packer.Add(CreateStoreItem(context, definition));
            }
        }
    }

    private static void AddGeneralGoods(IContext context, GameConfiguration gameConfiguration, MerchantStorePacker packer)
    {
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 14, 3), byte.MaxValue, 1));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 14, 6), byte.MaxValue, 1));
        var antidote = GetItemDefinition(gameConfiguration, 14, 8);
        packer.Add(CreateStoreItem(context, antidote, Math.Max(1, (int)antidote.Durability)));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 4, 7)));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 4, 15)));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 14, 10)));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 13, 29)));
    }

    private static void AddClassChangeAndWingItems(IContext context, GameConfiguration gameConfiguration, MerchantStorePacker packer)
    {
        foreach (var (number, level) in new (byte Number, byte Level)[]
                 {
                     (23, 0), (23, 1), (24, 0), (24, 1), (25, 0), (26, 0), (65, 0), (66, 0), (67, 0), (68, 0),
                 })
        {
            packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 14, number), level: level));
        }

        var itemHelper = new ItemHelper(context, gameConfiguration);
        foreach (var (group, number) in new (ItemGroups Group, byte Number)[]
                 {
                     (ItemGroups.Scepters, 6), (ItemGroups.Bows, 6), (ItemGroups.Staff, 7),
                 })
        {
            var definition = GetItemDefinition(gameConfiguration, (byte)group, number);
            var item = itemHelper.CreateWeapon(0, group, number, 4, 1, true, definition.Skill is not null, null);
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            packer.Add(item);
        }

        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 13, 14)));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 13, 14), level: 1));
        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 13, 52)));

        foreach (var (group, number) in new (byte Group, short Number)[]
                 {
                     (14, 13), // Jewel of Bless
                     (14, 14), // Jewel of Soul
                     (12, 15), // Jewel of Chaos
                     (14, 16), // Jewel of Life
                     (14, 22), // Jewel of Creation
                 })
        {
            packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, group, number)));
        }

        foreach (var number in new short[] { 30, 31, 136, 137, 141 })
        {
            var definition = GetItemDefinition(gameConfiguration, 12, number);
            for (byte level = 0; level <= definition.MaximumItemLevel; level++)
            {
                packer.Add(CreateStoreItem(context, definition, level: level));
            }
        }

        packer.Add(CreateStoreItem(context, GetItemDefinition(gameConfiguration, 13, 53)));
    }

    private static Item CreateStoreItem(IContext context, ItemDefinition definition, double durability = 1, byte level = 0)
    {
        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = durability;
        item.Level = level;
        return item;
    }

    private static void ConfigureGacha(IContext context, GameConfiguration gameConfiguration)
    {
        var kundunBox = GetItemDefinition(gameConfiguration, 14, 11);
        foreach (var level in Enumerable.Range(8, 5).Select(level => (byte)level))
        {
            var excellentGroup = kundunBox.DropItems.FirstOrDefault(group => group.SourceItemLevel == level && group.ItemType is SpecialItemType.Excellent or SpecialItemType.ExcellentWithLuck or SpecialItemType.FullExcellent)
                                 ?? kundunBox.DropItems.Single(group => group.SourceItemLevel == level);
            excellentGroup.Chance = 1.0;
            foreach (var obsolete in kundunBox.DropItems.Where(group => group.SourceItemLevel == level && group != excellentGroup).ToList())
            {
                kundunBox.DropItems.Remove(obsolete);
            }
        }

        var kundunFourOpening = kundunBox.DropItems.Single(group => group.SourceItemLevel == 11);
        ConfigureFullOptionOpening(gameConfiguration, kundunFourOpening, KundunFourDirectItems, KundunFourArmorSets);
        var kundunFiveOpening = kundunBox.DropItems.Single(group => group.SourceItemLevel == 12);
        ConfigureFullOptionOpening(gameConfiguration, kundunFiveOpening, KundunFiveDirectItems, KundunFiveArmorSets);
        ConfigureJackpotOpening(context, gameConfiguration);
        RemoveLegacyKundunMonsterDrops(gameConfiguration, kundunBox);

        var kundunOne = UpsertGachaGroup(context, gameConfiguration, 1, "Kundun +1 Gacha", 0.02, kundunBox, 8);
        var kundunTwo = UpsertGachaGroup(context, gameConfiguration, 2, "Kundun +2 Gacha", 0.015, kundunBox, 9);
        var kundunThree = UpsertGachaGroup(context, gameConfiguration, 3, "Kundun +3 Gacha", 0.01, kundunBox, 10);
        var kundunFour = UpsertGachaGroup(context, gameConfiguration, 4, "Kundun +4 Boss Gacha", 0.475, kundunBox, 11);
        var kundunFive = UpsertGachaGroup(context, gameConfiguration, 5, "Kundun +5 Boss Gacha", 0.475, kundunBox, 12);
        var jackpotBox = GetItemDefinition(gameConfiguration, 14, 52);
        var jackpot = UpsertGachaGroup(context, gameConfiguration, 6, "Full Option GM Gift Boss Gacha", 0.05, jackpotBox, 0);
        var jewelGroup = gameConfiguration.DropItemGroups.Single(group => group.GetId() == GuidHelper.CreateGuid<DropItemGroup>(4));

        var configuredGroups = new[] { kundunOne, kundunTwo, kundunThree, kundunFour, kundunFive, jackpot };
        foreach (var monster in gameConfiguration.Monsters)
        {
            foreach (var group in configuredGroups)
            {
                monster.DropItemGroups.Remove(group);
            }
        }

        var regularMonsters = gameConfiguration.Maps
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .Select(spawn => spawn.MonsterDefinition!)
            .Where(monster => !BossMonsterNumbers.Contains(monster.Number))
            .Distinct()
            .ToList();
        var bosses = gameConfiguration.Monsters
            .Where(monster => monster.ObjectKind == NpcObjectKind.Monster && BossMonsterNumbers.Contains(monster.Number))
            .ToList();
        foreach (var boss in bosses)
        {
            foreach (var obsolete in boss.DropItemGroups.Where(group => group.Chance < 1.0 && !configuredGroups.Contains(group)).ToList())
            {
                boss.DropItemGroups.Remove(obsolete);
            }
        }

        foreach (var monster in regularMonsters)
        {
            AttachGroups(monster, jewelGroup, kundunOne, kundunTwo, kundunThree);
            ReserveChanceDropSlot(monster);
        }

        foreach (var monster in bosses)
        {
            AttachGroups(monster, kundunFour, kundunFive, jackpot);
            ReserveChanceDropSlot(monster);
        }
    }

    private static void ConfigureJackpotOpening(IContext context, GameConfiguration gameConfiguration)
    {
        var gift = GetItemDefinition(gameConfiguration, 14, 52);
        var jackpotId = GuidHelper.CreateGuid<ItemDropItemGroup>(gift.Group, gift.Number, 0);
        var jackpot = gift.DropItems.FirstOrDefault(group => group.GetId() == jackpotId);
        if (jackpot is null)
        {
            jackpot = context.CreateNew<ItemDropItemGroup>();
            jackpot.SetGuid(jackpotId);
            gift.DropItems.Add(jackpot);
        }

        jackpot.SourceItemLevel = 0;
        jackpot.ItemType = SpecialItemType.FullExcellent;
        jackpot.Chance = 1.0;
        jackpot.MinimumLevel = 9;
        jackpot.MaximumLevel = 9;
        jackpot.Description = "Full Option Gacha Box (GM Gift)";
        jackpot.DropEffect = ItemDropEffect.FanfareSound;
        ReplaceEquipmentPool(gameConfiguration, jackpot, KundunFiveDirectItems, GmGiftArmorSets);
    }

    private static void ConfigureFullOptionOpening(
        GameConfiguration gameConfiguration,
        ItemDropItemGroup group,
        IReadOnlyCollection<(byte Group, short Number)> directItems,
        IReadOnlyCollection<short> armorSets)
    {
        group.ItemType = SpecialItemType.FullExcellent;
        group.Chance = 1.0;
        group.MinimumLevel = 9;
        group.MaximumLevel = 9;
        ReplaceEquipmentPool(gameConfiguration, group, directItems, armorSets);
    }

    private static void ReplaceEquipmentPool(
        GameConfiguration gameConfiguration,
        DropItemGroup group,
        IReadOnlyCollection<(byte Group, short Number)> directItems,
        IReadOnlyCollection<short> armorSets)
    {
        var items = directItems
            .Select(item => GetItemDefinition(gameConfiguration, item.Group, item.Number))
            .Concat(gameConfiguration.Items.Where(item => item.Group is >= 7 and <= 11 && armorSets.Contains(item.Number)))
            .Distinct()
            .ToList();
        group.PossibleItems.Clear();
        foreach (var item in items)
        {
            group.PossibleItems.Add(item);
        }
    }

    private static DropItemGroup UpsertGachaGroup(
        IContext context,
        GameConfiguration gameConfiguration,
        short tier,
        string description,
        double chance,
        ItemDefinition carrier,
        byte itemLevel)
    {
        var id = GuidHelper.CreateGuid<DropItemGroup>(9_999, tier);
        var group = gameConfiguration.DropItemGroups.FirstOrDefault(group => group.GetId() == id);
        if (group is null)
        {
            group = context.CreateNew<DropItemGroup>();
            group.SetGuid(id);
            gameConfiguration.DropItemGroups.Add(group);
        }

        group.Description = description;
        group.Chance = chance;
        group.ItemType = SpecialItemType.RandomItem;
        group.ItemLevel = itemLevel;
        group.MinimumMonsterLevel = null;
        group.MaximumMonsterLevel = null;
        group.Monster = null;
        group.PossibleItems.Clear();
        group.PossibleItems.Add(carrier);
        return group;
    }

    private static void RemoveLegacyKundunMonsterDrops(GameConfiguration gameConfiguration, ItemDefinition kundunBox)
    {
        var legacyGroups = gameConfiguration.DropItemGroups
            .Where(group => group.Monster is not null
                            && group.ItemLevel is >= 8 and <= 12
                            && group.PossibleItems.Count == 1
                            && group.PossibleItems.Single() == kundunBox)
            .ToList();
        foreach (var group in legacyGroups)
        {
            group.Monster?.DropItemGroups.Remove(group);
            gameConfiguration.DropItemGroups.Remove(group);
        }
    }

    private static void AttachGroups(MonsterDefinition monster, params DropItemGroup[] groups)
    {
        foreach (var group in groups)
        {
            if (!monster.DropItemGroups.Contains(group))
            {
                monster.DropItemGroups.Add(group);
            }
        }
    }

    private static void ReserveChanceDropSlot(MonsterDefinition monster)
    {
        monster.NumberOfMaximumItemDrops = 2;
    }

    private static ItemDefinition GetItemDefinition(GameConfiguration gameConfiguration, int group, int number)
        => gameConfiguration.Items.First(item => item.Group == group && item.Number == number);

    private sealed class MerchantStorePacker
    {
        private readonly bool[,] _occupied = new bool[InventoryConstants.WarehouseRows, InventoryConstants.RowSize];
        private readonly MonsterDefinition _merchant;
        private readonly List<Item> _pendingItems = [];

        internal MerchantStorePacker(ItemStorage store, MonsterDefinition merchant)
        {
            _ = new Storage(InventoryConstants.WarehouseSize, store);
            this.Store = store;
            this._merchant = merchant;
        }

        internal ItemStorage Store { get; }

        internal void Add(Item item)
        {
            this._pendingItems.Add(item);
        }

        internal void Complete()
        {
            foreach (var item in this._pendingItems.OrderByDescending(item => item.Definition?.Height).ThenByDescending(item => item.Definition?.Width))
            {
                if (item.Definition is null || !this.TryReserve(item.Definition, out var slot))
                {
                    throw new InvalidOperationException($"Merchant {this._merchant} cannot fit item {item.Definition}.");
                }

                item.ItemSlot = slot;
                this.Store.Items.Add(item);
            }
        }

        private bool TryReserve(ItemDefinition definition, out byte slot)
        {
            for (var row = 0; row <= InventoryConstants.WarehouseRows - definition.Height; row++)
            {
                for (var column = 0; column <= InventoryConstants.RowSize - definition.Width; column++)
                {
                    if (!this.Fits(row, column, definition.Width, definition.Height))
                    {
                        continue;
                    }

                    slot = (byte)((row * InventoryConstants.RowSize) + column);
                    this.MarkOccupied(row, column, definition.Width, definition.Height);
                    return true;
                }
            }

            slot = 0;
            return false;
        }

        private bool Fits(int row, int column, int width, int height)
        {
            for (var y = row; y < row + height; y++)
            {
                for (var x = column; x < column + width; x++)
                {
                    if (this._occupied[y, x])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void MarkOccupied(int row, int column, int width, int height)
        {
            for (var y = row; y < row + height; y++)
            {
                for (var x = column; x < column + width; x++)
                {
                    this._occupied[y, x] = true;
                }
            }
        }
    }
}
