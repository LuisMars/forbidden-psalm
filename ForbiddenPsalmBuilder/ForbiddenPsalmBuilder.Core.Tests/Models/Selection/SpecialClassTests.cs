using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class SpecialClassTests
{
    [Fact]
    public void SpecialClass_ShouldImplementISelectableItem()
    {
        // Arrange & Act
        var specialClass = new SpecialClass();

        // Assert
        Assert.IsAssignableFrom<ISelectableItem>(specialClass);
    }

    [Fact]
    public void SpecialClass_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var specialClass = new SpecialClass();

        // Assert
        Assert.NotNull(specialClass.Id);
        Assert.NotEmpty(specialClass.Id);
        Assert.Equal(string.Empty, specialClass.Name);
        Assert.Equal(string.Empty, specialClass.DisplayName);
        Assert.Equal(string.Empty, specialClass.Description);
        Assert.Equal("special-class", specialClass.Category);
        Assert.Null(specialClass.Cost);
        Assert.Equal("fas fa-user", specialClass.IconClass);
        Assert.NotNull(specialClass.Metadata);
        Assert.NotNull(specialClass.StartingEquipment);
        Assert.Empty(specialClass.StartingEquipment);
        Assert.NotNull(specialClass.StatBonuses);
        Assert.Empty(specialClass.StatBonuses);
        Assert.NotNull(specialClass.Abilities);
        Assert.Empty(specialClass.Abilities);
        Assert.False(specialClass.IsSpellcaster);
    }

    [Fact]
    public void SpecialClass_WithProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var specialClass = new SpecialClass
        {
            Id = "witch",
            Name = "Witch",
            DisplayName = "Witch",
            Description = "A spellcaster with magical abilities",
            Cost = 5,
            IconClass = "ra ra-crystal-ball",
            GameVariant = "28-psalms",
            StartingEquipment = new List<string> { "scroll1", "scroll2" },
            StatBonuses = new Dictionary<string, int> { { "presence", 1 } },
            Abilities = new List<string> { "Cast spells", "Use manuscripts" }
        };

        // Assert
        Assert.Equal("witch", specialClass.Id);
        Assert.Equal("Witch", specialClass.Name);
        Assert.Equal("A spellcaster with magical abilities", specialClass.Description);
        Assert.Equal(5, specialClass.Cost);
        Assert.Equal("ra ra-crystal-ball", specialClass.IconClass);
        Assert.Equal("28-psalms", specialClass.GameVariant);
        Assert.Equal(2, specialClass.StartingEquipment.Count);
        Assert.Single(specialClass.StatBonuses);
        Assert.Equal(2, specialClass.Abilities.Count);
    }

    [Fact]
    public void GetDetailedInfo_ShouldIncludeAllRelevantInformation()
    {
        // Arrange
        var specialClass = new SpecialClass
        {
            Name = "Sniper",
            Description = "Expert marksman",
            Cost = 5,
            StartingEquipment = new List<string> { "Sniper Rifle", "5 Ammo" },
            StatBonuses = new Dictionary<string, int> { { "presence", 1 } },
            Abilities = new List<string> { "+1 Presence" }
        };

        // Act
        var detailedInfo = specialClass.GetDetailedInfo();

        // Assert
        Assert.Contains("Sniper", detailedInfo);
        Assert.Contains("Expert marksman", detailedInfo);
        Assert.Contains("5", detailedInfo);
        Assert.Contains("Sniper Rifle", detailedInfo);
        Assert.Contains("+1 Presence", detailedInfo);
    }

    [Fact]
    public void GetDetailedInfo_WithNoStartingEquipment_ShouldNotShowEquipmentSection()
    {
        // Arrange
        var specialClass = new SpecialClass
        {
            Name = "Regular",
            Description = "Standard trooper",
            StartingEquipment = new List<string>()
        };

        // Act
        var detailedInfo = specialClass.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("Starting Equipment", detailedInfo);
    }

    [Fact]
    public void GetDetailedInfo_WithNoAbilities_ShouldNotShowAbilitiesSection()
    {
        // Arrange
        var specialClass = new SpecialClass
        {
            Name = "Regular",
            Description = "Standard trooper",
            Abilities = new List<string>()
        };

        // Act
        var detailedInfo = specialClass.GetDetailedInfo();

        // Assert
        Assert.DoesNotContain("Abilities", detailedInfo);
    }

    [Fact]
    public void Metadata_ShouldStoreAdditionalInformation()
    {
        // Arrange
        var specialClass = new SpecialClass();

        // Act
        specialClass.Metadata["replacementCost"] = 0;
        specialClass.Metadata["freeEquipment"] = true;
        specialClass.Metadata["limit"] = 1;

        // Assert
        Assert.Equal(3, specialClass.Metadata.Count);
        Assert.Equal(0, specialClass.Metadata["replacementCost"]);
        Assert.Equal(true, specialClass.Metadata["freeEquipment"]);
        Assert.Equal(1, specialClass.Metadata["limit"]);
    }

    [Fact]
    public void SpecialClass_Category_ShouldAlwaysBeSpecialClass()
    {
        // Arrange & Act
        var specialClass1 = new SpecialClass { Name = "Witch" };
        var specialClass2 = new SpecialClass { Name = "Sniper" };

        // Assert
        Assert.Equal("special-class", specialClass1.Category);
        Assert.Equal("special-class", specialClass2.Category);
    }

    [Fact]
    public void SpecialClass_IsSpellcaster_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var specialClass = new SpecialClass();

        // Assert
        Assert.False(specialClass.IsSpellcaster);
    }

    [Fact]
    public void SpecialClass_IsSpellcaster_CanBeSetToTrue()
    {
        // Arrange & Act
        var spellcaster = new SpecialClass
        {
            Name = "Witch",
            IsSpellcaster = true
        };

        // Assert
        Assert.True(spellcaster.IsSpellcaster);
    }
}