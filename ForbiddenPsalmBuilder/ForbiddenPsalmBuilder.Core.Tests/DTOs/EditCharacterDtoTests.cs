using ForbiddenPsalmBuilder.Core.DTOs;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.DTOs;

public class EditCharacterDtoTests
{
    [Fact]
    public void CalculatedStats_ShouldReturnCorrectValues()
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Agility = 3,
            Strength = 2,
            Toughness = 4
        };

        // Act & Assert
        Assert.Equal(8, dto.Movement); // 5 + 3
        Assert.Equal(12, dto.HP); // 8 + 4
        Assert.Equal(7, dto.EquipmentSlots); // 5 + 2
    }

    [Fact]
    public void CalculatedStats_WithZeroAttributes_ShouldReturnBaseValues()
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Agility = 0,
            Strength = 0,
            Toughness = 0
        };

        // Act & Assert
        Assert.Equal(5, dto.Movement); // 5 + 0
        Assert.Equal(8, dto.HP); // 8 + 0
        Assert.Equal(5, dto.EquipmentSlots); // 5 + 0
    }

    [Fact]
    public void CalculatedStats_WithMaxAttributes_ShouldReturnMaxValues()
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Agility = 10,
            Strength = 10,
            Toughness = 10
        };

        // Act & Assert
        Assert.Equal(15, dto.Movement); // 5 + 10
        Assert.Equal(18, dto.HP); // 8 + 10
        Assert.Equal(15, dto.EquipmentSlots); // 5 + 10
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var dto = new EditCharacterDto();

        // Assert
        Assert.Equal(string.Empty, dto.Id);
        Assert.Equal(string.Empty, dto.Name);
        Assert.Equal(0, dto.Agility);
        Assert.Equal(0, dto.Presence);
        Assert.Equal(0, dto.Strength);
        Assert.Equal(0, dto.Toughness);
        Assert.Null(dto.SpecialClassId);
    }
}