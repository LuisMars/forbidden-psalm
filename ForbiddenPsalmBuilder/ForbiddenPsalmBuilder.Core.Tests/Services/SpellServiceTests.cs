using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class SpellServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly SpellService _service;

    public SpellServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new SpellService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldReturnSpells_For28Psalms()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Fireball"",
                    ""effect"": ""Deals 2d6 fire damage"",
                    ""type"": ""offensive"",
                    ""cost"": 10
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Fireball", result[0].Name);
        Assert.Equal("Deals 2d6 fire damage", result[0].Effect);
        Assert.Equal("offensive", result[0].Type);
        Assert.Equal(10, result[0].Cost);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldCacheResults()
    {
        // Arrange
        var json = @"{""spells"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        await _service.GetSpellsAsync("28-psalms");
        await _service.GetSpellsAsync("28-psalms");

        // Assert - Resource should only be called once due to caching
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "spells.json"),
            Times.Once);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldReturnEmptyList_WhenNoSpells()
    {
        // Arrange
        var json = @"{""spells"": []}";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldParseEffectObjects()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Heal"",
                    ""effect"": ""Restore 1d6 HP"",
                    ""type"": ""support"",
                    ""effects"": [
                        {
                            ""type"": ""heal"",
                            ""dice"": ""1d6""
                        }
                    ]
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var spell = result[0];
        Assert.Equal("Heal", spell.Name);
        Assert.NotNull(spell.Effects);
        Assert.Single(spell.Effects);

        var effect = spell.Effects[0];
        Assert.Equal("heal", effect["type"]);
        Assert.Equal("1d6", effect["dice"]);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldHandleMissingEffects_Gracefully()
    {
        // Arrange - Spell without effects property
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Simple Spell"",
                    ""effect"": ""Some effect"",
                    ""type"": ""utility""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var spell = result[0];
        Assert.Equal("Simple Spell", spell.Name);
        Assert.NotNull(spell.Effects);
        Assert.Empty(spell.Effects); // Should have empty list, not null
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldReturnEmptyList_WhenResourceNotFound()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((JsonDocument?)null);

        // Act
        var result = await _service.GetSpellsAsync("invalid-variant");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldParseMultipleSpells()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Fireball"",
                    ""effect"": ""Deals fire damage"",
                    ""type"": ""offensive""
                },
                {
                    ""name"": ""Shield"",
                    ""effect"": ""Gain +2 AV"",
                    ""type"": ""defensive""
                },
                {
                    ""name"": ""Heal"",
                    ""effect"": ""Restore HP"",
                    ""type"": ""support""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Fireball", result[0].Name);
        Assert.Equal("Shield", result[1].Name);
        Assert.Equal("Heal", result[2].Name);
    }

    [Fact]
    public async Task GetSpellsAsync_ShouldParseSpellCategory()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Hope's Last Breath"",
                    ""effect"": ""Heal model within 1 inch for D6 HP"",
                    ""category"": ""clean""
                },
                {
                    ""name"": ""Shadow Bind"",
                    ""effect"": ""Target cannot move for D3 rounds"",
                    ""category"": ""unclean""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("clean", result[0].SpellCategory);
        Assert.Equal("unclean", result[1].SpellCategory);
    }

    [Fact]
    public async Task GetSpellsByCategoryAsync_ShouldReturnOnlyCleanSpells()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Hope's Last Breath"",
                    ""effect"": ""Heal model"",
                    ""category"": ""clean""
                },
                {
                    ""name"": ""Shadow Bind"",
                    ""effect"": ""Target cannot move"",
                    ""category"": ""unclean""
                },
                {
                    ""name"": ""Will of the Optimistic"",
                    ""effect"": ""Target adds extra D6"",
                    ""category"": ""clean""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.GetSpellsByCategoryAsync("end-times", "clean");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, spell => Assert.Equal("clean", spell.SpellCategory));
    }

    [Fact]
    public async Task RollRandomSpellAsync_ShouldReturnSpellFromCategory()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Spell1"",
                    ""effect"": ""Effect1"",
                    ""category"": ""clean""
                },
                {
                    ""name"": ""Spell2"",
                    ""effect"": ""Effect2"",
                    ""category"": ""clean""
                },
                {
                    ""name"": ""Spell3"",
                    ""effect"": ""Effect3"",
                    ""category"": ""unclean""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.RollRandomSpellAsync("end-times", "clean");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("clean", result.SpellCategory);
        Assert.Contains(result.Name, new[] { "Spell1", "Spell2" });
    }

    [Fact]
    public async Task RollRandomSpellAsync_ShouldReturnNull_WhenNoCategoryMatch()
    {
        // Arrange
        var json = @"{
            ""spells"": [
                {
                    ""name"": ""Spell1"",
                    ""effect"": ""Effect1"",
                    ""category"": ""clean""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(json);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("end-times", "spells.json"))
            .ReturnsAsync(jsonDoc);

        // Act
        var result = await _service.RollRandomSpellAsync("end-times", "nonexistent");

        // Assert
        Assert.Null(result);
    }
}
