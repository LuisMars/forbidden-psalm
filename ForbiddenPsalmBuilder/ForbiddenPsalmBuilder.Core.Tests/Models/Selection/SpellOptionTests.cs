using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class SpellOptionTests
{
    [Fact]
    public void SpellOption_ShouldImplementISelectableItem()
    {
        // Arrange & Act
        var spell = new SpellOption();

        // Assert
        Assert.IsAssignableFrom<ISelectableItem>(spell);
    }

    [Fact]
    public void SpellOption_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var spell = new SpellOption();

        // Assert
        Assert.NotNull(spell.Id);
        Assert.NotEmpty(spell.Id);
        Assert.Equal(string.Empty, spell.Name);
        Assert.Equal(string.Empty, spell.DisplayName);
        Assert.Equal(string.Empty, spell.Description);
        Assert.Equal("spell", spell.Category);
        Assert.Null(spell.Cost);
        Assert.Equal("fas fa-magic", spell.IconClass);
        Assert.NotNull(spell.Metadata);
        Assert.Equal(string.Empty, spell.Effect);
        Assert.Equal(string.Empty, spell.Type);
        Assert.NotNull(spell.Effects);
        Assert.Empty(spell.Effects);
    }

    [Fact]
    public void SpellOption_WithProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var spell = new SpellOption
        {
            Id = "fireball",
            Name = "Fireball",
            DisplayName = "Fireball",
            Description = "A devastating ball of fire",
            Cost = 10,
            IconClass = "fas fa-fire",
            Effect = "Deals 2d6 fire damage",
            Type = "offensive",
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "damage" },
                    { "dice", "2d6" },
                    { "damageType", "fire" }
                }
            }
        };

        // Assert
        Assert.Equal("fireball", spell.Id);
        Assert.Equal("Fireball", spell.Name);
        Assert.Equal("A devastating ball of fire", spell.Description);
        Assert.Equal(10, spell.Cost);
        Assert.Equal("fas fa-fire", spell.IconClass);
        Assert.Equal("Deals 2d6 fire damage", spell.Effect);
        Assert.Equal("offensive", spell.Type);
        Assert.Single(spell.Effects);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeEffect()
    {
        // Arrange
        var spell = new SpellOption
        {
            Name = "Heal",
            Effect = "Restore 1d6 HP",
            Type = "support"
        };

        // Act
        var detailedInfo = spell.GetDetailedInfo();

        // Assert
        Assert.Contains("Restore 1d6 HP", detailedInfo);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeTypeWhenPresent()
    {
        // Arrange
        var spell = new SpellOption
        {
            Name = "Shield",
            Effect = "Gain +2 AV",
            Type = "defensive"
        };

        // Act
        var detailedInfo = spell.GetDetailedInfo();

        // Assert
        Assert.Contains("defensive", detailedInfo);
    }

    [Fact]
    public void SpellOption_Category_ShouldAlwaysBeSpell()
    {
        // Arrange & Act
        var spell1 = new SpellOption { Name = "Fireball" };
        var spell2 = new SpellOption { Name = "Heal" };

        // Assert
        Assert.Equal("spell", spell1.Category);
        Assert.Equal("spell", spell2.Category);
    }
}
