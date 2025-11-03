using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceConsumableTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceConsumableTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UseConsumableAsync_WithCureBleedingEffect_ShouldRemoveBleedingInjury()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Add Bleeding injury
        character.Injuries.Add("Bleeding");

        var bandage = new Equipment
        {
            Name = "Bandage",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "cure_bleeding",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bandage);

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, bandage.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        Assert.DoesNotContain("Bleeding", updatedCharacter.Injuries); // Injury removed
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == bandage.Id); // Consumable removed
    }

    [Fact]
    public async Task UseConsumableAsync_WithCurePoisonedEffect_ShouldRemovePoisonedInjury()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        character.Injuries.Add("Poisoned");

        var antidote = new Equipment
        {
            Name = "Antidote",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "cure_poisoned",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, antidote);

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, antidote.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        Assert.DoesNotContain("Poisoned", updatedCharacter.Injuries);
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == antidote.Id);
    }

    [Fact]
    public async Task UseConsumableAsync_WithCureDiseaseEffect_ShouldRemoveDiseaseInjury()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "last-war");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        character.Injuries.Add("Disease");

        var medicine = new Equipment
        {
            Name = "Lead & Opium",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "cure_disease",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, medicine);

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, medicine.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        Assert.DoesNotContain("Disease", updatedCharacter.Injuries);
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == medicine.Id);
    }

    [Fact]
    public async Task UseConsumableAsync_WithHealHpEffect_ShouldIncreaseHP()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Damage the character (reduce HP from max)
        character.CurrentHP = character.Stats.HP - 2; // 2 HP below max

        var potion = new Equipment
        {
            Name = "Potion",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "heal_hp",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, potion);

        var initialHP = character.CurrentHP;

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, potion.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        Assert.True(updatedCharacter.CurrentHP > initialHP); // HP increased
        Assert.True(updatedCharacter.CurrentHP <= updatedCharacter.Stats.HP); // Not above max
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == potion.Id);
    }

    [Fact]
    public async Task UseConsumableAsync_WithNonConsumableItem_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var nonConsumable = new Equipment
        {
            Name = "Sword",
            Type = "weapon",
            IsConsumable = false,
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, nonConsumable);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UseConsumableAsync(warbandId, character.Id, nonConsumable.Id));
    }

    [Fact]
    public async Task UseConsumableAsync_WhenInjuryDoesNotExist_ShouldStillRemoveConsumable()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // No injuries added, but use bandage anyway

        var bandage = new Equipment
        {
            Name = "Bandage",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "cure_bleeding",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bandage);

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, bandage.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        // Consumable should still be removed even if effect didn't apply
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == bandage.Id);
    }

    [Fact]
    public async Task UseConsumableAsync_WithMultipleSameInjuries_ShouldRemoveOne()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(3, 3, 3, 3) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Add multiple Bleeding injuries (if game allows this)
        character.Injuries.Add("Bleeding");
        character.Injuries.Add("Bleeding");

        var bandage = new Equipment
        {
            Name = "Bandage",
            Type = "item",
            IsConsumable = true,
            ConsumableEffect = "cure_bleeding",
            Slots = 1
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bandage);

        // Act
        await _service.UseConsumableAsync(warbandId, character.Id, bandage.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        // Should remove only one instance
        Assert.Single(updatedCharacter.Injuries.Where(i => i == "Bleeding"));
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == bandage.Id);
    }
}
