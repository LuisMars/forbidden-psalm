using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceSpellEquipmentTests
{
    private readonly GameStateService _service;
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;

    public GameStateServiceSpellEquipmentTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddSpellAsync_ShouldAddSpellAsEquipment()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);

        // Should be in spells list
        Assert.Contains("Shadow Bind", updatedCharacter.Spells);

        // Should also be in equipment list
        Assert.Contains(updatedCharacter.Equipment, e => e.Name == "Shadow Bind" && e.Type == "spell");
    }

    [Fact]
    public async Task AddSpellAsync_ShouldTakeOneEquipmentSlot()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);
        var spellEquipment = updatedCharacter.Equipment.First(e => e.Name == "Shadow Bind");

        Assert.Equal(1, spellEquipment.Slots);
    }

    [Fact]
    public async Task RemoveSpellAsync_ShouldRemoveFromEquipment()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Act
        await _service.RemoveSpellAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);

        // Should be removed from spells list
        Assert.DoesNotContain("Shadow Bind", updatedCharacter.Spells);

        // Should be removed from equipment list
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Name == "Shadow Bind");
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenDead_ShouldMoveSpellsToStash()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");
        await _service.AddSpellAsync(warbandId, characterId, "Blood Boil");

        var warband = await _service.GetWarbandAsync(warbandId);
        var initialStashCount = warband!.Stash.Count;

        // Act - Character dies
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: true);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);

        // Spells should be in stash as equipment
        Assert.Equal(initialStashCount + 2, updatedWarband!.Stash.Count);
        Assert.Contains(updatedWarband.Stash, e => e.Name == "Shadow Bind" && e.Type == "spell");
        Assert.Contains(updatedWarband.Stash, e => e.Name == "Blood Boil" && e.Type == "spell");
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WhenFired_ShouldLoseSpells()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        var warband = await _service.GetWarbandAsync(warbandId);
        var initialStashCount = warband!.Stash.Count;

        // Act - Character fired
        await _service.RemoveCharacterFromWarbandAsync(warbandId, characterId, isDead: false);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);

        // Spells should NOT be in stash (lost)
        Assert.Equal(initialStashCount, updatedWarband!.Stash.Count);
        Assert.DoesNotContain(updatedWarband.Stash, e => e.Name == "Shadow Bind");
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldAllowSpellEquipmentFromStash()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);

        // Add spell to stash
        var spellEquipment = new Equipment
        {
            Name = "Shadow Bind",
            Type = "spell",
            Slots = 1,
            Effect = "Target cannot move for D3 rounds"
        };
        warband!.Stash.Add(spellEquipment);
        await _repository.SaveAsync(warband);

        // Create character
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act - Add spell from stash to character
        await _service.AddEquipmentToCharacterAsync(warbandId, characterId, spellEquipment.Id, "stash");

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = updatedWarband!.Members.First(c => c.Id == characterId);

        // Should be in character equipment
        Assert.Contains(updatedCharacter.Equipment, e => e.Name == "Shadow Bind");

        // Should be in character spells list
        Assert.Contains("Shadow Bind", updatedCharacter.Spells);

        // Should be removed from stash
        Assert.DoesNotContain(updatedWarband.Stash, e => e.Id == spellEquipment.Id);
    }

    [Fact]
    public async Task RemoveEquipmentFromCharacterAsync_SpellEquipment_ShouldRemoveFromSpellsList()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        var warband = await _service.GetWarbandAsync(warbandId);
        var spellEquipment = warband!.Members.First(c => c.Id == characterId)
            .Equipment.First(e => e.Name == "Shadow Bind");

        // Act - Remove spell equipment
        await _service.RemoveEquipmentFromCharacterAsync(warbandId, characterId, spellEquipment.Id);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = updatedWarband!.Members.First(c => c.Id == characterId);

        // Should be removed from equipment
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Name == "Shadow Bind");

        // Should be removed from spells list
        Assert.DoesNotContain("Shadow Bind", updatedCharacter.Spells);

        // Should be back in stash
        Assert.Contains(updatedWarband.Stash, e => e.Name == "Shadow Bind" && e.Type == "spell");
    }

    [Fact]
    public async Task RemoveSpellAsync_ShouldMoveSpellToStash()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Act
        await _service.RemoveSpellAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);

        // Should be removed from spells list
        Assert.DoesNotContain("Shadow Bind", updatedCharacter.Spells);

        // Should be removed from equipment
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Name == "Shadow Bind" && e.Type == "spell");

        // Should be in stash
        Assert.Contains(warband.Stash, e => e.Name == "Shadow Bind" && e.Type == "spell");
    }

    [Fact]
    public async Task DeleteSpellPermanentlyAsync_ShouldDeleteSpellCompletely()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0)
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Act
        await _service.DeleteSpellPermanentlyAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);

        // Should be removed from spells list
        Assert.DoesNotContain("Shadow Bind", updatedCharacter.Spells);

        // Should be removed from equipment
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Name == "Shadow Bind" && e.Type == "spell");

        // Should NOT be in stash
        Assert.DoesNotContain(warband.Stash, e => e.Name == "Shadow Bind");
    }

    [Fact]
    public async Task AddSpellAsync_ShouldThrowWhenNotEnoughEquipmentSlots()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0) // Has 10 equipment slots
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Fill all equipment slots
        for (int i = 0; i < 10; i++)
        {
            var equipment = new Equipment
            {
                Name = $"Item {i}",
                Type = "item",
                Slots = 1,
                Cost = 0
            };
            character.Equipment.Add(equipment);
        }
        await _repository.SaveAsync(await _service.GetWarbandAsync(warbandId) ?? throw new InvalidOperationException());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind")
        );

        Assert.Contains("not enough equipment slots", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSpellAsync_ShouldAllowDuplicateSpells()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Character")
        {
            Stats = new Stats(0, 0, 0, 0) // Has 10 equipment slots
        };
        var characterId = await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act - Add the same spell twice
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");
        await _service.AddSpellAsync(warbandId, characterId, "Shadow Bind");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == characterId);

        // Should have the spell twice in the spells list
        Assert.Equal(2, updatedCharacter.Spells.Count(s => s == "Shadow Bind"));

        // Should have two spell equipment items
        Assert.Equal(2, updatedCharacter.Equipment.Count(e => e.Name == "Shadow Bind" && e.Type == "spell"));

        // Should take 2 equipment slots total
        Assert.Equal(2, updatedCharacter.Equipment.Sum(e => e.Slots));
    }
}
