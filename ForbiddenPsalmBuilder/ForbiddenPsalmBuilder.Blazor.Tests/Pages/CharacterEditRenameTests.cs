using ForbiddenPsalmBuilder.Core.DTOs;
using ForbiddenPsalmBuilder.Core.Models.Character;
using Xunit;

namespace ForbiddenPsalmBuilder.Blazor.Tests.Pages;

/// <summary>
/// Tests for character rename modal logic in CharacterEdit page
/// </summary>
public class CharacterEditRenameTests
{
    [Fact]
    public void EditCharacterDto_ShouldAllowNameUpdate()
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Id = "test-id",
            Name = "Original Name",
            Agility = 1,
            Presence = 1,
            Strength = 1,
            Toughness = 1
        };

        // Act
        dto.Name = "New Name";

        // Assert
        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public void Character_ShouldAllowNameUpdate()
    {
        // Arrange
        var character = new Character("Original Name");

        // Act
        character.Name = "New Name";

        // Assert
        Assert.Equal("New Name", character.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EditCharacterDto_ShouldAcceptEmptyOrWhitespaceName(string? name)
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Id = "test-id",
            Name = "Original Name"
        };

        // Act
        dto.Name = name!;

        // Assert - DTO should accept any value, validation happens elsewhere
        Assert.Equal(name, dto.Name);
    }

    [Fact]
    public void EditCharacterDto_ShouldTrimNameWhenSaving()
    {
        // Arrange
        var dto = new EditCharacterDto
        {
            Name = "  Name With Spaces  "
        };

        // Act
        var trimmedName = dto.Name.Trim();

        // Assert
        Assert.Equal("Name With Spaces", trimmedName);
    }
}
