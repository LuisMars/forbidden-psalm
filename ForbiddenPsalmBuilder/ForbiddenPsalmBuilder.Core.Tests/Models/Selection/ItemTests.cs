using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class ItemTests
{
    [Fact]
    public void Item_ShouldImplementISelectableItem()
    {
        // Arrange & Act
        var item = new Item();

        // Assert
        Assert.IsAssignableFrom<ISelectableItem>(item);
    }

    [Fact]
    public void Item_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var item = new Item
        {
            Id = "cheese",
            Name = "Cheese",
            DisplayName = "Cheese",
            Description = "Restorative food",
            Effect = "Can be eaten as action to heal 1 HP",
            Cost = 3,
            Slots = 1,
            Category = "item",
            IconClass = "fas fa-cheese"
        };

        // Assert
        Assert.Equal("cheese", item.Id);
        Assert.Equal("Cheese", item.Name);
        Assert.Equal("Cheese", item.DisplayName);
        Assert.Equal("Restorative food", item.Description);
        Assert.Equal("Can be eaten as action to heal 1 HP", item.Effect);
        Assert.Equal(3, item.Cost);
        Assert.Equal(1, item.Slots);
        Assert.Equal("item", item.Category);
        Assert.Equal("fas fa-cheese", item.IconClass);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeEffect()
    {
        // Arrange
        var item = new Item
        {
            Effect = "Heals 1D6 HP",
            Cost = 6,
            Slots = 1
        };

        // Act
        var info = item.GetDetailedInfo();

        // Assert
        Assert.Contains("**Effect:** Heals 1D6 HP", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeCostAndSlots()
    {
        // Arrange
        var item = new Item
        {
            Effect = "Cures Bleeding",
            Cost = 1,
            Slots = 1
        };

        // Act
        var info = item.GetDetailedInfo();

        // Assert
        Assert.Contains("**Cost:** 1", info);
        Assert.Contains("**Slots:** 1", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldHandleNullableCost()
    {
        // Arrange
        var item = new Item
        {
            Effect = "Does something",
            Cost = null,
            Slots = 1
        };

        // Act
        var info = item.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Cost:**", info);
    }

    [Fact]
    public void Item_ShouldSupportAmmoType()
    {
        // Arrange & Act
        var ammo = new Item
        {
            Name = "Arrows",
            Type = "ammo",
            Shots = 5,
            Cost = 2,
            Slots = 1
        };

        // Assert
        Assert.Equal("ammo", ammo.Type);
        Assert.Equal(5, ammo.Shots);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeShotsForAmmo()
    {
        // Arrange
        var ammo = new Item
        {
            Name = "Bullets",
            Type = "ammo",
            Shots = 6,
            Cost = 4,
            Slots = 1
        };

        // Act
        var info = ammo.GetDetailedInfo();

        // Assert
        Assert.Contains("**Shots:** 6", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldNotIncludeShotsForRegularItems()
    {
        // Arrange
        var item = new Item
        {
            Name = "Cheese",
            Type = "item",
            Effect = "Heals 1 HP",
            Cost = 3,
            Slots = 1
        };

        // Act
        var info = item.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Shots:**", info);
    }

    [Fact]
    public void Item_ShouldSupportTechLevel()
    {
        // Arrange & Act
        var futureItem = new Item
        {
            TechLevel = "future"
        };

        // Assert
        Assert.Equal("future", futureItem.TechLevel);
    }
}