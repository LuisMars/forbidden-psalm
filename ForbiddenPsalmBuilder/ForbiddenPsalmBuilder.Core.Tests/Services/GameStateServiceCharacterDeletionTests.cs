using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceCharacterDeletionTests
{
    private readonly GameStateService _service;
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;

    public GameStateServiceCharacterDeletionTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenDead_ShouldMoveEquipmentToStash()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);

        // Create character with equipment
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };

        var equipment1 = new Equipment { Id = "eq1", Name = "Sword", Slots = 1 };
        var equipment2 = new Equipment { Id = "eq2", Name = "Shield", Slots = 1 };
        character.Equipment.Add(equipment1);
        character.Equipment.Add(equipment2);

        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        var initialStashCount = warband!.Stash.Count;

        // Act - Remove character as "dead"
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: true);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);

        // Character should be removed
        Assert.DoesNotContain(updatedWarband!.Members, c => c.Id == characterId);

        // Equipment should be in stash
        Assert.Equal(initialStashCount + 2, updatedWarband.Stash.Count);
        Assert.Contains(updatedWarband.Stash, e => e.Name == "Sword");
        Assert.Contains(updatedWarband.Stash, e => e.Name == "Shield");
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenFired_ShouldLoseEquipment()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);

        // Create character with equipment
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };

        var equipment1 = new Equipment { Id = "eq1", Name = "Sword", Slots = 1 };
        var equipment2 = new Equipment { Id = "eq2", Name = "Shield", Slots = 1 };
        character.Equipment.Add(equipment1);
        character.Equipment.Add(equipment2);

        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        var initialStashCount = warband!.Stash.Count;

        // Act - Remove character as "fired"
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: false);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);

        // Character should be removed
        Assert.DoesNotContain(updatedWarband!.Members, c => c.Id == characterId);

        // Equipment should NOT be in stash (lost)
        Assert.Equal(initialStashCount, updatedWarband.Stash.Count);
        Assert.DoesNotContain(updatedWarband.Stash, e => e.Name == "Sword");
        Assert.DoesNotContain(updatedWarband.Stash, e => e.Name == "Shield");
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenDead_ShouldNotRefundSpecialClassCost()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        // Create character with special class
        var character = new Character("Test Spellcaster")
        {
            SpecialClassId = "spellcaster",
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warbandBeforeDelete = await _service.GetWarbandAsync(warbandId);
        var goldBeforeDelete = warbandBeforeDelete!.Gold;

        // Act - Remove character (dead)
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: true);

        // Assert - Should NOT refund the special class cost (only equipment goes to stash)
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Equal(goldBeforeDelete, updatedWarband!.Gold); // No change
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenFired_ShouldNotRefundSpecialClassCost()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        // Create character with special class
        var character = new Character("Test Spellcaster")
        {
            SpecialClassId = "spellcaster",
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        var warbandBeforeDelete = await _service.GetWarbandAsync(warbandId);
        var goldBeforeDelete = warbandBeforeDelete!.Gold;

        // Act - Remove character (fired)
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: false);

        // Assert - Should NOT refund the special class cost
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Equal(goldBeforeDelete, updatedWarband!.Gold); // No change
    }
}
