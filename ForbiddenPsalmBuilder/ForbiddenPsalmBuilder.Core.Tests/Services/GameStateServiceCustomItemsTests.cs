using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceCustomItemsTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceCustomItemsTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateCustomItemAsync_ShouldAddToWarbandCustomItems()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        var customItem = new Equipment
        {
            Name = "Magic Potion",
            Type = "item",
            Cost = 25,
            Slots = 1,
            Effect = "Restores 5 HP",
            IconClass = "fas fa-flask",
            IsCustom = true
        };

        // Act
        await _service.CreateCustomItemAsync(warbandId, customItem);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        Assert.NotNull(warband);
        Assert.Single(warband.CustomItems);
        Assert.Equal("Magic Potion", warband.CustomItems[0].Name);
        Assert.Equal(25, warband.CustomItems[0].Cost);
        Assert.True(warband.CustomItems[0].IsCustom);
    }

    [Fact]
    public async Task GetCustomItemsAsync_ShouldReturnWarbandCustomItems()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        var item1 = new Equipment { Name = "Item 1", Type = "item", Cost = 10, Slots = 1, IsCustom = true };
        var item2 = new Equipment { Name = "Item 2", Type = "item", Cost = 20, Slots = 2, IsCustom = true };

        await _service.CreateCustomItemAsync(warbandId, item1);
        await _service.CreateCustomItemAsync(warbandId, item2);

        // Act
        var customItems = await _service.GetCustomItemsAsync(warbandId);

        // Assert
        Assert.NotNull(customItems);
        Assert.Equal(2, customItems.Count);
        Assert.Contains(customItems, i => i.Name == "Item 1");
        Assert.Contains(customItems, i => i.Name == "Item 2");
    }

    [Fact]
    public async Task DeleteCustomItemAsync_WhenNotInUse_ShouldRemove()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        var customItem = new Equipment
        {
            Name = "Throwaway Item",
            Type = "item",
            Cost = 5,
            Slots = 1,
            IsCustom = true
        };

        await _service.CreateCustomItemAsync(warbandId, customItem);
        var warband = await _service.GetWarbandAsync(warbandId);
        var itemId = warband!.CustomItems[0].Id;

        // Act
        await _service.DeleteCustomItemAsync(warbandId, itemId);

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Empty(updatedWarband!.CustomItems);
    }

    [Fact]
    public async Task DeleteCustomItemAsync_WhenInCharacterInventory_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test Hero") { Stats = new Stats(0, 0, 5, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var customItem = new Equipment
        {
            Name = "Equipped Item",
            Type = "item",
            Cost = 50,
            Slots = 1,
            IsCustom = true
        };

        await _service.CreateCustomItemAsync(warbandId, customItem);
        var warband = await _service.GetWarbandAsync(warbandId);
        var itemId = warband!.CustomItems[0].Id;

        // Add item to character
        var itemCopy = new Equipment
        {
            Id = itemId,
            Name = customItem.Name,
            Type = customItem.Type,
            Cost = customItem.Cost,
            Slots = customItem.Slots,
            IsCustom = true
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, itemCopy);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DeleteCustomItemAsync(warbandId, itemId)
        );
    }

    [Fact]
    public async Task DeleteCustomItemAsync_WhenInStash_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        var customItem = new Equipment
        {
            Name = "Stashed Item",
            Type = "item",
            Cost = 30,
            Slots = 1,
            IsCustom = true
        };

        await _service.CreateCustomItemAsync(warbandId, customItem);
        var warband = await _service.GetWarbandAsync(warbandId);
        var itemId = warband!.CustomItems[0].Id;

        // Add item to stash
        var itemCopy = new Equipment
        {
            Id = itemId,
            Name = customItem.Name,
            Type = customItem.Type,
            Cost = customItem.Cost,
            Slots = customItem.Slots,
            IsCustom = true
        };
        warband.Stash.Add(itemCopy);
        await _repository.SaveAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DeleteCustomItemAsync(warbandId, itemId)
        );
    }

    [Fact]
    public async Task CustomItems_ShouldPersistAcrossWarbandLoadSave()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");

        var customItem = new Equipment
        {
            Name = "Persistent Item",
            Type = "item",
            Cost = 15,
            Slots = 1,
            Effect = "Does something cool",
            IconClass = "fas fa-star",
            IsCustom = true
        };

        await _service.CreateCustomItemAsync(warbandId, customItem);

        // Act - Reload warband
        var reloadedWarband = await _service.GetWarbandAsync(warbandId);

        // Assert
        Assert.NotNull(reloadedWarband);
        Assert.Single(reloadedWarband.CustomItems);
        Assert.Equal("Persistent Item", reloadedWarband.CustomItems[0].Name);
        Assert.Equal(15, reloadedWarband.CustomItems[0].Cost);
        Assert.True(reloadedWarband.CustomItems[0].IsCustom);
    }
}
