using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models;

public class WarbandStashTests
{
    [Fact]
    public void Warband_Should_Have_Empty_Stash_By_Default()
    {
        // Arrange & Act
        var warband = new Warband("Test Warband", "28-psalms");

        // Assert
        Assert.NotNull(warband.Stash);
        Assert.Empty(warband.Stash);
    }

    [Fact]
    public void Warband_Should_Be_Able_To_Add_Equipment_To_Stash()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms");
        var equipment = new Equipment
        {
            Name = "Sword",
            Type = "weapon",
            Cost = 3,
            Slots = 1
        };

        // Act
        warband.Stash.Add(equipment);

        // Assert
        Assert.Single(warband.Stash);
        Assert.Equal("Sword", warband.Stash[0].Name);
    }

    [Fact]
    public void Warband_Should_Be_Able_To_Remove_Equipment_From_Stash()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms");
        var equipment = new Equipment
        {
            Name = "Sword",
            Type = "weapon",
            Cost = 3,
            Slots = 1
        };
        warband.Stash.Add(equipment);

        // Act
        warband.Stash.Remove(equipment);

        // Assert
        Assert.Empty(warband.Stash);
    }

    [Fact]
    public void Warband_Total_Value_Should_Include_Stash_Equipment()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms")
        {
            Gold = 50
        };

        var sword = new Equipment
        {
            Name = "Sword",
            Type = "weapon",
            Cost = 3,
            Slots = 1
        };

        var armor = new Equipment
        {
            Name = "Light Armor",
            Type = "armor",
            Cost = 2,
            Slots = 1,
            ArmorValue = 1
        };

        warband.Stash.Add(sword);
        warband.Stash.Add(armor);

        // Act
        var totalValue = warband.TotalValue;

        // Assert
        Assert.Equal(55, totalValue); // 50 gold + 3 (sword) + 2 (armor)
    }

    [Fact]
    public void Warband_Stash_Value_Should_Calculate_Correctly()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms");

        warband.Stash.Add(new Equipment { Name = "Sword", Cost = 3 });
        warband.Stash.Add(new Equipment { Name = "Dagger", Cost = 1 });
        warband.Stash.Add(new Equipment { Name = "Light Armor", Cost = 2 });

        // Act
        var stashValue = warband.StashValue;

        // Assert
        Assert.Equal(6, stashValue);
    }

    [Fact]
    public void Warband_Can_Afford_Should_Return_True_When_Gold_Sufficient()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms")
        {
            Gold = 50
        };

        // Act & Assert
        Assert.True(warband.CanAfford(30));
        Assert.True(warband.CanAfford(50));
    }

    [Fact]
    public void Warband_Can_Afford_Should_Return_False_When_Gold_Insufficient()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms")
        {
            Gold = 50
        };

        // Act & Assert
        Assert.False(warband.CanAfford(51));
        Assert.False(warband.CanAfford(100));
    }
}