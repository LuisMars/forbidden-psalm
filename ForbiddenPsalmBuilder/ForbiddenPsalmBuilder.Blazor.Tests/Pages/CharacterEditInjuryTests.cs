using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Blazor.Tests.Pages;

/// <summary>
/// Tests for injury management logic in CharacterEdit page
/// </summary>
public class CharacterEditInjuryTests
{
    [Fact]
    public void AddInjuryToCharacter_ShouldAddInjuryToList()
    {
        // Arrange
        var character = new Character("Test Character");
        var injuryName = "Broken Bones";

        // Act
        character.Injuries.Add(injuryName);

        // Assert
        Assert.Single(character.Injuries);
        Assert.Contains(injuryName, character.Injuries);
    }

    [Fact]
    public void RemoveInjuryFromCharacter_ShouldRemoveFromList()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Injuries.Add("Broken Bones");
        character.Injuries.Add("Weak");

        // Act
        character.Injuries.Remove("Broken Bones");

        // Assert
        Assert.Single(character.Injuries);
        Assert.Contains("Weak", character.Injuries);
        Assert.DoesNotContain("Broken Bones", character.Injuries);
    }

    [Fact]
    public void GetInjuryOptions_ShouldMapNamesToOptions()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Injuries.Add("Broken Bones");
        character.Injuries.Add("Weak");

        var availableInjuries = new List<InjuryOption>
        {
            new InjuryOption
            {
                Name = "Broken Bones",
                Effect = "-1 Agility",
                Type = "permanent",
                Roll = 4
            },
            new InjuryOption
            {
                Name = "Weak",
                Effect = "-1 Strength",
                Type = "permanent",
                Roll = 6
            },
            new InjuryOption
            {
                Name = "Saddened",
                Effect = "-1 Presence",
                Type = "permanent",
                Roll = 5
            }
        };

        // Act
        var injuryOptions = character.Injuries
            .Select(name => availableInjuries.FirstOrDefault(i => i.Name == name))
            .Where(i => i != null)
            .Cast<InjuryOption>()
            .ToList();

        // Assert
        Assert.Equal(2, injuryOptions.Count);
        Assert.Contains(injuryOptions, i => i.Name == "Broken Bones");
        Assert.Contains(injuryOptions, i => i.Name == "Weak");
        Assert.DoesNotContain(injuryOptions, i => i.Name == "Saddened");
    }

    [Fact]
    public void GetInjuryOptions_WithInvalidName_ShouldFilterOut()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Injuries.Add("Broken Bones");
        character.Injuries.Add("NonExistentInjury");

        var availableInjuries = new List<InjuryOption>
        {
            new InjuryOption
            {
                Name = "Broken Bones",
                Effect = "-1 Agility",
                Type = "permanent",
                Roll = 4
            }
        };

        // Act
        var injuryOptions = character.Injuries
            .Select(name => availableInjuries.FirstOrDefault(i => i.Name == name))
            .Where(i => i != null)
            .Cast<InjuryOption>()
            .ToList();

        // Assert
        Assert.Single(injuryOptions);
        Assert.Equal("Broken Bones", injuryOptions[0].Name);
    }

    [Fact]
    public void AddInjury_PreventsDuplicates()
    {
        // Arrange
        var character = new Character("Test Character");
        var injuryName = "Broken Bones";

        // Act
        if (!character.Injuries.Contains(injuryName))
        {
            character.Injuries.Add(injuryName);
        }

        // Try to add again
        if (!character.Injuries.Contains(injuryName))
        {
            character.Injuries.Add(injuryName);
        }

        // Assert
        Assert.Single(character.Injuries);
    }

    [Fact]
    public void RemoveInjury_NonExistent_ShouldNotCrash()
    {
        // Arrange
        var character = new Character("Test Character");
        character.Injuries.Add("Weak");

        // Act
        var result = character.Injuries.Remove("NonExistent");

        // Assert
        Assert.False(result);
        Assert.Single(character.Injuries);
        Assert.Contains("Weak", character.Injuries);
    }

    [Fact]
    public void InjuryOptions_ShouldHaveEffectObjects()
    {
        // Arrange
        var injury = new InjuryOption
        {
            Name = "Broken Bones",
            Effect = "-1 Agility",
            Type = "permanent",
            Roll = 4,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "stat_modifier" },
                    { "stat", "agility" },
                    { "modifier", -1 }
                }
            }
        };

        // Assert
        Assert.NotNull(injury.Effects);
        Assert.Single(injury.Effects);
        Assert.Equal("stat_modifier", injury.Effects[0]["type"]);
        Assert.Equal("agility", injury.Effects[0]["stat"]);
        Assert.Equal(-1, injury.Effects[0]["modifier"]);
    }

    [Fact]
    public void TemporaryInjury_ShouldHaveDuration()
    {
        // Arrange
        var injury = new InjuryOption
        {
            Name = "Sprained Ankle",
            Effect = "-1 Agility for next Scenario only",
            Type = "temporary",
            Duration = "next scenario",
            Roll = 13
        };

        // Assert
        Assert.Equal("temporary", injury.Type);
        Assert.NotNull(injury.Duration);
        Assert.Equal("next scenario", injury.Duration);
    }

    [Fact]
    public void PositiveInjury_ShouldHaveCorrectType()
    {
        // Arrange
        var injury = new InjuryOption
        {
            Name = "Learn through Failure",
            Effect = "Roll for one extra Feat",
            Type = "positive",
            Roll = 19,
            Effects = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "type", "feat_gain" },
                    { "count", 1 }
                }
            }
        };

        // Assert
        Assert.Equal("positive", injury.Type);
        Assert.NotNull(injury.Effects);
        Assert.Single(injury.Effects);
        Assert.Equal("feat_gain", injury.Effects[0]["type"]);
    }
}
