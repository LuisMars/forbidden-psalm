using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class WeaponTests
{
    [Fact]
    public void Weapon_ShouldImplementISelectableItem()
    {
        // Arrange & Act
        var weapon = new Weapon();

        // Assert
        Assert.IsAssignableFrom<ISelectableItem>(weapon);
    }

    [Fact]
    public void Weapon_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var weapon = new Weapon
        {
            Id = "dagger",
            Name = "Dagger",
            DisplayName = "Dagger",
            Description = "A sharp dagger",
            Damage = "D4",
            Properties = new List<string> { "Thrown" },
            Stat = "Agility",
            Cost = 1,
            Slots = 1,
            Category = "oneHandedMelee",
            IconClass = "ra ra-daggers"
        };

        // Assert
        Assert.Equal("dagger", weapon.Id);
        Assert.Equal("Dagger", weapon.Name);
        Assert.Equal("Dagger", weapon.DisplayName);
        Assert.Equal("A sharp dagger", weapon.Description);
        Assert.Equal("D4", weapon.Damage);
        Assert.Single(weapon.Properties);
        Assert.Equal("Thrown", weapon.Properties[0]);
        Assert.Equal("Agility", weapon.Stat);
        Assert.Equal(1, weapon.Cost);
        Assert.Equal(1, weapon.Slots);
        Assert.Equal("oneHandedMelee", weapon.Category);
        Assert.Equal("ra ra-daggers", weapon.IconClass);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeDamageAndStat()
    {
        // Arrange
        var weapon = new Weapon
        {
            Damage = "D6",
            Stat = "Strength",
            Cost = 3,
            Slots = 1
        };

        // Act
        var info = weapon.GetDetailedInfo();

        // Assert
        Assert.Contains("**Damage:** D6", info);
        Assert.Contains("**Stat:** Strength", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeProperties()
    {
        // Arrange
        var weapon = new Weapon
        {
            Damage = "D6",
            Stat = "Strength",
            Properties = new List<string> { "Thrown", "Reach" },
            Cost = 8,
            Slots = 2
        };

        // Act
        var info = weapon.GetDetailedInfo();

        // Assert
        Assert.Contains("**Properties:**", info);
        Assert.Contains("- Thrown", info);
        Assert.Contains("- Reach", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeCostAndSlots()
    {
        // Arrange
        var weapon = new Weapon
        {
            Damage = "D10",
            Stat = "Strength",
            Cost = 10,
            Slots = 2
        };

        // Act
        var info = weapon.GetDetailedInfo();

        // Assert
        Assert.Contains("**Cost:** 10", info);
        Assert.Contains("**Slots:** 2", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldHandleNoProperties()
    {
        // Arrange
        var weapon = new Weapon
        {
            Damage = "D4",
            Stat = "Strength",
            Properties = new List<string>(),
            Cost = 0,
            Slots = 1
        };

        // Act
        var info = weapon.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Properties:**", info);
        Assert.Contains("**Damage:**", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldHandleNullableCost()
    {
        // Arrange
        var weapon = new Weapon
        {
            Damage = "D4",
            Stat = "Strength",
            Cost = null,
            Slots = 1
        };

        // Act
        var info = weapon.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Cost:**", info);
    }

    [Fact]
    public void Weapon_ShouldHaveWeaponCategory()
    {
        // Arrange & Act
        var weapon = new Weapon
        {
            Category = "twoHandedRanged"
        };

        // Assert
        Assert.Equal("twoHandedRanged", weapon.Category);
    }

    [Fact]
    public void Weapon_ShouldSupportTechLevel()
    {
        // Arrange & Act
        var futureWeapon = new Weapon
        {
            TechLevel = "future"
        };
        var pastWeapon = new Weapon
        {
            TechLevel = "past"
        };

        // Assert
        Assert.Equal("future", futureWeapon.TechLevel);
        Assert.Equal("past", pastWeapon.TechLevel);
    }
}