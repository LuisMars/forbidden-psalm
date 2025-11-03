using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;

namespace ForbiddenPsalmBuilder.Core.Tests;

public class GameStateServiceTests
{
    private readonly GlobalGameState _globalGameState;
    private readonly IWarbandRepository _mockWarbandRepository;
    private readonly GameStateService _gameStateService;

    public GameStateServiceTests()
    {
        _globalGameState = new GlobalGameState();
        _mockWarbandRepository = new InMemoryWarbandRepository();
        _gameStateService = new GameStateService(_globalGameState, _mockWarbandRepository);
    }

    [Fact]
    public async Task SetGameVariantAsync_ShouldThrowException_WhenVariantNotInGameConfigs()
    {
        // Arrange
        var variantId = "28-psalms";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameStateService.SetGameVariantAsync(variantId));

        Assert.Equal($"Unknown game variant: {variantId}", exception.Message);
    }

    [Fact]
    public async Task SetGameVariantAsync_ShouldChangeVariant_WhenVariantExistsInGameConfigs()
    {
        // Arrange
        var variantId = "28-psalms";
        await _gameStateService.LoadGameDataAsync(); // This should populate GameConfigs

        // Act
        await _gameStateService.SetGameVariantAsync(variantId);

        // Assert
        Assert.Equal(variantId, _globalGameState.SelectedGameVariant);
    }

    [Fact]
    public async Task UpdateWarbandAsync_ShouldUpdateWarbandName_WhenGivenValidWarband()
    {
        // Arrange
        await _gameStateService.LoadGameDataAsync();
        var originalName = "Test Warband";
        var updatedName = "Updated Warband Name";

        // Create a warband first
        var warbandId = await _gameStateService.CreateWarbandAsync(originalName, "28-psalms");
        var warband = await _gameStateService.GetWarbandAsync(warbandId);
        Assert.NotNull(warband);

        // Modify the warband
        warband.Name = updatedName;

        // Act
        await _gameStateService.UpdateWarbandAsync(warband);

        // Assert
        var updatedWarband = await _gameStateService.GetWarbandAsync(warbandId);
        Assert.NotNull(updatedWarband);
        Assert.Equal(updatedName, updatedWarband.Name);
        Assert.NotEqual(warband.Created, updatedWarband.LastModified);
    }

    [Fact]
    public async Task UpdateWarbandAsync_ShouldThrowException_WhenWarbandDoesNotExist()
    {
        // Arrange
        await _gameStateService.LoadGameDataAsync();
        var nonExistentWarband = new Warband("Test", "28-psalms");
        nonExistentWarband.Id = "non-existent-id";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameStateService.UpdateWarbandAsync(nonExistentWarband));

        Assert.Contains("Warband not found", exception.Message);
    }
}