using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class WarbandExportFormatterTests
{
    private readonly WarbandExportFormatterService _formatter;
    private readonly Warband _testWarband;

    public WarbandExportFormatterTests()
    {
        _formatter = new WarbandExportFormatterService();

        // Create a test warband with sample data
        _testWarband = new Warband("Test Warband", "end-times")
        {
            Gold = 150,
            Experience = 12
        };

        var character1 = new Character("Brave Knight")
        {
            Stats = new Stats(7, 5, 8, 6),
            CurrentHP = 5,
            CurrentGold = 25
        };
        character1.Feats.Add("Tough");
        character1.Flaws.Add("Slow Learner");
        character1.Equipment.Add(new Equipment
        {
            Id = "sword1",
            Name = "Longsword",
            Type = "weapon",
            Damage = "2",
            IsEquipped = true
        });

        var character2 = new Character("Wise Wizard")
        {
            Stats = new Stats(4, 8, 3, 4),
            CurrentHP = 3,
            CurrentGold = 10
        };
        character2.Spells.Add("Fireball");

        _testWarband.Members.Add(character1);
        _testWarband.Members.Add(character2);

        // Add a dead member
        var deadChar = new Character("Fallen Hero") { Stats = new Stats(5, 5, 5, 5) };
        _testWarband.DeadMembers.Add(deadChar);

        // Add stash item
        _testWarband.Stash.Add(new Equipment
        {
            Id = "potion1",
            Name = "Healing Potion",
            Type = "item"
        });
    }

    [Fact]
    public void FormatAsMarkdown_WithPrintFriendlyOptions_ShouldIncludeActiveMembers()
    {
        // Arrange
        var options = ExportOptions.PrintFriendly();

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test Warband", result);
        Assert.Contains("Brave Knight", result);
        Assert.Contains("Wise Wizard", result);
        Assert.DoesNotContain("Fallen Hero", result); // Dead member not included
        Assert.DoesNotContain("Healing Potion", result); // Stash not included
    }

    [Fact]
    public void FormatAsMarkdown_WithFullCampaign_ShouldIncludeEverything()
    {
        // Arrange
        var options = ExportOptions.FullCampaign();

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test Warband", result);
        Assert.Contains("Brave Knight", result);
        Assert.Contains("Wise Wizard", result);
        Assert.Contains("Fallen Hero", result); // Dead member included
        Assert.Contains("Healing Potion", result); // Stash included
    }

    [Fact]
    public void FormatAsMarkdown_ShouldIncludeWarbandStats()
    {
        // Arrange
        var options = ExportOptions.PrintFriendly();

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.Contains("150", result); // Gold
        Assert.Contains("12", result); // Experience
    }

    [Fact]
    public void FormatAsMarkdown_WithIncludeEquipment_ShouldShowEquipment()
    {
        // Arrange
        var options = new ExportOptions { IncludeEquipment = true };

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.Contains("Longsword", result);
    }

    [Fact]
    public void FormatAsMarkdown_WithoutEquipment_ShouldNotShowEquipment()
    {
        // Arrange
        var options = new ExportOptions { IncludeEquipment = false };

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.DoesNotContain("Longsword", result);
    }

    [Fact]
    public void FormatAsMarkdown_ShouldUseMarkdownFormatting()
    {
        // Arrange
        var options = ExportOptions.PrintFriendly();

        // Act
        var result = _formatter.FormatAsMarkdown(_testWarband, options);

        // Assert
        Assert.Contains("#", result); // Headers
        Assert.Contains("|", result); // Tables
    }
}
