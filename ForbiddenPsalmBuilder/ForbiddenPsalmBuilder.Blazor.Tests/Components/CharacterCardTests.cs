using Bunit;
using ForbiddenPsalmBuilder.Blazor.Components;
using ForbiddenPsalmBuilder.Core.Models.Character;
using Xunit;

namespace ForbiddenPsalmBuilder.Blazor.Tests.Components;

public class CharacterCardTests : TestContext
{
    [Fact]
    public void CharacterCard_ShouldDisplayCharacterName()
    {
        // Arrange
        var character = new Character("Test Hero")
        {
            Stats = new Stats(2, 1, 0, -1)
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var nameElement = cut.Find("h3");
        Assert.Equal("Test Hero", nameElement.TextContent);
    }

    [Fact]
    public void CharacterCard_ShouldDisplayBaseStats()
    {
        // Arrange
        var character = new Character("Warrior")
        {
            Stats = new Stats(agility: 2, presence: 1, strength: 3, toughness: 0)
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var statsContainers = cut.FindAll(".stats-container");
        Assert.True(statsContainers.Count >= 1, "Should have at least one stats container");
        var mainStats = statsContainers[0].TextContent;
        Assert.Contains("AGI", mainStats);
        Assert.Contains("2", mainStats);
        Assert.Contains("PRE", mainStats);
        Assert.Contains("1", mainStats);
        Assert.Contains("STR", mainStats);
        Assert.Contains("3", mainStats);
        Assert.Contains("TGH", mainStats);
        Assert.Contains("0", mainStats);
    }

    [Fact]
    public void CharacterCard_ShouldDisplayCalculatedStats()
    {
        // Arrange
        var character = new Character("Rogue")
        {
            Stats = new Stats(agility: 3, presence: 0, strength: 1, toughness: 2),
            CurrentHP = 8
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var statsContainers = cut.FindAll(".stats-container");
        Assert.True(statsContainers.Count >= 2, "Should have at least two stats containers");
        var calculatedStats = statsContainers[1].TextContent;

        // HP: 8 + toughness(2) = 10 (players heal between games, so just show max)
        Assert.Contains("HP", calculatedStats);
        Assert.Contains("10", calculatedStats);

        // Movement: 5 + agility(3) = 8
        Assert.Contains("MOV", calculatedStats);
        Assert.Contains("8", calculatedStats);

        // Armor Value: 0 (no armor equipped)
        Assert.Contains("AV", calculatedStats);

        // Note: SLOTS is only visible in print mode, not in normal view
    }

    [Fact]
    public void CharacterCard_ShouldDisplaySpecialClassTag()
    {
        // Arrange
        var character = new Character("Mage")
        {
            Stats = new Stats(1, 2, 0, 1),
            SpecialClassId = "witch"
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var tagsDiv = cut.Find(".character-tags");
        Assert.Contains("witch", tagsDiv.TextContent);
    }

    [Fact]
    public void CharacterCard_ShouldDisplaySpecialClassForSniper()
    {
        // Arrange
        var character = new Character("Sniper")
        {
            Stats = new Stats(3, 0, 1, 1),
            SpecialClassId = "sniper"
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var tagsDiv = cut.Find(".character-tags");
        Assert.Contains("sniper", tagsDiv.TextContent);
    }

    [Fact]
    public void CharacterCard_ShouldDisplayEquipment()
    {
        // Arrange
        var character = new Character("Knight")
        {
            Stats = new Stats(1, 1, 2, 2),
            Equipment = new List<Equipment>
            {
                new Equipment { Name = "Sword", Slots = 1 },
                new Equipment { Name = "Shield", Slots = 1 }
            }
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var equipmentDiv = cut.Find(".character-equipment-grid");
        Assert.Contains("Sword", equipmentDiv.TextContent);
        Assert.Contains("Shield", equipmentDiv.TextContent);

        // Equipment header should show slot usage
        var equipmentSection = cut.Find("small");
        Assert.Contains("2/7", equipmentSection.TextContent); // 5 + strength(2) = 7 slots
    }

    [Fact]
    public void CharacterCard_ShouldDisplayInjuries()
    {
        // Arrange
        var character = new Character("Wounded")
        {
            Stats = new Stats(1, 1, 1, 1),
            Injuries = new List<string> { "Broken Leg", "Scarred Face" }
        };

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character));

        // Assert
        var injuriesDiv = cut.Find(".character-injuries");
        Assert.Contains("Broken Leg", injuriesDiv.TextContent);
        Assert.Contains("Scarred Face", injuriesDiv.TextContent);
    }

    [Fact]
    public void CharacterCard_ShouldTriggerEditCallback()
    {
        // Arrange
        var character = new Character("Test")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var editCalled = false;

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character)
            .Add(p => p.OnEdit, () => editCalled = true));

        var editButton = cut.FindAll("button")[0];
        editButton.Click();

        // Assert
        Assert.True(editCalled);
    }

    [Fact]
    public void CharacterCard_ShouldTriggerDeleteCallback()
    {
        // Arrange
        var character = new Character("Test")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var deleteCalled = false;

        // Setup JSInterop for Modal
        JSInterop.SetupVoid("document.body.classList.add", _ => true);
        JSInterop.SetupVoid("document.body.classList.remove", _ => true);

        // Act
        var cut = RenderComponent<CharacterCard>(parameters => parameters
            .Add(p => p.Character, character)
            .Add(p => p.OnDelete, () => deleteCalled = true));

        // Click delete button to show confirmation modal
        var deleteButton = cut.FindAll("button")[1];
        deleteButton.Click();

        // Find and click the confirm button in the modal (it's the btn-danger class for "Dead")
        var confirmButton = cut.Find(".btn-danger.flex-fill");
        confirmButton.Click();

        // Assert
        Assert.True(deleteCalled);
    }
}