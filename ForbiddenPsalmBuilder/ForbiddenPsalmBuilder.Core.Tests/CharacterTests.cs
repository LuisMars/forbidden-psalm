using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;

namespace ForbiddenPsalmBuilder.Core.Tests;

public class CharacterTests
{
    [Fact]
    public void Character_ShouldInitializeWithDefaults()
    {
        var character = new Character();

        Assert.NotNull(character.Id);
        Assert.NotEmpty(character.Id);
        Assert.Equal(string.Empty, character.Name);
        Assert.NotNull(character.Stats);
        Assert.NotNull(character.Feats);
        Assert.NotNull(character.Flaws);
        Assert.NotNull(character.Equipment);
        Assert.NotNull(character.Injuries);
        Assert.NotNull(character.Spells);
        Assert.Equal(0, character.CurrentGold);
        Assert.Equal(0, character.Tragedies);
        Assert.Null(character.SpecialClassId);
    }

    [Fact]
    public void Character_TragediesShouldBeSettable()
    {
        var character = new Character();

        character.Tragedies = 5;

        Assert.Equal(5, character.Tragedies);
    }

    [Fact]
    public void Character_ShouldInitializeWithName()
    {
        var name = "Test Character";
        var character = new Character(name);

        Assert.Equal(name, character.Name);
        Assert.NotNull(character.Id);
        Assert.NotEmpty(character.Id);
    }

    [Fact]
    public void CanEquip_ShouldReturnTrueWhenSlotsAvailable()
    {
        var character = new Character();
        character.Stats.Strength = 5; // This gives 10 equipment slots (5 + 5)

        var equipment = new Equipment
        {
            Name = "Test Item",
            Slots = 3
        };

        var canEquip = character.CanEquip(equipment);

        Assert.True(canEquip);
    }

    [Fact]
    public void CanEquip_ShouldReturnFalseWhenSlotsExceeded()
    {
        var character = new Character();
        character.Stats.Strength = 0; // This gives 5 equipment slots (5 + 0)
        character.Equipment.Add(new Equipment { Name = "Existing", Slots = 3, IsEquipped = true });

        var equipment = new Equipment
        {
            Name = "Test Item",
            Slots = 3
        };

        var canEquip = character.CanEquip(equipment);

        Assert.False(canEquip);
    }

    [Fact]
    public void CanEquip_ShouldNotDoubleCountInventoryItem_WhenEquipping()
    {
        // Arrange - Character with 5 slots total
        var character = new Character();
        character.Stats.Strength = 0; // This gives 5 equipment slots (5 + 0)

        // Add an equipped item taking 3 slots
        character.Equipment.Add(new Equipment { Id = "sword-1", Name = "Equipped Sword", Slots = 3, IsEquipped = true });

        // Add an unequipped item (in inventory) taking 2 slots
        var inventoryItem = new Equipment { Id = "armor-1", Name = "Inventory Armor", Slots = 2, IsEquipped = false };
        character.Equipment.Add(inventoryItem);

        // Act - Try to equip the inventory item (should not add slots again since it's already counted)
        var canEquip = character.CanEquip(inventoryItem);

        // Assert - Should be able to equip because item is already in list
        // Total slots used: 3 (sword) + 2 (armor already counted) = 5 slots
        Assert.True(canEquip);
    }

    [Fact]
    public void CanEquip_ShouldCountAllItems_IncludingInventory()
    {
        // Arrange - Character with 5 slots total
        var character = new Character();
        character.Stats.Strength = 0; // This gives 5 equipment slots (5 + 0)

        // Add equipped items taking 2 slots total
        character.Equipment.Add(new Equipment { Id = "sword-1", Name = "Sword", Slots = 2, IsEquipped = true });

        // Add unequipped items (inventory) taking 3 slots
        character.Equipment.Add(new Equipment { Id = "backup-1", Name = "Backup Weapon", Slots = 3, IsEquipped = false });

        // Act - Try to add a new 1-slot item
        var newItem = new Equipment { Id = "new-1", Name = "New Item", Slots = 1 };
        var canEquip = character.CanEquip(newItem);

        // Assert - Should fail because 2 equipped + 3 inventory + 1 new = 6 slots needed, only 5 available
        Assert.False(canEquip);
    }

