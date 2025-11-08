using ForbiddenPsalmBuilder.Data.Services;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Data.Tests;

public class EmbeddedResourceTests
{
    private readonly IEmbeddedResourceService _resourceService;

    public EmbeddedResourceTests()
    {
        _resourceService = new EmbeddedResourceService();
    }

    [Fact]
    public void GetAllResourceNames_ShouldReturnJsonFiles()
    {
        var resourceNames = _resourceService.GetAllResourceNames().ToList();

        Assert.NotEmpty(resourceNames);
        Assert.All(resourceNames, name => Assert.EndsWith(".json", name));
    }

    [Theory]
    [InlineData("28-psalms", "game-config.json")]
    [InlineData("end-times", "game-config.json")]
    [InlineData("last-war", "game-config.json")]
    public async Task GetGameResourceAsync_ShouldLoadGameConfigs(string gameVariant, string fileName)
    {
        var result = await _resourceService.GetGameResourceAsync<object>(gameVariant, fileName);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("character-creation.json")]
    [InlineData("weapon-properties.json")]
    [InlineData("campaign-progression.json")]
    [InlineData("game-mechanics.json")]
    public async Task GetSharedResourceAsync_ShouldLoadSharedResources(string fileName)
    {
        var result = await _resourceService.GetSharedResourceAsync<object>(fileName);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetResourceAsStringAsync_ShouldReturnValidJson()
    {
        var jsonString = await _resourceService.GetResourceAsStringAsync("data.shared.character-creation.json");

        Assert.NotNull(jsonString);
        Assert.NotEmpty(jsonString);

        // Verify it's valid JSON by attempting to parse
        var parseTest = () => JsonDocument.Parse(jsonString);
        var exception = Record.Exception(parseTest);
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("28-psalms")]
    [InlineData("end-times")]
    [InlineData("last-war")]
    public async Task AllGameVariantConfigs_ShouldHaveRequiredFiles(string gameVariant)
    {
        var requiredFiles = new[]
        {
            "game-config.json",
            "weapons.json",
            "armor.json",
            "equipment.json",
            "feats-flaws.json",
            "injuries.json",
            "magic-system.json",
            "traders.json",
            "pets.json"
        };

        foreach (var fileName in requiredFiles)
        {
            // Use the service method which handles path conversion internally
            var content = await _resourceService.GetGameResourceAsync<object>(gameVariant, fileName);
            Assert.NotNull(content);
            Assert.True(content != null, $"Required file {fileName} not found for game variant {gameVariant}");
        }
    }

    [Fact]
    public async Task SharedResources_ShouldAllBeAccessible()
    {
        var sharedFiles = new[]
        {
            "character-creation.json",
            "weapon-properties.json",
            "campaign-progression.json",
            "game-mechanics.json"
        };

        foreach (var fileName in sharedFiles)
        {
            // Use the service method which handles path conversion internally
            var content = await _resourceService.GetSharedResourceAsync<object>(fileName);
            Assert.NotNull(content);
            Assert.True(content != null, $"Shared file {fileName} not found");
        }
    }

    [Fact]
    public async Task GameConfig_ShouldParseToSpecificStructure()
    {
        var config = await _resourceService.GetGameResourceAsync<GameConfigTest>("end-times", "game-config.json");

        Assert.NotNull(config);
        Assert.NotNull(config.GameName);
        Assert.NotEmpty(config.GameName);
    }

    [Fact]
    public async Task WeaponData_ShouldParseToValidStructure()
    {
        var weaponData = await _resourceService.GetGameResourceAsync<object>("28-psalms", "weapons.json");

        Assert.NotNull(weaponData);

        // Just verify we can parse it as JSON, don't assume specific structure
        var jsonString = await _resourceService.GetResourceAsStringAsync("data._28_psalms.weapons.json");
        Assert.NotNull(jsonString);
        Assert.NotEmpty(jsonString);
    }

    [Fact]
    public void ResourceExists_ShouldReturnFalseForNonExistentResource()
    {
        var exists = _resourceService.ResourceExists("data.nonexistent.file.json");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetResourceAsStringAsync_ShouldThrowForNonExistentResource()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _resourceService.GetResourceAsStringAsync("data.nonexistent.file.json"));
    }

    [Theory]
    [InlineData("end-times")]
    [InlineData("last-war")]
    public async Task Relics_ShouldLoadWithEffectField(string gameVariant)
    {
        // Arrange & Act
        var relics = await _resourceService.GetGameResourceAsync<List<RelicTest>>(gameVariant, "relics.json");

        // Assert
        Assert.NotNull(relics);
        Assert.NotEmpty(relics);

        // Every relic should have an effect
        foreach (var relic in relics)
        {
            Assert.False(string.IsNullOrEmpty(relic.Effect),
                $"Relic '{relic.Name}' in {gameVariant} is missing Effect field");
        }
    }
}

// Test DTOs for structure validation
public class GameConfigTest
{
    public string GameName { get; set; } = string.Empty;
    public CurrencyTest Currency { get; set; } = new();
}

public class CurrencyTest
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int StartingBudget { get; set; }
}

public class RelicTest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Cost { get; set; }
    public int Slots { get; set; }
    public bool Unique { get; set; }
}