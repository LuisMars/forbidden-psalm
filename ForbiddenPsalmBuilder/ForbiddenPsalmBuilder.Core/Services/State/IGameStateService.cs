using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Models.Warband;

namespace ForbiddenPsalmBuilder.Core.Services.State;

public interface IGameStateService
{
    // State access
    GlobalGameState State { get; }

    // Game variant management
    Task SetGameVariantAsync(string variant);
    Task<List<string>> GetAvailableGameVariantsAsync();

    // Warband management
    Task<string> CreateWarbandAsync(string name);
    Task<string> CreateWarbandAsync(string name, string gameVariant);
    Task DeleteWarbandAsync(string warbandId);
    Task SetActiveWarbandAsync(string? warbandId);
    Task<List<Warband>> GetWarbandsAsync();
    Task<List<Warband>> GetWarbandsForGameAsync(string gameVariant);
    Task<Warband?> GetWarbandAsync(string warbandId);
    Task UpdateWarbandAsync(Warband warband);
    Task<string> GenerateWarbandNameAsync();
    Task<string> GenerateCharacterNameAsync(string gameVariant);

    // Character management
    Task<string> AddCharacterToWarbandAsync(string warbandId, Character character);
    Task UpdateCharacterAsync(string warbandId, string characterId, Character character);
    Task RemoveCharacterFromWarbandAsync(string warbandId, string characterId, bool isDead = true);

    // Character builder state
    Task StartCharacterBuilderAsync(string warbandId);
    Task SetCharacterBeingBuiltAsync(Character character);
    Task FinishCharacterBuilderAsync();
    Task CancelCharacterBuilderAsync();

    // Data loading
    Task LoadGameDataAsync();
    Task RefreshGameDataAsync();
    Task<List<ForbiddenPsalmBuilder.Core.Models.Character.StatArray>> GetStatArraysAsync();

    // Special Classes
    Task<List<SpecialClass>> GetSpecialClassesAsync(string gameVariant);
    Task<SpecialClass?> GetSpecialClassByIdAsync(string specialClassId, string gameVariant);
    Task<bool> CanAddSpecialClassAsync(string warbandId, string specialClassId);
    Task<string?> ValidateSpecialClassSelectionAsync(string warbandId, string? specialClassId, string? characterIdToExclude = null);

    // Equipment Management
    Task AddEquipmentToCharacterAsync(string warbandId, string characterId, string equipmentId, string equipmentType, bool equipImmediately = false);
    Task AddEquipmentToCharacterAsync(string warbandId, string characterId, Equipment equipment);
    Task RemoveEquipmentFromCharacterAsync(string warbandId, string characterId, string equipmentId);
    Task EquipItemAsync(string warbandId, string characterId, string equipmentId);
    Task UnequipItemAsync(string warbandId, string characterId, string equipmentId);
    bool CanCharacterEquip(string warbandId, string characterId, Equipment equipment);
    Task<List<Equipment>> GetAvailableEquipmentForCharacterAsync(string warbandId, string characterId);

    // Equipment Economy & Transfers
    Task BuyEquipmentAsync(string warbandId, string equipmentId, string equipmentType, string? traderId = null);
    Task BuyRelicAsync(string warbandId, string relicId);
    Task SellEquipmentAsync(string warbandId, string equipmentId, string? traderId = null);
    Task TransferEquipmentToCharacterAsync(string warbandId, string characterId, string equipmentId, bool equipImmediately = false);
    Task TransferEquipmentToStashAsync(string warbandId, string characterId, string equipmentId);
    Task MarkRelicAsAcquiredAsync(string warbandId, string relicId);
    Task MarkRelicAsAvailableAsync(string warbandId, string relicId);

    // Feat & Flaw Management
    Task<string> RollFeatAsync(string warbandId, string characterId);
    Task<string> RollFlawAsync(string warbandId, string characterId);
    Task AddFeatAsync(string warbandId, string characterId, string featName);
    Task AddFlawAsync(string warbandId, string characterId, string flawName);
    Task RemoveFeatAsync(string warbandId, string characterId, string featName);
    Task RemoveFlawAsync(string warbandId, string characterId, string flawName);

    // Injury Management
    Task<string> RollInjuryAsync(string warbandId, string characterId);
    Task AddInjuryAsync(string warbandId, string characterId, string injuryName);
    Task RemoveInjuryAsync(string warbandId, string characterId, string injuryName);

    // Spell Management
    Task AddSpellAsync(string warbandId, string characterId, string spellName);
    Task RemoveSpellAsync(string warbandId, string characterId, string spellName);
    Task DeleteSpellPermanentlyAsync(string warbandId, string characterId, string spellName);

    // Level-Up Management (Character)
    Task<int> GetCharacterXpCostAsync(string warbandId, string characterId);
    Task<bool> CanCharacterLevelUpAsync(string warbandId, string characterId);
    Task LevelUpIncrementStatAsync(string warbandId, string characterId, string statName);
    Task<string> LevelUpRerollFlawAsync(string warbandId, string characterId, string oldFlawName);
    Task LevelUpGainFeatAsync(string warbandId, string characterId, string featName);
    Task LevelUpRemoveInjuryAsync(string warbandId, string characterId, string injuryName);

    // Warband XP Spending
    Task ResurrectMemberAsync(string warbandId, string deadMemberId);

    // Ammo Management
    Task AddAmmoToCharacterAsync(string warbandId, string characterId, Equipment ammo);
    Task ReloadWeaponAsync(string warbandId, string characterId, string weaponId, string ammoId);
    Task ConsumeAmmoShotAsync(string warbandId, string characterId, string weaponId);
    Task BuyAmmoForWeaponAsync(string warbandId, string characterId, string weaponId, string traderId);
    Task UnloadAmmoFromWeaponAsync(string warbandId, string characterId, string weaponId, int shotsToUnload);

    // Consumable Management
    Task UseConsumableAsync(string warbandId, string characterId, string equipmentId);

    // Custom Items Management
    Task CreateCustomItemAsync(string warbandId, Equipment customItem);
    Task<List<Equipment>> GetCustomItemsAsync(string warbandId);
    Task DeleteCustomItemAsync(string warbandId, string itemId);

    // Import/Export Management
    Task<string> ExportWarbandAsJsonAsync(string warbandId);
    Task<string> ImportWarbandFromJsonAsync(string json);
    Task<bool> ValidateWarbandJsonAsync(string json);

    // State persistence
    Task SaveStateAsync();
    Task LoadStateAsync();
    Task ClearStateAsync();

    // Error handling
    Task SetErrorAsync(string? error);
    Task ClearErrorAsync();

    // Events
    event Action? StateChanged;
    event Action<string>? WarbandChanged;
    event Action<string>? GameVariantChanged;
    event Action<string?>? ActiveWarbandChanged;
}