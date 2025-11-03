using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class ArmorTests
{
    [Fact]
    public void Armor_ShouldImplementISelectableItem()
    {
        // Arrange & Act
        var armor = new Armor();

        // Assert
        Assert.IsAssignableFrom<ISelectableItem>(armor);
    }

    [Fact]
    public void Armor_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var armor = new Armor
        {
            Id = "light-armor",
            Name = "Light",
            DisplayName = "Light Armor",
            Description = "Light protective armor",
            ArmorValue = 1,
            Cost = 2,
            Slots = 1,
            Category = "armor",
            IconClass = "ra ra-vest"
        };

        // Assert
        Assert.Equal("light-armor", armor.Id);
        Assert.Equal("Light", armor.Name);
        Assert.Equal("Light Armor", armor.DisplayName);
        Assert.Equal("Light protective armor", armor.Description);
        Assert.Equal(1, armor.ArmorValue);
        Assert.Equal(2, armor.Cost);
        Assert.Equal(1, armor.Slots);
        Assert.Equal("armor", armor.Category);
        Assert.Equal("ra ra-vest", armor.IconClass);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeArmorValue()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 2,
            Cost = 10,
            Slots = 1
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.Contains("**Armor Value:** 2", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeCostAndSlots()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 3,
            Cost = 20,
            Slots = 1
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.Contains("**Cost:** 20", info);
        Assert.Contains("**Slots:** 1", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeSpecialText()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 4,
            Special = "Must have 2+ Strength to use",
            Cost = 50,
            Slots = 1
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.Contains("**Special:** Must have 2+ Strength to use", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldHandleNoSpecial()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 1,
            Special = null,
            Cost = 2,
            Slots = 1
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Special:**", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeEffects()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 1,
            Special = "-1 movement",
            Cost = 0,
            Slots = 1,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "movement_penalty" },
                    { "modifier", -1 }
                }
            }
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.Contains("**Special:** -1 movement", info);
    }

    [Fact]
    public void GetDetailedInfo_ShouldHandleNullableCost()
    {
        // Arrange
        var armor = new Armor
        {
            ArmorValue = 1,
            Cost = null,
            Slots = 1
        };

        // Act
        var info = armor.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("**Cost:**", info);
    }

    [Fact]
    public void Armor_ShouldSupportRestrictions()
    {
        // Arrange & Act
        var armor = new Armor
        {
            ArmorValue = 4,
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 }
                }
            }
        };

        // Assert
        Assert.Single(armor.Restrictions);
        Assert.Equal("stat_requirement", armor.Restrictions[0]["type"]);
    }
}