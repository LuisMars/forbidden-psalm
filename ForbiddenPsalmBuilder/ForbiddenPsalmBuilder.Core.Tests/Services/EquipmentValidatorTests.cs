using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class EquipmentValidatorTests
{
    private readonly EquipmentValidator _validator;

    public EquipmentValidatorTests()
    {
        _validator = new EquipmentValidator();
    }

    [Fact]
    public void CanEquipArmor_ShouldReturnTrue_WhenNoArmorEquipped()
    {
        // Arrange
        var character = new Character("Test");
        var armor = new Equipment { Name = "Light", Type = "armor", ArmorValue = 1, Slots = 1 };

        // Act
        var result = _validator.CanEquipArmor(character, armor);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEquipArmor_ShouldReturnFalse_WhenBodyArmorAlreadyEquipped()
    {
        // Arrange
        var character = new Character("Test");
        character.Equipment.Add(new Equipment { Name = "Light", Type = "armor", ArmorType = "body", ArmorValue = 1, Slots = 1, IsEquipped = true });
        var newArmor = new Equipment { Name = "Heavy", Type = "armor", ArmorType = "body", ArmorValue = 3, Slots = 1 };

        // Act
        var result = _validator.CanEquipArmor(character, newArmor);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanEquipArmor_ShouldReturnTrue_WhenEquippingAccessoryWithBodyArmor()
    {
        // Arrange
        var character = new Character("Test");
        character.Equipment.Add(new Equipment { Name = "Light", Type = "armor", ArmorType = "body", ArmorValue = 1, Slots = 1, IsEquipped = true });
        var shield = new Equipment { Name = "Shield", Type = "armor", ArmorType = "accessory", ArmorValue = 1, Slots = 1 };

        // Act
        var result = _validator.CanEquipArmor(character, shield);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsStatRequirements_ShouldReturnTrue_WhenNoRequirements()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 0;
        var equipment = new Equipment { Name = "Sword", Type = "weapon", Slots = 1 };

        // Act
        var result = _validator.MeetsStatRequirements(character, equipment);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsStatRequirements_ShouldReturnTrue_WhenStrengthSufficient()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 2;
        var equipment = new Equipment
        {
            Name = "Power/Plate",
            Type = "armor",
            ArmorValue = 4,
            Slots = 1,
            Special = "Must have 2+ Strength to use"
        };

        // Act
        var result = _validator.MeetsStatRequirements(character, equipment, requiredStrength: 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsStatRequirements_ShouldReturnFalse_WhenStrengthInsufficient()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 1;
        var equipment = new Equipment
        {
            Name = "Power/Plate",
            Type = "armor",
            ArmorValue = 4,
            Slots = 1,
            Special = "Must have 2+ Strength to use"
        };

        // Act
        var result = _validator.MeetsStatRequirements(character, equipment, requiredStrength: 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateEquipment_ShouldReturnNull_WhenValid()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 2;
        var equipment = new Equipment { Name = "Sword", Type = "weapon", Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, equipment);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateEquipment_ShouldReturnError_WhenNotEnoughSlots()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = -4; // Equipment slots = 5 + (-4) = 1
        character.Equipment.Add(new Equipment { Name = "Dagger", Slots = 1, Type = "weapon" });
        var equipment = new Equipment { Name = "Sword", Type = "weapon", Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, equipment);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Not enough equipment slots", result);
    }

    [Fact]
    public void ValidateEquipment_ShouldReturnError_WhenBodyArmorAlreadyEquipped()
    {
        // Arrange
        var character = new Character("Test");
        character.Equipment.Add(new Equipment { Name = "Light", Type = "armor", ArmorType = "body", ArmorValue = 1, Slots = 1, IsEquipped = true });
        var armor = new Equipment { Name = "Heavy", Type = "armor", ArmorType = "body", ArmorValue = 3, Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, armor);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("only wear one body armor", result);
    }

    [Fact]
    public void ValidateEquipment_ShouldAllowBodyArmor_WhenExistingBodyArmorInInventoryOnly()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 2; // Give enough slots
        // Add body armor in inventory (not equipped)
        character.Equipment.Add(new Equipment { Name = "Inventory Armor", Type = "armor", ArmorType = "body", ArmorValue = 1, Slots = 1, IsEquipped = false });

        // Try to equip different body armor
        var newArmor = new Equipment { Name = "New Armor", Type = "armor", ArmorType = "body", ArmorValue = 2, Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, newArmor);

        // Assert - Should be valid since no body armor is currently equipped
        Assert.Null(result);
    }

    [Fact]
    public void ValidateEquipment_ShouldReturnError_WhenStatRequirementsNotMet()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 0;
        var equipment = new Equipment
        {
            Name = "Power/Plate",
            Type = "armor",
            ArmorValue = 4,
            Slots = 1,
            Special = "Must have 2+ Strength to use"
        };

        // Act
        var result = _validator.ValidateEquipment(character, equipment, requiredStrength: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Requires Strength 2+", result);
    }

    [Fact]
    public void ValidateEquipment_ShouldAllowMultipleWeapons()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 2; // Equipment slots = 7
        character.Equipment.Add(new Equipment { Name = "Sword", Type = "weapon", Slots = 1 });
        character.Equipment.Add(new Equipment { Name = "Dagger", Type = "weapon", Slots = 1 });
        var weapon = new Equipment { Name = "Axe", Type = "weapon", Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, weapon);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateEquipment_ShouldAllowMultipleItems()
    {
        // Arrange
        var character = new Character("Test");
        character.Stats.Strength = 2; // Equipment slots = 7
        character.Equipment.Add(new Equipment { Name = "Cheese", Type = "item", Slots = 1 });
        character.Equipment.Add(new Equipment { Name = "Arrows", Type = "item", Slots = 1 });
        var item = new Equipment { Name = "Rope", Type = "item", Slots = 1 };

        // Act
        var result = _validator.ValidateEquipment(character, item);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStatRequirements_FromEquipment_ShouldReturnNoRequirements_WhenNoRestrictions()
    {
        // Arrange
        var equipment = new Equipment
        {
            Name = "Light Armor",
            Type = "armor",
            Restrictions = new List<Dictionary<string, object>>()
        };

        // Act
        var (strength, agility, presence, toughness) = _validator.ExtractStatRequirements(equipment);

        // Assert
        Assert.Null(strength);
        Assert.Null(agility);
        Assert.Null(presence);
        Assert.Null(toughness);
    }

    [Fact]
    public void ExtractStatRequirements_FromEquipment_ShouldExtractStrengthRequirement()
    {
        // Arrange
        var equipment = new Equipment
        {
            Name = "Heavy Armor",
            Type = "armor",
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

        // Act
        var (strength, agility, presence, toughness) = _validator.ExtractStatRequirements(equipment);

        // Assert
        Assert.Equal(2, strength);
        Assert.Null(agility);
        Assert.Null(presence);
        Assert.Null(toughness);
    }

    [Fact]
    public void ExtractStatRequirements_FromEquipment_ShouldExtractMultipleRequirements()
    {
        // Arrange
        var equipment = new Equipment
        {
            Name = "Master Armor",
            Type = "armor",
            Restrictions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "strength" },
                    { "requiredValue", 2 }
                },
                new Dictionary<string, object>
                {
                    { "type", "stat_requirement" },
                    { "stat", "agility" },
                    { "requiredValue", 1 }
                }
            }
        };

        // Act
        var (strength, agility, presence, toughness) = _validator.ExtractStatRequirements(equipment);

        // Assert
        Assert.Equal(2, strength);
        Assert.Equal(1, agility);
        Assert.Null(presence);
        Assert.Null(toughness);
    }
}