    [Fact]
    public void TotalEquipmentValue_ShouldSumEquipmentCosts()
    {
        var character = new Character();
        character.Equipment.Add(new Equipment { Name = "Item1", Cost = 10 });
        character.Equipment.Add(new Equipment { Name = "Item2", Cost = 25 });
        character.Equipment.Add(new Equipment { Name = "Item3", Cost = 5 });

        var totalValue = character.TotalEquipmentValue;

        Assert.Equal(40, totalValue);
    }

    [Fact]
    public void TotalEquipmentValue_ShouldReturnZeroForNoEquipment()
    {
        var character = new Character();

        var totalValue = character.TotalEquipmentValue;

        Assert.Equal(0, totalValue);
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyFlawStatModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);
        character.Flaws.Add("Too Many Teeth");

        var flaws = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Too Many Teeth",
                Effect = "Warband member suffers -2 to Presence",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "presence" },
                        { "modifier", -2 }
                    }
                }
            }
        };

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), flaws, new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // unchanged
        Assert.Equal(-1, effectiveStats.Presence); // 1 + (-2) = -1
        Assert.Equal(1, effectiveStats.Strength); // unchanged
        Assert.Equal(1, effectiveStats.Toughness); // unchanged
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyFeatStatModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 0, presence: 0, strength: 0, toughness: 0);
        character.Feats.Add("Swift");

        var feats = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Swift",
                Effect = "+1 to Agility",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "agility" },
                        { "modifier", 1 }
                    }
                }
            }
        };

        // Act
        var effectiveStats = character.GetEffectiveStats(feats, new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // 0 + 1 = 1
        Assert.Equal(0, effectiveStats.Presence); // unchanged
        Assert.Equal(0, effectiveStats.Strength); // unchanged
        Assert.Equal(0, effectiveStats.Toughness); // unchanged
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyMultipleModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);
        character.Feats.Add("Strong");
        character.Flaws.Add("Weak");

        var feats = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Strong",
                Effect = "+1 to Strength",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "strength" },
                        { "modifier", 1 }
                    }
                }
            }
        };

        var flaws = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Weak",
                Effect = "-1 to Toughness",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "toughness" },
                        { "modifier", -1 }
                    }
                }
            }
        };

        // Act
        var effectiveStats = character.GetEffectiveStats(feats, flaws, new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // unchanged
        Assert.Equal(1, effectiveStats.Presence); // unchanged
        Assert.Equal(2, effectiveStats.Strength); // 1 + 1 = 2
        Assert.Equal(0, effectiveStats.Toughness); // 1 + (-1) = 0
    }

    [Fact]
    public void GetEffectiveStats_ShouldReturnBaseStatsWhenNoModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 2, strength: 3, toughness: 0);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility);
        Assert.Equal(2, effectiveStats.Presence);
        Assert.Equal(3, effectiveStats.Strength);
        Assert.Equal(0, effectiveStats.Toughness);
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyInjuryStatModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 2, presence: 1, strength: 1, toughness: 1);
        character.Injuries.Add("Broken Bones");

        var injuries = new List<InjuryOption>
        {
            new InjuryOption
            {
                Name = "Broken Bones",
                Effect = "-1 Agility",
                Type = "permanent",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "agility" },
                        { "modifier", -1 }
                    }
                }
            }
        };

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), injuries);

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // 2 + (-1) = 1
        Assert.Equal(1, effectiveStats.Presence); // unchanged
        Assert.Equal(1, effectiveStats.Strength); // unchanged
        Assert.Equal(1, effectiveStats.Toughness); // unchanged
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyFeatsFlawsAndInjuriesTogether()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);
        character.Feats.Add("Swift");
        character.Flaws.Add("Weak Will");
        character.Injuries.Add("Broken Arm");

        var feats = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Swift",
                Effect = "+1 to Agility",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "agility" },
                        { "modifier", 1 }
                    }
                }
            }
        };

        var flaws = new List<FeatFlawOption>
        {
            new FeatFlawOption
            {
                Name = "Weak Will",
                Effect = "-1 to Presence",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "presence" },
                        { "modifier", -1 }
                    }
                }
            }
        };

        var injuries = new List<InjuryOption>
        {
            new InjuryOption
            {
                Name = "Broken Arm",
                Effect = "-1 Strength",
                Type = "permanent",
                Effects = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "stat_modifier" },
                        { "stat", "strength" },
                        { "modifier", -1 }
                    }
                }
            }
        };

        // Act
        var effectiveStats = character.GetEffectiveStats(feats, flaws, injuries);

        // Assert
        Assert.Equal(2, effectiveStats.Agility); // 1 + 1 (feat) = 2
        Assert.Equal(0, effectiveStats.Presence); // 1 + (-1) (flaw) = 0
        Assert.Equal(0, effectiveStats.Strength); // 1 + (-1) (injury) = 0
        Assert.Equal(1, effectiveStats.Toughness); // unchanged
    }

    [Fact]
    public void CanEquip_ShouldConsiderBackpackSlotBonus()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 0; // Base 5 equipment slots (5 + 0)

        // Add a backpack that takes 1 slot but provides 2 additional slots (net +1 slot)
        var backpack = new Equipment
        {
            Name = "Backpack",
            Type = "item",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "slot_modifier" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(backpack);

        // Now character should have 6 effective slots: 5 base - 1 (backpack) + 2 (backpack bonus) = 6
        // Already used: 1 slot by backpack
        // Available: 5 slots

        // Try to equip 5 more slots worth of equipment
        var heavyItem = new Equipment { Name = "Heavy Item", Slots = 5 };

        // Act
        var canEquip = character.CanEquip(heavyItem);

        // Assert
        Assert.True(canEquip); // Should be able to equip because backpack provides extra slots
    }

    [Fact]
    public void CanEquip_ShouldFailWhenExceedingBackpackBonusSlots()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 0; // Base 5 equipment slots (5 + 0)

        // Add a backpack that takes 1 slot but provides 2 additional slots
        var backpack = new Equipment
        {
            Name = "Backpack",
            Type = "item",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "slot_modifier" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(backpack);

        // Total available: 5 base + 2 bonus = 7 slots
        // Already used: 1 slot by backpack
        // Available: 6 slots
        // Try to equip 7 slots worth of equipment (should exceed the 6 remaining slots)
        var tooHeavyItem = new Equipment { Name = "Too Heavy", Slots = 7 };

        // Act
        var canEquip = character.CanEquip(tooHeavyItem);

        // Assert
        Assert.False(canEquip); // Should fail because we'd exceed available slots (need 7, have 6)
    }

    [Fact]
    public void GetEffectiveEquipmentSlots_ShouldReturnBaseSlots_WhenNoEquipment()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 2; // Base slots = 5 + 2 = 7

        // Act
        var effectiveSlots = character.GetEffectiveEquipmentSlots();

        // Assert
        Assert.Equal(7, effectiveSlots);
    }

    [Fact]
    public void GetEffectiveEquipmentSlots_ShouldIncludeBackpackBonus()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 0; // Base slots = 5 + 0 = 5

        // Add a backpack that provides 2 extra slots
        var backpack = new Equipment
        {
            Name = "Backpack",
            Type = "item",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "slot_modifier" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(backpack);

        // Act
        var effectiveSlots = character.GetEffectiveEquipmentSlots();

        // Assert - should be base (5) + bonus (2) = 7
        Assert.Equal(7, effectiveSlots);
    }

    [Fact]
    public void GetEffectiveEquipmentSlots_ShouldStackMultipleBackpacks()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 1; // Base slots = 5 + 1 = 6

        // Add two backpacks (2 slots bonus each)
        var backpack1 = new Equipment
        {
            Name = "Backpack 1",
            Type = "item",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "slot_modifier" },
                    { "modifier", 2 }
                }
            }
        };
        var backpack2 = new Equipment
        {
            Name = "Backpack 2",
            Type = "item",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "slot_modifier" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(backpack1);
        character.Equipment.Add(backpack2);

        // Act
        var effectiveSlots = character.GetEffectiveEquipmentSlots();

        // Assert - should be base (6) + bonus1 (2) + bonus2 (2) = 10
        Assert.Equal(10, effectiveSlots);
    }

    [Fact]
    public void GetEffectiveStats_ShouldApplyRelicStatModifiers()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);

        // Equip Ring of the Mighty (+2 Strength)
        var ringOfTheMighty = new Equipment
        {
            Name = "Ring of the Mighty",
            Type = "relic",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "strength" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(ringOfTheMighty);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // unchanged
        Assert.Equal(1, effectiveStats.Presence); // unchanged
        Assert.Equal(3, effectiveStats.Strength); // 1 + 2 = 3 from relic
        Assert.Equal(1, effectiveStats.Toughness); // unchanged
    }

    [Fact]
    public void HasEquipmentRestriction_ShouldDetectArmorRestriction()
    {
        // Arrange
        var character = new Character("Test Character");

        // Equip Ring of the Mighty (cannot wear armor)
        var ringOfTheMighty = new Equipment
        {
            Name = "Ring of the Mighty",
            Type = "relic",
            Slots = 1,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "equipment_restriction" },
                    { "excludedTypes", new List<object> { "armor" } }
                }
            }
        };
        character.Equipment.Add(ringOfTheMighty);

        // Try to equip armor
        var lightArmor = new Equipment
        {
            Name = "Light Armor",
            Type = "armor",
            Slots = 1
        };

        // Act
        var hasRestriction = character.HasEquipmentRestriction(lightArmor);

        // Assert
        Assert.True(hasRestriction); // Should be restricted from wearing armor
    }

    [Fact]
    public void HasEquipmentRestriction_ShouldAllowNonRestrictedEquipment()
    {
        // Arrange
        var character = new Character("Test Character");

        // Equip Ring of the Mighty (cannot wear armor)
        var ringOfTheMighty = new Equipment
        {
            Name = "Ring of the Mighty",
            Type = "relic",
            Slots = 1,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "equipment_restriction" },
                    { "excludedTypes", new List<object> { "armor" } }
                }
            }
        };
        character.Equipment.Add(ringOfTheMighty);

        // Try to equip a weapon (not restricted)
        var sword = new Equipment
        {
            Name = "Sword",
            Type = "weapon",
            Slots = 1
        };

        // Act
        var hasRestriction = character.HasEquipmentRestriction(sword);

        // Assert
        Assert.False(hasRestriction); // Should be allowed to equip weapons
    }

    [Fact]
    public void HasFlawRestriction_AllergicToMetal_ShouldRestrictHeavyArmor()
    {
        // Arrange
        var character = new Character("Test Character");

        // Add "Allergic To Metal" flaw
        character.Flaws.Add("Allergic To Metal");

        // Try to equip Heavy Armor
        var heavyArmor = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
            ArmorValue = 3
        };

        // Simulate the flaw's restriction effects
        var allergicToMetalFlaw = new FeatFlawOption
        {
            Name = "Allergic To Metal",
            Effect = "Cannot wear metal Armor",
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "equipment_restriction" },
                    { "excludedEquipment", new List<object> { "Heavy Armor", "Medium Armor", "Full Plate" } }
                }
            }
        };

        // Act
        var hasRestriction = character.HasFlawRestriction(heavyArmor, new List<FeatFlawOption> { allergicToMetalFlaw });

        // Assert
        Assert.True(hasRestriction); // Should be restricted from wearing Heavy Armor
    }

    [Fact]
    public void HasFlawRestriction_AllergicToMetal_ShouldAllowLightArmor()
    {
        // Arrange
        var character = new Character("Test Character");

        // Add "Allergic To Metal" flaw
        character.Flaws.Add("Allergic To Metal");

        // Try to equip Light Armor
        var lightArmor = new Equipment
        {
            Name = "Light Armor",
            Type = "armor",
            ArmorValue = 1
        };

        // Simulate the flaw's restriction effects
        var allergicToMetalFlaw = new FeatFlawOption
        {
            Name = "Allergic To Metal",
            Effect = "Cannot wear metal Armor",
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "equipment_restriction" },
                    { "excludedEquipment", new List<object> { "Heavy Armor", "Medium Armor", "Full Plate" } }
                }
            }
        };

        // Act
        var hasRestriction = character.HasFlawRestriction(lightArmor, new List<FeatFlawOption> { allergicToMetalFlaw });

        // Assert
        Assert.False(hasRestriction); // Should be allowed to wear Light Armor
    }

    [Fact]
    public void InventoryItems_ShouldCountTowardSlotLimits()
    {
        // Arrange
        var character = new Character();
        character.Stats.Strength = 0; // Base 5 equipment slots

        // Add equipped item (2 slots)
        var equippedItem = new Equipment
        {
            Name = "Equipped Sword",
            Type = "weapon",
            Slots = 2,
            IsEquipped = true
        };
        character.Equipment.Add(equippedItem);

        // Add inventory item (2 slots)
        var inventoryItem = new Equipment
        {
            Name = "Inventory Shield",
            Type = "armor",
            Slots = 2,
            IsEquipped = false
        };
        character.Equipment.Add(inventoryItem);

        // Try to equip another 2-slot item (should fail as we have 4/5 slots used)
        var newItem = new Equipment { Name = "New Item", Slots = 2 };

        // Act
        var canEquip = character.CanEquip(newItem);

        // Assert
        Assert.False(canEquip); // Should fail because inventory items count toward slot limits
    }

    [Fact]
    public void InventoryItems_ShouldNotProvideArmorValue()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);

        // Add equipped armor (should provide armor)
        var equippedArmor = new Equipment
        {
            Name = "Equipped Armor",
            Type = "armor",
            ArmorValue = 2,
            IsEquipped = true
        };
        character.Equipment.Add(equippedArmor);

        // Add inventory armor (should NOT provide armor)
        var inventoryArmor = new Equipment
        {
            Name = "Inventory Armor",
            Type = "armor",
            ArmorValue = 3,
            IsEquipped = false
        };
        character.Equipment.Add(inventoryArmor);

        // Act
        var totalArmor = character.GetTotalArmorValue();

        // Assert
        Assert.Equal(2, totalArmor); // Only equipped armor should count
    }

    [Fact]
    public void InventoryItems_ShouldNotProvideStatBonuses()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);

        // Add equipped relic with stat bonus
        var equippedRelic = new Equipment
        {
            Name = "Equipped Ring",
            Type = "relic",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "strength" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(equippedRelic);

        // Add inventory relic with stat bonus (should NOT apply)
        var inventoryRelic = new Equipment
        {
            Name = "Inventory Ring",
            Type = "relic",
            Slots = 1,
            IsEquipped = false,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "strength" },
                    { "modifier", 3 }
                }
            }
        };
        character.Equipment.Add(inventoryRelic);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(3, effectiveStats.Strength); // 1 base + 2 from equipped relic only
    }

    [Fact]
    public void EquipItem_ShouldApplyBenefits()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);

        // Add relic to inventory (not equipped)
        var relic = new Equipment
        {
            Id = "relic-1",
            Name = "Power Ring",
            Type = "relic",
            Slots = 1,
            IsEquipped = false,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "strength" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(relic);

        // Act - equip the item
        character.EquipItem("relic-1");
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.True(relic.IsEquipped);
        Assert.Equal(3, effectiveStats.Strength); // 1 base + 2 from equipped relic
    }

    [Fact]
    public void UnequipItem_ShouldRemoveBenefits()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 1, presence: 1, strength: 1, toughness: 1);

        // Add equipped relic
        var relic = new Equipment
        {
            Id = "relic-1",
            Name = "Power Ring",
            Type = "relic",
            Slots = 1,
            IsEquipped = true,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "strength" },
                    { "modifier", 2 }
                }
            }
        };
        character.Equipment.Add(relic);

        // Act - unequip the item
        character.UnequipItem("relic-1");
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.False(relic.IsEquipped);
        Assert.Equal(1, effectiveStats.Strength); // Back to base strength only
    }

    [Fact]
    public void Equipment_ShouldApplyPenalty_WhenRequirementNotMet()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 2, presence: 1, strength: 0, toughness: 1);

        // Add equipment with stat requirement and penalty
        var heavyArmor = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
            Slots = 1,
            ArmorValue = 3,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 },
                    { "penalty", new Dictionary<string, object>
                        {
                            { "stat", "agility" },
                            { "value", -1 }
                        }
                    }
                }
            }
        };
        character.Equipment.Add(heavyArmor);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(1, effectiveStats.Agility); // 2 base - 1 penalty = 1
        Assert.Equal(0, effectiveStats.Strength); // No change to strength
    }

    [Fact]
    public void Equipment_ShouldNotApplyPenalty_WhenRequirementMet()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 2, presence: 1, strength: 2, toughness: 1);

        // Add equipment with stat requirement and penalty
        var heavyArmor = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
            Slots = 1,
            ArmorValue = 3,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 },
                    { "penalty", new Dictionary<string, object>
                        {
                            { "stat", "agility" },
                            { "value", -1 }
                        }
                    }
                }
            }
        };
        character.Equipment.Add(heavyArmor);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(2, effectiveStats.Agility); // No penalty applied
        Assert.Equal(2, effectiveStats.Strength); // No change
    }

    [Fact]
    public void Equipment_ShouldApplyMultiplePenalties_FromDifferentItems()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 3, presence: 2, strength: 0, toughness: 0);

        // Add two items with unmet requirements
        var heavyArmor = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
            Slots = 1,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 },
                    { "penalty", new Dictionary<string, object>
                        {
                            { "stat", "agility" },
                            { "value", -1 }
                        }
                    }
                }
            }
        };

        var heavyWeapon = new Equipment
        {
            Name = "Heavy Sword",
            Type = "weapon",
            Slots = 1,
            IsEquipped = true,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "toughness" },
                    { "requiredValue", 1 },
                    { "penalty", new Dictionary<string, object>
                        {
                            { "stat", "presence" },
                            { "value", -1 }
                        }
                    }
                }
            }
        };

        character.Equipment.Add(heavyArmor);
        character.Equipment.Add(heavyWeapon);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(2, effectiveStats.Agility); // 3 - 1 = 2
        Assert.Equal(1, effectiveStats.Presence); // 2 - 1 = 1
    }

    [Fact]
    public void Equipment_ShouldOnlyApplyPenalty_WhenEquipped()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Stats = new Stats(agility: 2, presence: 1, strength: 0, toughness: 1);

        // Add equipment with requirement but NOT equipped
        var heavyArmor = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
            Slots = 1,
            IsEquipped = false, // Not equipped, just in inventory
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 },
                    { "penalty", new Dictionary<string, object>
                        {
                            { "stat", "agility" },
                            { "value", -1 }
                        }
                    }
                }
            }
        };
        character.Equipment.Add(heavyArmor);

        // Act
        var effectiveStats = character.GetEffectiveStats(new List<FeatFlawOption>(), new List<FeatFlawOption>(), new List<InjuryOption>());

        // Assert
        Assert.Equal(2, effectiveStats.Agility); // No penalty since item not equipped
    }
}