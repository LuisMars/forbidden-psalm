using ForbiddenPsalmBuilder.Core.Models.Character;

namespace ForbiddenPsalmBuilder.Core.Tests;

public class EquipmentTests
{
    [Fact]
    public void Equipment_ShouldInitializeWithDefaults()
    {
        var equipment = new Equipment();

        Assert.NotNull(equipment.Id);
        Assert.NotEmpty(equipment.Id);
        Assert.Equal(string.Empty, equipment.Name);
        Assert.Equal(string.Empty, equipment.Type);
        Assert.Null(equipment.Damage);
        Assert.NotNull(equipment.Properties);
        Assert.Empty(equipment.Properties);
        Assert.Null(equipment.Stat);
        Assert.Equal(0, equipment.Cost);
        Assert.Equal(1, equipment.Slots);
        Assert.Equal(0, equipment.ArmorValue);
        Assert.Null(equipment.Special);
        Assert.Null(equipment.Effect);
        Assert.Null(equipment.RollRange);
    }

    [Fact]
    public void IsWeapon_ShouldReturnTrueForWeaponType()
    {
        var equipment = new Equipment { Type = "weapon" };

        Assert.True(equipment.IsWeapon);
    }

    [Fact]
    public void IsWeapon_ShouldReturnTrueForWeaponTypeCaseInsensitive()
    {
        var equipment = new Equipment { Type = "WEAPON" };

        Assert.True(equipment.IsWeapon);
    }

    [Fact]
    public void IsWeapon_ShouldReturnFalseForNonWeaponType()
    {
        var equipment = new Equipment { Type = "armor" };

        Assert.False(equipment.IsWeapon);
    }

    [Fact]
    public void IsArmor_ShouldReturnTrueForArmorType()
    {
        var equipment = new Equipment { Type = "armor" };

        Assert.True(equipment.IsArmor);
    }

    [Fact]
    public void IsArmor_ShouldReturnTrueForArmorTypeCaseInsensitive()
    {
        var equipment = new Equipment { Type = "ARMOR" };

        Assert.True(equipment.IsArmor);
    }

    [Fact]
    public void IsArmor_ShouldReturnFalseForNonArmorType()
    {
        var equipment = new Equipment { Type = "item" };

        Assert.False(equipment.IsArmor);
    }

    [Fact]
    public void IsItem_ShouldReturnTrueForItemType()
    {
        var equipment = new Equipment { Type = "item" };

        Assert.True(equipment.IsItem);
    }

    [Fact]
    public void IsItem_ShouldReturnTrueForItemTypeCaseInsensitive()
    {
        var equipment = new Equipment { Type = "ITEM" };

        Assert.True(equipment.IsItem);
    }

    [Fact]
    public void IsItem_ShouldReturnFalseForNonItemType()
    {
        var equipment = new Equipment { Type = "weapon" };

        Assert.False(equipment.IsItem);
    }

    [Theory]
    [InlineData("weapon", true, false, false)]
    [InlineData("armor", false, true, false)]
    [InlineData("item", false, false, true)]
    [InlineData("", false, false, false)]
    public void TypeProperties_ShouldWorkCorrectly(string type, bool isWeapon, bool isArmor, bool isItem)
    {
        var equipment = new Equipment { Type = type };

        Assert.Equal(isWeapon, equipment.IsWeapon);
        Assert.Equal(isArmor, equipment.IsArmor);
        Assert.Equal(isItem, equipment.IsItem);
    }
}