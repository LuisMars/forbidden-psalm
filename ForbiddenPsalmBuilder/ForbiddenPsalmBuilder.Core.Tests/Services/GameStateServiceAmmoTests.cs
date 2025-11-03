using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Tests.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceAmmoTests
{
    private readonly GlobalGameState _state;
    private readonly InMemoryWarbandRepository _repository;
    private readonly GameStateService _service;

    public GameStateServiceAmmoTests()
    {
        _state = new GlobalGameState();
        _repository = new InMemoryWarbandRepository();
        var resourceService = new EmbeddedResourceService();
        _service = new GameStateService(_state, _repository, resourceService);

        // Load game data synchronously for tests
        _service.LoadGameDataAsync().GetAwaiter().GetResult();
    }

    [Theory]
    [InlineData(1, 1)]  // 1 shot = 1 slot
    [InlineData(5, 1)]  // 5 shots = 1 slot
    [InlineData(6, 2)]  // 6 shots = 2 slots
    [InlineData(10, 2)] // 10 shots = 2 slots
    [InlineData(11, 3)] // 11 shots = 3 slots
    [InlineData(15, 3)] // 15 shots = 3 slots
    [InlineData(20, 4)] // 20 shots = 4 slots
    public void CalculateRequiredSlots_ForAmmo_ShouldCalculateCorrectly(int currentShots, int expectedSlots)
    {
        // Arrange
        var ammo = new Equipment
        {
            Name = "Rifle Round",
            Type = "ammo",
            CurrentShots = currentShots,
            Slots = 1
        };

        // Act
        var actualSlots = ammo.CalculateRequiredSlots();

        // Assert
        Assert.Equal(expectedSlots, actualSlots);
    }

    [Fact]
    public void CalculateRequiredSlots_ForNonAmmo_ShouldReturnStandardSlots()
    {
        // Arrange
        var weapon = new Equipment
        {
            Name = "Rifle",
            Type = "weapon",
            Slots = 2,
            CurrentShots = 100 // Shouldn't affect slot calculation for non-ammo
        };

        // Act
        var actualSlots = weapon.CalculateRequiredSlots();

        // Assert
        Assert.Equal(2, actualSlots); // Should return Slots property, not calculate from shots
    }

    [Fact]
    public async Task AddAmmoToCharacter_WithExistingAmmo_ShouldAutoMerge()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var ammo1 = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 5, MaxShots = 5, Slots = 1 };
        var ammo2 = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 3, MaxShots = 5, Slots = 1 };

        // Act
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo1);
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo2);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var ammoItems = updatedCharacter.Equipment.Where(e => e.Type == "ammo" && e.Name == "Rifle Round").ToList();

        Assert.Single(ammoItems); // Should be merged into one item
        Assert.Equal(8, ammoItems[0].CurrentShots); // 5 + 3 = 8
        Assert.Equal(2, ammoItems[0].CalculateRequiredSlots()); // 8 shots = 2 slots
    }

    [Fact]
    public async Task AddAmmoToCharacter_WithDifferentAmmoTypes_ShouldNotMerge()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var rifleRound = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 5, Slots = 1 };
        var pistolRound = new Equipment { Name = "Pistol Round", Type = "ammo", CurrentShots = 6, Slots = 1 };

        // Act
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, rifleRound);
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, pistolRound);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        Assert.Equal(2, updatedCharacter.Equipment.Count); // Should be 2 separate ammo items
    }

    [Fact]
    public async Task BuyWeapon_ShouldComeWithStartingAmmo()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) }; // 10 slots total
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act - Buy a weapon by ID (weapon should come with one load of starting ammo as separate item)
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, "bow", "weapon");

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var addedWeapon = updatedCharacter.Equipment.FirstOrDefault(e => e.Name == "Bow");
        var ammoItem = updatedCharacter.Equipment.FirstOrDefault(e => e.Type == "ammo" && e.Name == "Arrows");

        // Weapon should come LOADED with ammo
        Assert.NotNull(addedWeapon);
        Assert.Equal(5, addedWeapon.CurrentShots); // Should be loaded!
        Assert.Equal(5, addedWeapon.MaxShots);

        // Starting ammo should be created as a separate SPARE ammo item (Arrows for bow in end-times)
        Assert.NotNull(ammoItem);
        Assert.Equal(5, ammoItem.CurrentShots); // One full load of spare ammo
        Assert.Equal(1, ammoItem.CalculateRequiredSlots()); // 5 shots = 1 slot

        // Total slots used: 3 (bow with 1 magazine loaded) + 1 (spare ammo) = 4
        var usedSlots = updatedCharacter.Equipment.Sum(e => e.CalculateRequiredSlots());
        Assert.Equal(4, usedSlots);
    }

    [Fact]
    public async Task ReloadWeaponAsync_ShouldAddFullMagazineToWeapon()
    {
        // CRITICAL: Reloading adds a FULL magazine (MaxShots) to the weapon
        // Example: 4/5 + reload (5 shots) = 9/10

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var weapon = new Equipment { Name = "Rifle", Type = "weapon", AmmoType = "Rifle Round", CurrentShots = 4, MaxShots = 5, Slots = 2 };
        var ammo = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 10, MaxShots = 5, Slots = 1 };

        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, weapon);
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo);

        // Act - Reload weapon (should add full magazine of 5 shots)
        await _service.ReloadWeaponAsync(warbandId, character.Id, weapon.Id, ammo.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var reloadedWeapon = updatedCharacter.Equipment.First(e => e.Id == weapon.Id);
        var remainingAmmo = updatedCharacter.Equipment.First(e => e.Id == ammo.Id);

        // Weapon should now have 4 + 5 = 9 shots (displayed as 9/10)
        Assert.Equal(9, reloadedWeapon.CurrentShots);
        // Remaining ammo should be 10 - 5 = 5 shots
        Assert.Equal(5, remainingAmmo.CurrentShots);

        // CRITICAL: Weapon with 9 shots (2 magazines) should take 4 slots (2 base + 2 magazines)
        Assert.Equal(4, reloadedWeapon.CalculateRequiredSlots());
    }

    [Fact]
    public async Task ReloadWeaponAsync_WithInsufficientAmmo_ShouldTransferWhatAvailable()
    {
        // When reloading with less than a full magazine available, transfer what's there
        // Example: Have 3 shots of ammo → Weapon gets +3, not +5

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var weapon = new Equipment { Name = "Rifle", Type = "weapon", AmmoType = "Rifle Round", CurrentShots = 2, MaxShots = 5, Slots = 2 };
        var ammo = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 3, MaxShots = 5, Slots = 1 };

        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, weapon);
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo);

        // Act - Try to reload but only 3 shots available (less than MaxShots of 5)
        await _service.ReloadWeaponAsync(warbandId, character.Id, weapon.Id, ammo.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var reloadedWeapon = updatedCharacter.Equipment.First(e => e.Id == weapon.Id);

        // Weapon should have 2 + 3 = 5 shots
        Assert.Equal(5, reloadedWeapon.CurrentShots);
        // Ammo should be removed when depleted
        Assert.DoesNotContain(updatedCharacter.Equipment, e => e.Id == ammo.Id);
    }

    [Fact]
    public async Task ConsumeAmmoShotAsync_ShouldDecrementCurrentShots()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var weapon = new Equipment { Name = "Rifle", Type = "weapon", AmmoType = "Rifle Round", CurrentShots = 5, MaxShots = 5, Slots = 2 };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, weapon);

        // Act
        await _service.ConsumeAmmoShotAsync(warbandId, character.Id, weapon.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var updatedWeapon = updatedCharacter.Equipment.First(e => e.Id == weapon.Id);

        Assert.Equal(4, updatedWeapon.CurrentShots);
    }

    [Fact]
    public async Task ConsumeAmmoShotAsync_WhenEmpty_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var weapon = new Equipment { Name = "Rifle", Type = "weapon", AmmoType = "Rifle Round", CurrentShots = 0, MaxShots = 5, Slots = 2 };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, weapon);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ConsumeAmmoShotAsync(warbandId, character.Id, weapon.Id));
    }

    [Fact]
    public async Task BuyAmmoForWeapon_ShouldPurchaseAndAutoReload()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 100; // Set initial gold

        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Buy a weapon that needs ammo
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, "bow", "weapon");

        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = updatedWarband!.Members.First(c => c.Id == character.Id);
        var weapon = updatedCharacter.Equipment.First(e => e.Name == "Bow");

        // Act - Use "merchant" trader (standard pricing)
        await _service.BuyAmmoForWeaponAsync(warbandId, character.Id, weapon.Id, "merchant");

        // Assert
        var finalWarband = await _service.GetWarbandAsync(warbandId);
        var finalCharacter = finalWarband!.Members.First(c => c.Id == character.Id);
        var reloadedWeapon = finalCharacter.Equipment.First(e => e.Id == weapon.Id);

        // Weapon starts with 5 shots, reload adds 5 more = 10 total
        Assert.Equal(10, reloadedWeapon.CurrentShots);
        Assert.True(finalWarband.Gold < 100); // Gold should be deducted
    }

    [Fact]
    public async Task BuyAmmoForWeapon_WithInsufficientGold_ShouldThrowException()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 0; // No gold

        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, "bow", "weapon");

        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = updatedWarband!.Members.First(c => c.Id == character.Id);
        var weapon = updatedCharacter.Equipment.First(e => e.Name == "Bow");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.BuyAmmoForWeaponAsync(warbandId, character.Id, weapon.Id, "merchant"));
    }

    [Fact]
    public async Task BuyAmmoForWeapon_ShouldAutoMergeWithExistingAmmo()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 100;

        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, "bow", "weapon");

        // Add existing ammo (Arrows for bow in end-times)
        var existingAmmo = new Equipment { Name = "Arrows", Type = "ammo", CurrentShots = 3, MaxShots = 5, Slots = 1 };
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, existingAmmo);

        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = updatedWarband!.Members.First(c => c.Id == character.Id);
        var weapon = updatedCharacter.Equipment.First(e => e.Name == "Bow");

        // Act - Use "merchant" trader (standard pricing)
        await _service.BuyAmmoForWeaponAsync(warbandId, character.Id, weapon.Id, "merchant");

        // Assert
        var finalWarband = await _service.GetWarbandAsync(warbandId);
        var finalCharacter = finalWarband!.Members.First(c => c.Id == character.Id);
        var ammoItems = finalCharacter.Equipment.Where(e => e.Type == "ammo").ToList();

        Assert.Single(ammoItems); // Should be merged into one stack
    }

    [Fact]
    public async Task BuyEquipmentAsync_WithRangedWeapon_ShouldCreateStartingAmmoInStash()
    {
        // Arrange - This tests the ACTUAL buy flow used by the UI
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 100;

        // Act - Call BuyEquipmentAsync (this is what the UI actually calls!)
        await _service.BuyEquipmentAsync(warbandId, "bow", "weapon");

        // Assert
        var updatedWarband = await _service.GetWarbandAsync(warbandId);

        // Should have 2 items in stash: bow + ammo
        Assert.Equal(2, updatedWarband!.Stash.Count);

        var bow = updatedWarband.Stash.FirstOrDefault(e => e.Name == "Bow");
        var ammo = updatedWarband.Stash.FirstOrDefault(e => e.Type == "ammo");

        // CRITICAL: Bow should come LOADED with ammo when purchased
        Assert.NotNull(bow);
        Assert.Equal("weapon", bow.Type);
        Assert.Equal("Bow", bow.Name);
        Assert.Equal(5, bow.CurrentShots); // Should be LOADED (5/5), not empty!
        Assert.Equal(5, bow.MaxShots);

        // Ammo should exist with 5 shots as SPARE ammo (Arrows for bow in end-times)
        Assert.NotNull(ammo);
        Assert.Equal("ammo", ammo.Type);
        Assert.Equal("Arrows", ammo.Name);
        Assert.Equal(5, ammo.CurrentShots);

        // Verify they are different items
        Assert.NotEqual(bow.Id, ammo.Id);
    }

    [Fact]
    public async Task TransferEquipmentToCharacter_WithRangedWeapon_ShouldAlsoTransferStartingAmmo()
    {
        // Arrange - Buy weapon (creates bow + ammo in stash)
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var warband = await _service.GetWarbandAsync(warbandId);
        warband!.Gold = 100;

        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) }; // 10 slots
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Buy bow (should create bow + ammo in stash)
        await _service.BuyEquipmentAsync(warbandId, "bow", "weapon");

        var updatedWarband = await _service.GetWarbandAsync(warbandId);
        Assert.Equal(2, updatedWarband!.Stash.Count); // bow + ammo in stash

        var bowInStash = updatedWarband.Stash.First(e => e.Name == "Bow");
        var ammoInStash = updatedWarband.Stash.First(e => e.Type == "ammo");

        // Act - Transfer bow from stash to character (this is the "assign" action)
        await _service.TransferEquipmentToCharacterAsync(warbandId, character.Id, bowInStash.Id);

        // Assert
        var finalWarband = await _service.GetWarbandAsync(warbandId);
        var finalCharacter = finalWarband!.Members.First(c => c.Id == character.Id);

        // Character should have bow
        var bowOnChar = finalCharacter.Equipment.FirstOrDefault(e => e.Name == "Bow");
        Assert.NotNull(bowOnChar);

        // Ammo should STILL be in stash (not automatically transferred)
        Assert.Single(finalWarband.Stash);
        Assert.Equal("Arrows", finalWarband.Stash[0].Name); // Arrows for bow in end-times
    }

    [Fact]
    public void WeaponWithLoadedAmmo_ShouldCalculateSlots_BasePlusAllMagazines()
    {
        // CRITICAL: Weapon with 10 shots (2 magazines) should take 4 slots total:
        // - 2 slots for the bow
        // - 2 slots for ammo (10 shots / 5 per magazine = 2 magazines)

        var bow = new Equipment
        {
            Name = "Bow",
            Type = "weapon",
            Slots = 2,
            MaxShots = 5,
            CurrentShots = 10,
            AmmoType = "Arrows"
        };

        // Act
        var requiredSlots = bow.CalculateRequiredSlots();

        // Assert
        Assert.Equal(4, requiredSlots); // 2 (base) + 2 (for 2 magazines)
    }

    [Fact]
    public void Character_CanEquip_ShouldUseCalculateRequiredSlotsForAmmo()
    {
        // Arrange - Character with 10 equipment slots (5 base + 5 STR)
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) };

        // Add weapon (2 slots) and ammo with 8 shots (should take 2 slots: ceil(8/5) = 2)
        var weapon = new Equipment { Name = "Rifle", Type = "weapon", Slots = 2 };
        var ammo = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 8, Slots = 1 };

        character.Equipment.Add(weapon);
        character.Equipment.Add(ammo);

        // Currently using: 2 (weapon) + 2 (ammo) = 4 slots
        // New item needs 3 slots: total would be 7
        var newItem = new Equipment { Name = "Armor", Type = "armor", Slots = 3 };

        // Act & Assert
        Assert.True(character.CanEquip(newItem)); // 4 + 3 = 7 <= 10, should fit

        // Add more ammo to push it over the limit
        var moreAmmo = new Equipment { Name = "More Ammo", Type = "ammo", CurrentShots = 15, Slots = 1 }; // 3 slots
        character.Equipment.Add(moreAmmo);

        // Now using: 2 + 2 + 3 = 7 slots
        // New item needs 4 slots: total would be 11
        var bigItem = new Equipment { Name = "Big Item", Type = "item", Slots = 4 };

        Assert.False(character.CanEquip(bigItem)); // 7 + 4 = 11 > 10, should NOT fit
    }

    [Fact]
    public async Task MoveAmmoToStash_ShouldRemoveFromCharacterAndAddToWarband()
    {
        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Add ammo to character
        var ammo = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Ammo",
            Type = "ammo",
            CurrentShots = 10,
            MaxShots = 5,
            Slots = 1
        };
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo);

        var beforeWarband = await _service.GetWarbandAsync(warbandId);
        var beforeChar = beforeWarband!.Members.First(c => c.Id == character.Id);
        Assert.Single(beforeChar.Equipment); // Has ammo

        // Act - Move ammo to stash
        await _service.RemoveEquipmentFromCharacterAsync(warbandId, character.Id, ammo.Id);

        // Assert
        var afterWarband = await _service.GetWarbandAsync(warbandId);
        var afterChar = afterWarband!.Members.First(c => c.Id == character.Id);

        // Character should have no equipment
        Assert.Empty(afterChar.Equipment);

        // Warband stash should have the ammo
        Assert.Single(afterWarband.Stash);
        var stashedAmmo = afterWarband.Stash.First();
        Assert.Equal("Ammo", stashedAmmo.Name);
        Assert.Equal(10, stashedAmmo.CurrentShots);
    }

    [Fact]
    public async Task AddAmmoToCharacter_ShouldProperlyOccupySlots()
    {
        // Arrange - Character with 5 equipment slots (5 base + 0 STR)
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 0, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Act - Add 12 shots of ammo (should take 3 slots: ceil(12/5) = 3)
        var ammo = new Equipment { Name = "Rifle Round", Type = "ammo", CurrentShots = 12, MaxShots = 5, Slots = 1 };
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, ammo);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        // Character should have used 3 slots (calculated from 12 shots)
        var usedSlots = updatedCharacter.Equipment.Sum(e => e.CalculateRequiredSlots());
        Assert.Equal(3, usedSlots);

        // Should be able to add 2 more slots worth of items
        var twoSlotItem = new Equipment { Name = "Item", Type = "item", Slots = 2 };
        Assert.True(updatedCharacter.CanEquip(twoSlotItem));

        // Should NOT be able to add 3 more slots
        var threeSlotItem = new Equipment { Name = "Big Item", Type = "item", Slots = 3 };
        Assert.False(updatedCharacter.CanEquip(threeSlotItem));
    }

    [Fact]
    public async Task RemoveWeapon_WithLoadedAmmo_ShouldPreserveAmmoAsSeparateItem()
    {
        // CRITICAL: When selling/removing a weapon with loaded ammo, the ammo should be preserved
        // as a separate ammo item in the character's inventory, not lost

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        // Add a loaded bow with 10 shots
        var bow = new Equipment
        {
            Name = "Bow",
            Type = "weapon",
            AmmoType = "Arrows",
            CurrentShots = 10,
            MaxShots = 5,
            Slots = 2
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bow);

        // Act - Remove weapon from character (simulating sell or move to stash)
        await _service.RemoveEquipmentFromCharacterAsync(warbandId, character.Id, bow.Id);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);

        // Character should no longer have the weapon
        Assert.Empty(updatedCharacter.Equipment.Where(e => e.Type == "weapon"));

        // BUT character should have an ammo item with 10 shots
        var ammoItem = updatedCharacter.Equipment.FirstOrDefault(e => e.Type == "ammo" && e.Name == "Arrows");
        Assert.NotNull(ammoItem);
        Assert.Equal(10, ammoItem.CurrentShots);
        Assert.Equal(5, ammoItem.MaxShots);
    }

    [Fact]
    public async Task UnloadAmmoFromWeapon_ShouldTransferSpecifiedAmountToAmmoItem()
    {
        // CRITICAL: Unloading ammo should transfer specified shots from weapon to ammo item
        // Example: Bow with 10/10 shots, unload 5 → Bow has 5/10, Arrows item has 5 shots

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var bow = new Equipment
        {
            Name = "Bow",
            Type = "weapon",
            AmmoType = "Arrows",
            CurrentShots = 10,
            MaxShots = 5,
            Slots = 2
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bow);

        // Act - Unload 5 shots from the weapon
        await _service.UnloadAmmoFromWeaponAsync(warbandId, character.Id, bow.Id, 5);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var updatedBow = updatedCharacter.Equipment.First(e => e.Id == bow.Id);

        // Weapon should have 5 shots remaining
        Assert.Equal(5, updatedBow.CurrentShots);

        // Ammo item should exist with 5 shots
        var ammoItem = updatedCharacter.Equipment.FirstOrDefault(e => e.Type == "ammo" && e.Name == "Arrows");
        Assert.NotNull(ammoItem);
        Assert.Equal(5, ammoItem.CurrentShots);
    }

    [Fact]
    public async Task UnloadAmmoFromWeapon_WhenAmmoExists_ShouldStackAmmo()
    {
        // When unloading and ammo item already exists, add to existing stack

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var bow = new Equipment
        {
            Name = "Bow",
            Type = "weapon",
            AmmoType = "Arrows",
            CurrentShots = 10,
            MaxShots = 5,
            Slots = 2
        };
        var existingAmmo = new Equipment
        {
            Name = "Arrows",
            Type = "ammo",
            CurrentShots = 3,
            MaxShots = 5,
            Slots = 1
        };

        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bow);
        await _service.AddAmmoToCharacterAsync(warbandId, character.Id, existingAmmo);

        // Act - Unload 7 shots (should add to existing 3 = 10 total)
        await _service.UnloadAmmoFromWeaponAsync(warbandId, character.Id, bow.Id, 7);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var updatedBow = updatedCharacter.Equipment.First(e => e.Id == bow.Id);
        var updatedAmmo = updatedCharacter.Equipment.First(e => e.Id == existingAmmo.Id);

        // Weapon should have 3 shots remaining
        Assert.Equal(3, updatedBow.CurrentShots);

        // Ammo should have 3 + 7 = 10 shots
        Assert.Equal(10, updatedAmmo.CurrentShots);

        // Should still be only ONE ammo item (stacked)
        Assert.Single(updatedCharacter.Equipment.Where(e => e.Type == "ammo" && e.Name == "Arrows"));
    }

    [Fact]
    public async Task UnloadAmmoFromWeapon_UnloadAll_ShouldEmptyWeapon()
    {
        // Unloading all ammo should leave weapon at 0 shots

        // Arrange
        var warbandId = await _service.CreateWarbandAsync("Test Warband", "end-times");
        var character = new Character("Test") { Stats = new Stats(0, 0, 5, 0) };
        await _service.AddCharacterToWarbandAsync(warbandId, character);

        var bow = new Equipment
        {
            Name = "Bow",
            Type = "weapon",
            AmmoType = "Arrows",
            CurrentShots = 10,
            MaxShots = 5,
            Slots = 2
        };
        await _service.AddEquipmentToCharacterAsync(warbandId, character.Id, bow);

        // Act - Unload all 10 shots
        await _service.UnloadAmmoFromWeaponAsync(warbandId, character.Id, bow.Id, 10);

        // Assert
        var warband = await _service.GetWarbandAsync(warbandId);
        var updatedCharacter = warband!.Members.First(c => c.Id == character.Id);
        var updatedBow = updatedCharacter.Equipment.First(e => e.Id == bow.Id);

        // Weapon should be empty
        Assert.Equal(0, updatedBow.CurrentShots);

        // Ammo item should have all 10 shots
        var ammoItem = updatedCharacter.Equipment.FirstOrDefault(e => e.Type == "ammo" && e.Name == "Arrows");
        Assert.NotNull(ammoItem);
        Assert.Equal(10, ammoItem.CurrentShots);
    }
}
