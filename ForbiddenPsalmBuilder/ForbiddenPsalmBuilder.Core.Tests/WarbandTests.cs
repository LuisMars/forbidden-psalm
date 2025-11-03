using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;

namespace ForbiddenPsalmBuilder.Core.Tests;

public class WarbandTests
{
    [Fact]
    public void Warband_ShouldInitializeWithDefaults()
    {
        var warband = new Warband();

        Assert.NotNull(warband.Id);
        Assert.NotEmpty(warband.Id);
        Assert.Equal(string.Empty, warband.Name);
        Assert.Equal(string.Empty, warband.GameVariant);
        Assert.NotNull(warband.Members);
        Assert.Empty(warband.Members);
        Assert.Equal(50, warband.Gold);
        Assert.Equal(0, warband.Experience);
        Assert.NotNull(warband.UpgradesPurchased);
        Assert.Empty(warband.UpgradesPurchased);
    }

    [Fact]
    public void Warband_ShouldInitializeWithNameAndGameVariant()
    {
        var name = "Test Warband";
        var gameVariant = "end-times";
        var warband = new Warband(name, gameVariant);

        Assert.Equal(name, warband.Name);
        Assert.Equal(gameVariant, warband.GameVariant);
        Assert.NotNull(warband.Id);
        Assert.NotEmpty(warband.Id);
    }

    [Fact]
    public void IsValid_ShouldReturnTrueForValidWarband()
    {
        var warband = new Warband();
        warband.Members.Add(new Character("Character 1"));
        warband.Members.Add(new Character("Character 2"));

        Assert.True(warband.IsValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalseForEmptyWarband()
    {
        var warband = new Warband();

        Assert.False(warband.IsValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalseForOversizedWarband()
    {
        var warband = new Warband();
        for (int i = 0; i < 6; i++)
        {
            warband.Members.Add(new Character($"Character {i + 1}"));
        }

        Assert.False(warband.IsValid);
    }

    [Fact]
    public void CanAddMember_ShouldReturnTrueWhenUnderLimit()
    {
        var warband = new Warband();
        warband.Members.Add(new Character("Character 1"));

        Assert.True(warband.CanAddMember);
    }

    [Fact]
    public void CanAddMember_ShouldReturnFalseWhenAtLimit()
    {
        var warband = new Warband();
        for (int i = 0; i < 5; i++)
        {
            warband.Members.Add(new Character($"Character {i + 1}"));
        }

        Assert.False(warband.CanAddMember);
    }

    [Fact]
    public void TotalValue_ShouldIncludeGoldAndEquipmentValue()
    {
        var warband = new Warband();
        warband.Gold = 100;

        var character = new Character("Test");
        character.Equipment.Add(new Equipment { Name = "Weapon", Cost = 25 });
        warband.Members.Add(character);

        Assert.Equal(125, warband.TotalValue);
    }

    [Fact]
    public void UpdateLastModified_ShouldUpdateTimestamp()
    {
        var warband = new Warband();
        var originalTime = warband.LastModified;

        Thread.Sleep(10); // Ensure time difference
        warband.UpdateLastModified();

        Assert.True(warband.LastModified > originalTime);
    }
}