using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceWarbandXpTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceWarbandXpTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ResurrectMemberAsync_ShouldMoveFromDeadToMembers()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Dead Hero") { Stats = new Stats(3, 3, 3, 3) };

        // Add character to warband
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Kill the character (move to dead members)
        await _service.RemoveCharacterFromWarbandAsync(warbandId, character.Id, isDead: true);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        await _service.ResurrectMemberAsync(warbandId, character.Id);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Empty(updatedWarband!.DeadMembers);
        Assert.Single(updatedWarband.Members);
        Assert.Equal(character.Id, updatedWarband.Members[0].Id);
    }

    [Fact]
    public async Task ResurrectMemberAsync_ShouldAddNewFlaw()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Dead Hero") { Stats = new Stats(3, 3, 3, 3) };

        await _service.AddCharacterToWarbandAsync(warbandId, character);
        var initialFlawCount = character.Flaws.Count;

        await _service.RemoveCharacterFromWarbandAsync(warbandId, character.Id, isDead: true);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        await _service.ResurrectMemberAsync(warbandId, character.Id);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var resurrectedChar = updatedWarband!.Members.First(c => c.Id == character.Id);
        Assert.Equal(initialFlawCount + 1, resurrectedChar.Flaws.Count);
    }

    [Fact]
    public async Task ResurrectMemberAsync_ShouldDeductXP()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Dead Hero") { Stats = new Stats(3, 3, 3, 3) };

        await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.RemoveCharacterFromWarbandAsync(warbandId, character.Id, isDead: true);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act
        await _service.ResurrectMemberAsync(warbandId, character.Id);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Equal(5, updatedWarband!.Experience); // 10 - 5 = 5
    }

    [Fact]
    public async Task ResurrectMemberAsync_WithInsufficientXP_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Dead Hero") { Stats = new Stats(3, 3, 3, 3) };

        await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.RemoveCharacterFromWarbandAsync(warbandId, character.Id, isDead: true);

        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 3; // Not enough XP
        await _service.UpdateWarbandAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ResurrectMemberAsync(warbandId, character.Id));
    }

    [Fact]
    public async Task ResurrectMemberAsync_WithNonExistentDeadMember_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Experience = 10;
        await _service.UpdateWarbandAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ResurrectMemberAsync(warbandId, "non-existent-id"));
    }
}
