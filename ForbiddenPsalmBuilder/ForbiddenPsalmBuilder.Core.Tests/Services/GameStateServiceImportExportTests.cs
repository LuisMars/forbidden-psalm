using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceImportExportTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceImportExportTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ExportWarbandAsJsonAsync_ShouldReturnValidJson()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Export Test", "end-times");
        var character = new Character("Test Hero") { Stats = new Stats(5, 4, 6, 5) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act
        var json = await _service.ExportWarbandAsJsonAsync(warbandId);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("Export Test", json);
        Assert.Contains("Test Hero", json);
        Assert.Contains("end-times", json);
    }

    [Fact]
    public async Task ExportWarbandAsJsonAsync_WhenWarbandNotFound_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ExportWarbandAsJsonAsync("nonexistent-id")
        );
    }

    [Fact]
    public async Task ImportWarbandFromJsonAsync_ShouldCreateNewWarband()
    {
        // Arrange
        var originalWarbandId = await _service.CreateWarbandAsync("Original Warband", "28-psalms");
        var character = new Character("Cowboy") { Stats = new Stats(6, 3, 5, 4) };
        await _service.AddCharacterToWarbandAsync(originalWarbandId, character);

        var json = await _service.ExportWarbandAsJsonAsync(originalWarbandId);

        // Clear state to simulate fresh import
        await _service.DeleteWarbandAsync(originalWarbandId);

        // Act
        var importedWarbandId = await _service.ImportWarbandFromJsonAsync(json);

        // Assert
        var importedWarband = await _service.GetWarbandAsync(importedWarbandId);
        Assert.NotNull(importedWarband);
        Assert.Equal("Original Warband", importedWarband.Name);
        Assert.Equal("28-psalms", importedWarband.GameVariant);
        Assert.Single(importedWarband.Members);
        Assert.Equal("Cowboy", importedWarband.Members[0].Name);
    }

    [Fact]
    public async Task ImportWarbandFromJsonAsync_WhenIdExists_ShouldReplaceWarband()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Old Warband", "end-times");
        var oldCharacter = new Character("Old Hero") { Stats = new Stats(5, 5, 5, 5) };
        await _service.AddCharacterToWarbandAsync(warbandId, oldCharacter);

        // Create a modified version to import
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Name = "Updated Warband";
        warband.Members.Clear();
        warband.Members.Add(new Character("New Hero") { Stats = new Stats(6, 6, 6, 6) });

        // Serialize with camelCase (same as export)
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        var json = System.Text.Json.JsonSerializer.Serialize(warband, options);

        // Act
        var importedWarbandId = await _service.ImportWarbandFromJsonAsync(json);

        // Assert
        Assert.Equal(warbandId, importedWarbandId); // Same ID
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.NotNull(updatedWarband);
        Assert.Equal("Updated Warband", updatedWarband.Name);
        Assert.Single(updatedWarband.Members);
        Assert.Equal("New Hero", updatedWarband.Members[0].Name);
    }

    [Fact]
    public async Task ImportWarbandFromJsonAsync_WhenInvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act & Assert
        // Note: Validation catches invalid JSON and throws InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ImportWarbandFromJsonAsync(invalidJson)
        );
    }

    [Fact]
    public async Task ImportWarbandFromJsonAsync_WhenMissingRequiredFields_ShouldThrowException()
    {
        // Arrange
        var incompleteJson = "{\"Name\":\"Test\"}"; // Missing Id, GameVariant, etc.

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ImportWarbandFromJsonAsync(incompleteJson)
        );
    }

    [Fact]
    public async Task ValidateWarbandJsonAsync_WhenValid_ShouldReturnTrue()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Valid Warband", "last-war");
        var json = await _service.ExportWarbandAsJsonAsync(warbandId);

        // Act
        var isValid = await _service.ValidateWarbandJsonAsync(json);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateWarbandJsonAsync_WhenInvalid_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = "{ not valid }";

        // Act
        var isValid = await _service.ValidateWarbandJsonAsync(invalidJson);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ExportImport_ShouldPreserveAllWarbandData()
    {
        // Arrange - Create warband with comprehensive data
        var warbandId = await _service.CreateWarbandAsync("Complete Warband", "end-times");

        var character = new Character("Veteran")
        {
            Stats = new Stats(7, 6, 8, 7),
            CurrentHP = 5,
            CurrentGold = 25
        };
        character.Feats.Add("Tough");
        character.Flaws.Add("Slow Learner");
        character.Injuries.Add("Broken Leg");

        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 150;
        warband.Experience = 12;
        await _repository.SaveAsync(warband);

        // Act - Export and re-import
        var json = await _service.ExportWarbandAsJsonAsync(warbandId);
        await _service.DeleteWarbandAsync(warbandId);
        var newWarbandId = await _service.ImportWarbandFromJsonAsync(json);

        // Assert - Verify all data preserved
        var importedWarband = await _service.GetWarbandAsync(newWarbandId);
        Assert.NotNull(importedWarband);
        Assert.Equal("Complete Warband", importedWarband.Name);
        Assert.Equal(150, importedWarband.Gold);
        Assert.Equal(12, importedWarband.Experience);
        Assert.Single(importedWarband.Members);

        var importedChar = importedWarband.Members[0];
        Assert.Equal("Veteran", importedChar.Name);
        Assert.Equal(7, importedChar.Stats.Agility);
        Assert.Equal(6, importedChar.Stats.Presence);
        Assert.Equal(8, importedChar.Stats.Strength);
        Assert.Equal(7, importedChar.Stats.Toughness);
        Assert.Equal(5, importedChar.CurrentHP);
        Assert.Equal(25, importedChar.CurrentGold);
        Assert.Contains("Tough", importedChar.Feats);
        Assert.Contains("Slow Learner", importedChar.Flaws);
        Assert.Contains("Broken Leg", importedChar.Injuries);
    }
}
