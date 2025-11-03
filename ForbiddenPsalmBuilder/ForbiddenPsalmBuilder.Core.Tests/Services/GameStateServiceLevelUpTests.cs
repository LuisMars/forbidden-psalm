using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceLevelUpTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceLevelUpTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetCharacterXpCostAsync_WithoutSlowLearner_ShouldReturn5()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);
        warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        var cost = await _service.GetCharacterXpCostAsync(warbandId, character.Id);

        // Assert
        Assert.Equal(5, cost);
    }

    [Fact]
    public async Task GetCharacterXpCostAsync_WithSlowLearner_ShouldReturn6()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Add Slow Learner flaw
        await _service.AddFlawAsync(warbandId, character.Id, "Slow Learner");

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        var cost = await _service.GetCharacterXpCostAsync(warbandId, character.Id);

        // Assert
        Assert.Equal(6, cost);
    }

    [Fact]
    public async Task LevelUpIncrementStatAsync_ShouldIncreaseStatAndDeductXP()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        await _service.LevelUpIncrementStatAsync(warbandId, character.Id, "Agility");

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedChar = updatedWarband!.Members.First(c => c.Id == character.Id);
        Assert.Equal(1, updatedChar.Stats.Agility);
        Assert.Equal(5, updatedWarband.Experience); // 10 - 5 = 5
    }

    [Fact]
    public async Task LevelUpGainFeatAsync_ShouldAddFeatAndDeductXP()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        await _service.LevelUpGainFeatAsync(warbandId, character.Id, "Intimidating Presence");

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedChar = updatedWarband!.Members.First(c => c.Id == character.Id);
        Assert.Contains("Intimidating Presence", updatedChar.Feats);
        Assert.Equal(5, updatedWarband.Experience);
    }

    [Fact]
    public async Task LevelUpIncrementStatAsync_WithInsufficientXP_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 3; // Not enough XP
        await _service.UpdateWarbandAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.LevelUpIncrementStatAsync(warbandId, character.Id, "Agility"));
    }
}
