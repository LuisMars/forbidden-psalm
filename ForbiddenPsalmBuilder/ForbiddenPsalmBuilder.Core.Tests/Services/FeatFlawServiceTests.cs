using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class FeatFlawServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly FeatFlawService _service;

    public FeatFlawServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new FeatFlawService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldReturnFeats_ForEndTimes()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Intimidating Presence"",
                    ""effect"": ""Force enemy to make Presence test"",
                    ""type"": ""permanent"",
                    ""category"": ""special_action""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Intimidating Presence", result[0].Name);
        Assert.Equal("Force enemy to make Presence test", result[0].Effect);
        Assert.Equal("1-4", result[0].RollRange);
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldReturnFlaws_For28Psalms()
    {
        // Arrange
        var json = @"{
            ""flaws"": [
                {
                    ""roll"": 1,
                    ""name"": ""Xeno"",
                    ""effect"": ""No healing from others"",
                    ""type"": ""permanent"",
                    ""category"": ""restriction""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFlawsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Xeno", result[0].Name);
        Assert.Equal("No healing from others", result[0].Effect);
        Assert.Equal(1, result[0].Roll);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldReturnFeats_ForLastWar()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Battle Hardened"",
                    ""effect"": ""+1 to morale tests"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("last-war", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("last-war");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Battle Hardened", result[0].Name);
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldReturnFlaws_ForLastWar()
    {
        // Arrange
        var json = @"{
            ""flaws"": [
                {
                    ""roll"": 1,
                    ""name"": ""Damned"",
                    ""effect"": ""Roll two more times"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("last-war", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFlawsAsync("last-war");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Damned", result[0].Name);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldCacheResults()
    {
        // Arrange
        var json = @"{""feats"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        await _service.GetFeatsAsync("end-times");
        await _service.GetFeatsAsync("end-times");

        // Assert - Resource should only be called once due to caching
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"),
            Times.Once);
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldCacheResults()
    {
        // Arrange
        var json = @"{""flaws"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        await _service.GetFlawsAsync("end-times");
        await _service.GetFlawsAsync("end-times");

        // Assert - Resource should only be called once due to caching
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"),
            Times.Once);
    }

    [Fact]
    public async Task RollFeat_ShouldReturnValidFeat()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-50"",
                    ""name"": ""Common Feat"",
                    ""effect"": ""Common effect"",
                    ""type"": ""permanent""
                },
                {
                    ""rollRange"": ""51-100"",
                    ""name"": ""Rare Feat"",
                    ""effect"": ""Rare effect"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.RollFeatAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Name, new[] { "Common Feat", "Rare Feat" });
    }

    [Fact]
    public async Task RollFlaw_ShouldReturnValidFlaw()
    {
        // Arrange
        var json = @"{
            ""flaws"": [
                {
                    ""rollRange"": ""1-50"",
                    ""name"": ""Common Flaw"",
                    ""effect"": ""Common effect"",
                    ""type"": ""permanent""
                },
                {
                    ""rollRange"": ""51-100"",
                    ""name"": ""Rare Flaw"",
                    ""effect"": ""Rare effect"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.RollFlawAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Name, new[] { "Common Flaw", "Rare Flaw" });
    }

    [Fact]
    public async Task RollFeat_ShouldMatchRollRange()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Feat 1-4"",
                    ""effect"": ""Effect 1-4"",
                    ""type"": ""permanent""
                },
                {
                    ""rollRange"": ""5-100"",
                    ""name"": ""Feat 5-100"",
                    ""effect"": ""Effect 5-100"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act - Roll multiple times to test distribution
        var results = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var result = await _service.RollFeatAsync("end-times");
            results.Add(result.Name);
        }

        // Assert - Should get both feats in distribution (with 100 rolls, very likely to get both)
        Assert.Contains("Feat 1-4", results);
        Assert.Contains("Feat 5-100", results);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldReturnEmptyList_WhenNoFeats()
    {
        // Arrange
        var json = @"{""feats"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldReturnEmptyList_WhenNoFlaws()
    {
        // Arrange
        var json = @"{""flaws"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFlawsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RollFeat_ShouldThrow_WhenNoFeatsAvailable()
    {
        // Arrange
        var json = @"{""feats"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.RollFeatAsync("end-times"));
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldParseEffectObjects_WithStatModifier()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Strong"",
                    ""effect"": ""+1 Strength"",
                    ""type"": ""permanent"",
                    ""effects"": [
                        {
                            ""type"": ""stat_modifier"",
                            ""stat"": ""strength"",
                            ""modifier"": 1
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var feat = result[0];
        Assert.Equal("Strong", feat.Name);
        Assert.NotNull(feat.Effects);
        Assert.Single(feat.Effects);

        var effect = feat.Effects[0];
        Assert.Equal("stat_modifier", effect["type"]);
        Assert.Equal("strength", effect["stat"]);
        var modifier = effect["modifier"];
        Assert.True(modifier is int || modifier is double);
        Assert.Equal(1, Convert.ToInt32(modifier));
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldParseEffectObjects_WithMultipleEffects()
    {
        // Arrange
        var json = @"{
            ""flaws"": [
                {
                    ""roll"": 1,
                    ""name"": ""Weak and Slow"",
                    ""effect"": ""-1 Strength, -1 Agility"",
                    ""type"": ""permanent"",
                    ""effects"": [
                        {
                            ""type"": ""stat_modifier"",
                            ""stat"": ""strength"",
                            ""modifier"": -1
                        },
                        {
                            ""type"": ""stat_modifier"",
                            ""stat"": ""agility"",
                            ""modifier"": -1
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFlawsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var flaw = result[0];
        Assert.NotNull(flaw.Effects);
        Assert.Equal(2, flaw.Effects.Count);

        Assert.Equal("stat_modifier", flaw.Effects[0]["type"]);
        Assert.Equal("strength", flaw.Effects[0]["stat"]);
        Assert.Equal(-1, Convert.ToInt32(flaw.Effects[0]["modifier"]));

        Assert.Equal("stat_modifier", flaw.Effects[1]["type"]);
        Assert.Equal("agility", flaw.Effects[1]["stat"]);
        Assert.Equal(-1, Convert.ToInt32(flaw.Effects[1]["modifier"]));
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldParseEffectObjects_WithComplexEffect()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-10"",
                    ""name"": ""Lucky"",
                    ""effect"": ""Reroll one failed test per scenario"",
                    ""type"": ""permanent"",
                    ""effects"": [
                        {
                            ""type"": ""reroll_ability"",
                            ""scope"": ""failed_test"",
                            ""limit"": 1,
                            ""refresh"": ""scenario""
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var feat = result[0];
        Assert.NotNull(feat.Effects);
        Assert.Single(feat.Effects);

        var effect = feat.Effects[0];
        Assert.Equal("reroll_ability", effect["type"]);
        Assert.Equal("failed_test", effect["scope"]);
        Assert.Equal(1, Convert.ToInt32(effect["limit"]));
        Assert.Equal("scenario", effect["refresh"]);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldHandleMissingEffects_Gracefully()
    {
        // Arrange - Feat without effects property (backward compatibility)
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Old Format Feat"",
                    ""effect"": ""Some effect"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var feat = result[0];
        Assert.Equal("Old Format Feat", feat.Name);
        Assert.NotNull(feat.Effects);
        Assert.Empty(feat.Effects); // Should have empty list, not null
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldParseEffectObjects_WithSpecialAction()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""81-84"",
                    ""name"": ""Improvised Fighter"",
                    ""effect"": ""As action, can make One-Handed Makeshift Weapon mid-Scenario"",
                    ""type"": ""permanent"",
                    ""category"": ""special_action"",
                    ""effects"": [
                        {
                            ""type"": ""special_action"",
                            ""action"": ""create_weapon""
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var feat = result[0];
        Assert.Equal("Improvised Fighter", feat.Name);
        Assert.NotNull(feat.Effects);
        Assert.Single(feat.Effects);

        var effect = feat.Effects[0];
        Assert.Equal("special_action", effect["type"]);
        Assert.Equal("create_weapon", effect["action"]);
    }

    [Fact]
    public async Task GetFeatsAsync_ShouldParseEffectObjects_WithEquipmentRestriction()
    {
        // Arrange
        var json = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-4"",
                    ""name"": ""Weapon Specialist"",
                    ""effect"": ""Can only use specific weapon types"",
                    ""type"": ""permanent"",
                    ""effects"": [
                        {
                            ""type"": ""equipment_restriction"",
                            ""weaponTypes"": [""one_handed"", ""two_handed""],
                            ""slotModifier"": -1
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFeatsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var feat = result[0];
        Assert.NotNull(feat.Effects);
        Assert.Single(feat.Effects);

        var effect = feat.Effects[0];
        Assert.Equal("equipment_restriction", effect["type"]);
        Assert.True(effect.ContainsKey("weaponTypes"));
        Assert.Equal(-1, Convert.ToInt32(effect["slotModifier"]));
    }

    [Fact]
    public async Task GetFlawsAsync_ShouldParseEffectObjects_WithBehavioral()
    {
        // Arrange
        var json = @"{
            ""flaws"": [
                {
                    ""roll"": 13,
                    ""name"": ""Phantom Limb"",
                    ""effect"": ""Occasionally acts as if lost limb still there"",
                    ""type"": ""permanent"",
                    ""category"": ""behavioral"",
                    ""effects"": [
                        {
                            ""type"": ""behavioral"",
                            ""description"": ""Occasionally acts as if lost limb still there""
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("last-war", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetFlawsAsync("last-war");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var flaw = result[0];
        Assert.Equal("Phantom Limb", flaw.Name);
        Assert.NotNull(flaw.Effects);
        Assert.Single(flaw.Effects);

        var effect = flaw.Effects[0];
        Assert.Equal("behavioral", effect["type"]);
        Assert.Equal("Occasionally acts as if lost limb still there", effect["description"]);
    }
}
