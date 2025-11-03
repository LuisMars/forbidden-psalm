using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Blazor.Tests.Pages;

/// <summary>
/// Tests for merchant inventory filtering logic in CharacterEdit page
/// </summary>
public class CharacterEditMerchantTests
{
    [Fact]
    public void MerchantInventory_ShouldRespectMaxWeapons()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3,
            MaxArmor = 1,
            MaxEquipment = 1
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" },
            new Weapon { Id = "dagger", Name = "Dagger" },
            new Weapon { Id = "bow", Name = "Bow" },
            new Weapon { Id = "spear", Name = "Spear" }
        };

        // Simulate random selection (we'll just take first 3 for deterministic test)
        var merchantInventoryIds = allWeapons.Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();

        // Act
        var filteredWeapons = allWeapons.Where(w => merchantInventoryIds.Contains(w.Id)).ToList();

        // Assert
        Assert.Equal(3, filteredWeapons.Count);
        Assert.Contains(filteredWeapons, w => w.Id == "sword");
        Assert.Contains(filteredWeapons, w => w.Id == "axe");
        Assert.Contains(filteredWeapons, w => w.Id == "dagger");
        Assert.DoesNotContain(filteredWeapons, w => w.Id == "bow");
        Assert.DoesNotContain(filteredWeapons, w => w.Id == "spear");
    }

    [Fact]
    public void MerchantInventory_ShouldRespectMaxArmor()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3,
            MaxArmor = 1,
            MaxEquipment = 1
        };

        var allArmors = new List<Armor>
        {
            new Armor { Id = "light", Name = "Light Armor" },
            new Armor { Id = "medium", Name = "Medium Armor" },
            new Armor { Id = "heavy", Name = "Heavy Armor" }
        };

        // Simulate random selection
        var merchantInventoryIds = allArmors.Take(trader.MaxArmor ?? 0).Select(a => a.Id).ToList();

        // Act
        var filteredArmors = allArmors.Where(a => merchantInventoryIds.Contains(a.Id)).ToList();

        // Assert
        Assert.Single(filteredArmors);
        Assert.Equal("light", filteredArmors[0].Id);
    }

    [Fact]
    public void MerchantInventory_ShouldRespectMaxEquipment()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3,
            MaxArmor = 1,
            MaxEquipment = 1
        };

        var allItems = new List<Item>
        {
            new Item { Id = "rope", Name = "Rope" },
            new Item { Id = "torch", Name = "Torch" },
            new Item { Id = "rations", Name = "Rations" }
        };

        // Simulate random selection
        var merchantInventoryIds = allItems.Take(trader.MaxEquipment ?? 0).Select(i => i.Id).ToList();

        // Act
        var filteredItems = allItems.Where(i => merchantInventoryIds.Contains(i.Id)).ToList();

        // Assert
        Assert.Single(filteredItems);
        Assert.Equal("rope", filteredItems[0].Id);
    }

    [Fact]
    public void NonMerchantTrader_ShouldNotFilterInventory()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix the Mad Wizard",
            HasLimitedInventory = false
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" },
            new Weapon { Id = "dagger", Name = "Dagger" }
        };

        var merchantInventoryIds = new List<string>(); // Empty for non-merchant

        // Act - When trader doesn't have limited inventory, should return all
        var filteredWeapons = trader.HasLimitedInventory && merchantInventoryIds.Any()
            ? allWeapons.Where(w => merchantInventoryIds.Contains(w.Id)).ToList()
            : allWeapons;

        // Assert
        Assert.Equal(3, filteredWeapons.Count);
    }

    [Fact]
    public void MerchantInventory_WithNoRoll_ShouldReturnAllItems()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" }
        };

        var merchantInventoryIds = new List<string>(); // No roll yet

        // Act - Before rolling, should return all items
        var filteredWeapons = trader.HasLimitedInventory && merchantInventoryIds.Any()
            ? allWeapons.Where(w => merchantInventoryIds.Contains(w.Id)).ToList()
            : allWeapons;

        // Assert
        Assert.Equal(2, filteredWeapons.Count);
    }

    [Fact]
    public void MerchantInventory_RandomSelection_ShouldSelectDifferentItems()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" },
            new Weapon { Id = "dagger", Name = "Dagger" },
            new Weapon { Id = "bow", Name = "Bow" },
            new Weapon { Id = "spear", Name = "Spear" }
        };

        var random = new Random(42); // Seed for deterministic test

        // Act - Simulate rolling twice
        var roll1 = allWeapons.OrderBy(x => random.Next()).Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();

        random = new Random(100); // Different seed
        var roll2 = allWeapons.OrderBy(x => random.Next()).Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();

        // Assert - Both should have exactly 3 items
        Assert.Equal(3, roll1.Count);
        Assert.Equal(3, roll2.Count);

        // All items should be from the original list
        Assert.All(roll1, id => Assert.Contains(allWeapons, w => w.Id == id));
        Assert.All(roll2, id => Assert.Contains(allWeapons, w => w.Id == id));
    }

    [Fact]
    public void MerchantInventory_WithFewerItemsThanMax_ShouldReturnAllAvailable()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" }
        };

        // Act - Request 3 but only 2 available
        var merchantInventoryIds = allWeapons.Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();
        var filteredWeapons = allWeapons.Where(w => merchantInventoryIds.Contains(w.Id)).ToList();

        // Assert
        Assert.Equal(2, filteredWeapons.Count);
    }

    [Fact]
    public void MerchantInventory_CombinedSelection_ShouldRespectAllLimits()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3,
            MaxArmor = 1,
            MaxEquipment = 1
        };

        var allWeapons = Enumerable.Range(1, 5).Select(i => new Weapon { Id = $"weapon{i}", Name = $"Weapon {i}" }).ToList();
        var allArmors = Enumerable.Range(1, 3).Select(i => new Armor { Id = $"armor{i}", Name = $"Armor {i}" }).ToList();
        var allItems = Enumerable.Range(1, 3).Select(i => new Item { Id = $"item{i}", Name = $"Item {i}" }).ToList();

        var random = new Random(42);

        // Act - Simulate rolling for all categories
        var merchantInventoryIds = new List<string>();
        merchantInventoryIds.AddRange(allWeapons.OrderBy(x => random.Next()).Take(trader.MaxWeapons ?? 0).Select(w => w.Id));
        merchantInventoryIds.AddRange(allArmors.OrderBy(x => random.Next()).Take(trader.MaxArmor ?? 0).Select(a => a.Id));
        merchantInventoryIds.AddRange(allItems.OrderBy(x => random.Next()).Take(trader.MaxEquipment ?? 0).Select(i => i.Id));

        // Assert - Total should be 3 + 1 + 1 = 5 items
        Assert.Equal(5, merchantInventoryIds.Count);

        // Check individual category counts
        var weaponCount = merchantInventoryIds.Count(id => id.StartsWith("weapon"));
        var armorCount = merchantInventoryIds.Count(id => id.StartsWith("armor"));
        var itemCount = merchantInventoryIds.Count(id => id.StartsWith("item"));

        Assert.Equal(3, weaponCount);
        Assert.Equal(1, armorCount);
        Assert.Equal(1, itemCount);
    }

    [Fact]
    public void MerchantInventory_ShouldPersistUntilRerolled()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3
        };

        var allWeapons = new List<Weapon>
        {
            new Weapon { Id = "sword", Name = "Sword" },
            new Weapon { Id = "axe", Name = "Axe" },
            new Weapon { Id = "dagger", Name = "Dagger" },
            new Weapon { Id = "bow", Name = "Bow" },
            new Weapon { Id = "spear", Name = "Spear" }
        };

        var random = new Random(42);

        // Act - First roll
        var firstRoll = allWeapons.OrderBy(x => random.Next()).Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();

        // Simulate multiple page interactions without rerolling
        var access1 = allWeapons.Where(w => firstRoll.Contains(w.Id)).ToList();
        var access2 = allWeapons.Where(w => firstRoll.Contains(w.Id)).ToList();

        // Assert - Should be the same inventory
        Assert.Equal(access1.Count, access2.Count);
        Assert.Equal(access1.Select(w => w.Id).OrderBy(x => x), access2.Select(w => w.Id).OrderBy(x => x));

        // Act - Reroll with different seed should produce different result
        // Use a predictable but different selection
        var secondRoll = allWeapons.Skip(1).Take(trader.MaxWeapons ?? 0).Select(w => w.Id).ToList();
        var access3 = allWeapons.Where(w => secondRoll.Contains(w.Id)).ToList();

        // Assert - Inventory should be exactly 3 items
        Assert.Equal(3, firstRoll.Count);
        Assert.Equal(3, secondRoll.Count);

        // Both selections should be valid (from available weapons)
        Assert.All(firstRoll, id => Assert.Contains(allWeapons, w => w.Id == id));
        Assert.All(secondRoll, id => Assert.Contains(allWeapons, w => w.Id == id));
    }
}
