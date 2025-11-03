using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class SpecialClassServiceTests
{
    [Fact]
    public async Task GetSpecialClassesAsync_ShouldReturnListOfSpecialClasses()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("28-psalms");

        // Assert
        Assert.NotNull(specialClasses);
        Assert.IsAssignableFrom<List<SpecialClass>>(specialClasses);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_ForEndTimes_ShouldReturnSpellcaster()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("end-times");

        // Assert
        var spellcaster = specialClasses.FirstOrDefault(sc => sc.Name == "Spellcaster");
        Assert.NotNull(spellcaster);
        Assert.Equal("spellcaster", spellcaster.Id);
        Assert.Equal("Spellcaster", spellcaster.DisplayName);
        Assert.Equal(5, spellcaster.Cost);
        Assert.Equal("ra ra-scroll-unfurled", spellcaster.IconClass);
        Assert.Contains("end-times", spellcaster.GameVariant);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_For28Psalms_ShouldReturnWitch()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("28-psalms");

        // Assert
        var witch = specialClasses.FirstOrDefault(sc => sc.Name == "Witch");
        Assert.NotNull(witch);
        Assert.Equal("witch", witch.Id);
        Assert.Equal(5, witch.Cost);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_ForLastWar_ShouldReturnMultipleTypes()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("last-war");

        // Assert
        Assert.NotEmpty(specialClasses);

        // Should have Witch, Sniper, Anti-Tank Gunner, etc.
        var witch = specialClasses.FirstOrDefault(sc => sc.Name == "Witch");
        var sniper = specialClasses.FirstOrDefault(sc => sc.Name == "Sniper");
        var antiTank = specialClasses.FirstOrDefault(sc => sc.Name == "Anti-Tank Gunner");

        Assert.NotNull(witch);
        Assert.NotNull(sniper);
        Assert.NotNull(antiTank);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_Sniper_ShouldHaveStatBonus()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("last-war");

        // Assert
        var sniper = specialClasses.FirstOrDefault(sc => sc.Name == "Sniper");
        Assert.NotNull(sniper);
        Assert.NotEmpty(sniper.StatBonuses);
        Assert.True(sniper.StatBonuses.ContainsKey("presence"));
        Assert.Equal(1, sniper.StatBonuses["presence"]);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_Sniper_ShouldHaveStartingEquipment()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("last-war");

        // Assert
        var sniper = specialClasses.FirstOrDefault(sc => sc.Name == "Sniper");
        Assert.NotNull(sniper);
        Assert.NotEmpty(sniper.StartingEquipment);
        Assert.Contains("sniper rifle", sniper.StartingEquipment);
        Assert.Contains("Ammo (5)", sniper.StartingEquipment);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_Witch_ShouldHaveAbilities()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("last-war");

        // Assert
        var witch = specialClasses.FirstOrDefault(sc => sc.Name == "Witch");
        Assert.NotNull(witch);
        Assert.NotEmpty(witch.Abilities);
        Assert.Contains("Can use Manuscripts to cast spells", witch.Abilities);
    }

    [Fact]
    public async Task GetSpecialClassesAsync_AllSpecialClasses_ShouldHaveMetadata()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("last-war");

        // Assert
        foreach (var sc in specialClasses)
        {
            Assert.NotNull(sc.Metadata);
            Assert.True(sc.Metadata.ContainsKey("replacementCost"));
            Assert.True(sc.Metadata.ContainsKey("freeEquipment"));

            // Verify replacement cost and free equipment are consistent
            var replacementCost = sc.Metadata["replacementCost"];
            var freeEquipment = sc.Metadata["freeEquipment"];

            // All should have these basic metadata fields
            Assert.NotNull(replacementCost);
            Assert.NotNull(freeEquipment);
        }
    }

    [Fact]
    public async Task GetSpecialClassesAsync_UnknownVariant_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var specialClasses = await service.GetSpecialClassesAsync("unknown-variant");

        // Assert
        Assert.NotNull(specialClasses);
        Assert.Empty(specialClasses);
    }

    [Fact]
    public async Task GetSpecialClassByIdAsync_WithValidId_ShouldReturnSpecialClass()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var witch = await service.GetSpecialClassByIdAsync("witch", "28-psalms");

        // Assert
        Assert.NotNull(witch);
        Assert.Equal("witch", witch.Id);
        Assert.Equal("Witch", witch.Name);
    }

    [Fact]
    public async Task GetSpecialClassByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var service = new SpecialClassService();

        // Act
        var result = await service.GetSpecialClassByIdAsync("invalid-id", "28-psalms");

        // Assert
        Assert.Null(result);
    }
}