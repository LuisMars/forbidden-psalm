using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Models.NameGeneration;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Services.State;

public class GameStateService : IGameStateService
{
    private readonly GlobalGameState _state;
    private readonly IWarbandRepository _warbandRepository;
    private readonly IEmbeddedResourceService _resourceService;
    private readonly IStateStorageService? _storageService;
    private readonly ILogger<GameStateService>? _logger;
    private readonly SpecialClassService _specialClassService;
    private readonly EquipmentService _equipmentService;
    private readonly EquipmentValidator _equipmentValidator;
    private readonly TraderService _traderService;
    private readonly FeatFlawService _featFlawService;
    private readonly InjuryService _injuryService;
    private readonly SpellService _spellService;
    private readonly CampaignProgressionService _campaignProgressionService;
    private readonly PetService _petService;
    private const string StateStorageKey = "forbidden-psalm-state";
    private static readonly Lazy<WarbandNameGenerator> _nameGenerator = new(LoadNameGenerator);

    public GlobalGameState State => _state;

    // Events
    public event Action? StateChanged
    {
        add => _state.StateChanged += value;
        remove => _state.StateChanged -= value;
    }

    public event Action<string>? WarbandChanged
    {
        add => _state.WarbandChanged += value;
        remove => _state.WarbandChanged -= value;
    }

    public event Action<string>? GameVariantChanged
    {
        add => _state.GameVariantChanged += value;
        remove => _state.GameVariantChanged -= value;
    }

    public event Action<string?>? ActiveWarbandChanged
    {
        add => _state.ActiveWarbandChanged += value;
        remove => _state.ActiveWarbandChanged -= value;
    }

    public GameStateService(
        GlobalGameState state,
        IWarbandRepository warbandRepository,
        IEmbeddedResourceService? resourceService = null,
        IStateStorageService? storageService = null,
        ILogger<GameStateService>? logger = null)
    {
        _state = state;
        _warbandRepository = warbandRepository;
        _resourceService = resourceService ?? new EmbeddedResourceService();
        _logger = logger;
        _storageService = storageService;
        _specialClassService = new SpecialClassService(_resourceService);
        _equipmentService = new EquipmentService(_resourceService);
        _equipmentValidator = new EquipmentValidator();
        _traderService = new TraderService(_resourceService);
        _featFlawService = new FeatFlawService(_resourceService);
        _injuryService = new InjuryService(_resourceService);
        _spellService = new SpellService(_resourceService);
        _campaignProgressionService = new CampaignProgressionService(_resourceService);
        _petService = new PetService(_resourceService);
    }

    // Game variant management
    public async Task SetGameVariantAsync(string variant)
    {
        if (!_state.GameConfigs.ContainsKey(variant))
            throw new ArgumentException($"Unknown game variant: {variant}");

        var oldVariant = _state.SelectedGameVariant;
        _state.SelectedGameVariant = variant;

        // Clear active warband if it doesn't belong to new variant
        if (_state.ActiveWarband?.GameVariant != variant)
        {
            _state.ActiveWarbandId = null;
            _state.NotifyActiveWarbandChanged(null);
        }

        _state.NotifyGameVariantChanged(variant);
        await SaveStateAsync();
    }

    public async Task<List<string>> GetAvailableGameVariantsAsync()
    {
        await Task.CompletedTask;
        return _state.AvailableGameVariants;
    }

    // Warband management
    public async Task<string> CreateWarbandAsync(string name)
    {
        return await CreateWarbandAsync(name, _state.SelectedGameVariant);
    }

    public async Task<string> CreateWarbandAsync(string name, string gameVariant)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warband name cannot be empty");

        if (!_state.GameConfigs.ContainsKey(gameVariant))
            throw new ArgumentException($"Unknown game variant: {gameVariant}");

        var warband = new Warband(name, gameVariant);
        var savedWarband = await _warbandRepository.SaveAsync(warband);

        // Update in-memory state for UI reactivity
        _state.Warbands[savedWarband.Id] = savedWarband;

        // Set as active if no current active warband for this game
        if (_state.ActiveWarbandId == null || _state.ActiveWarband?.GameVariant != gameVariant)
        {
            _state.ActiveWarbandId = savedWarband.Id;
            _state.NotifyActiveWarbandChanged(savedWarband.Id);
        }

        _state.NotifyWarbandChanged(savedWarband.Id);
        await SaveStateAsync();

        return savedWarband.Id;
    }

    public async Task DeleteWarbandAsync(string warbandId)
    {
        var deleted = await _warbandRepository.DeleteAsync(warbandId);
        if (!deleted)
            throw new ArgumentException($"Warband not found: {warbandId}");

        // Update in-memory state for UI reactivity
        _state.Warbands.Remove(warbandId);

        // Clear active warband if it was deleted
        if (_state.ActiveWarbandId == warbandId)
        {
            _state.ActiveWarbandId = null;
            _state.NotifyActiveWarbandChanged(null);
        }

        _state.NotifyWarbandChanged(warbandId);
        await SaveStateAsync();
    }

    public async Task SetActiveWarbandAsync(string? warbandId)
    {
        if (warbandId != null && !_state.Warbands.ContainsKey(warbandId))
            throw new ArgumentException($"Warband not found: {warbandId}");

        _state.ActiveWarbandId = warbandId;
        _state.NotifyActiveWarbandChanged(warbandId);
        await SaveStateAsync();
    }

    public async Task<List<Warband>> GetWarbandsAsync()
    {
        var warbands = await _warbandRepository.GetAllAsync();
        var warbandList = warbands.ToList();

        // Migrate spells to equipment for all warbands
        foreach (var warband in warbandList)
        {
            await MigrateSpellsToEquipmentAsync(warband);
        }

        // Update in-memory state for UI reactivity
        _state.Warbands.Clear();
        foreach (var warband in warbandList)
        {
            _state.Warbands[warband.Id] = warband;
        }

        return warbandList;
    }

    public async Task<List<Warband>> GetWarbandsForGameAsync(string gameVariant)
    {
        var allWarbands = await GetWarbandsAsync();
        return allWarbands.Where(w => w.GameVariant == gameVariant).ToList();
    }

    public async Task<Warband?> GetWarbandAsync(string warbandId)
    {
        var warband = await _warbandRepository.GetByIdAsync(warbandId);
        if (warband != null)
        {
            // Migrate existing spells to equipment if needed
            await MigrateSpellsToEquipmentAsync(warband);

            // Update in-memory state for UI reactivity
            _state.Warbands[warband.Id] = warband;
        }
        return warband;
    }

    private async Task MigrateSpellsToEquipmentAsync(Warband warband)
    {
        bool needsSave = false;

        foreach (var character in warband.Members)
        {
            // For each spell in character.Spells, check if it exists in character.Equipment
            foreach (var spellName in character.Spells.ToList())
            {
                var existsInEquipment = character.Equipment.Any(e => e.Name == spellName && e.Type == "spell");

                if (!existsInEquipment)
                {
                    // Get spell details
                    var spellOptions = await _spellService.GetSpellsAsync(warband.GameVariant);
                    var spellOption = spellOptions.FirstOrDefault(s => s.Name == spellName);

                    if (spellOption != null)
                    {
                        // Add spell as equipment
                        var spellEquipment = new Equipment
                        {
                            Name = spellName,
                            Type = "spell",
                            Category = spellOption.SpellCategory,
                            Slots = 1,
                            Effect = spellOption.Effect,
                            Cost = spellOption.Cost ?? 0,
                            IconClass = "fas fa-magic"
                        };
                        character.Equipment.Add(spellEquipment);
                        needsSave = true;
                    }
                }
            }
        }

        if (needsSave)
        {
            await _warbandRepository.SaveAsync(warband);
        }
    }

    public async Task UpdateWarbandAsync(Warband warband)
    {
        var exists = await _warbandRepository.ExistsAsync(warband.Id);
        if (!exists)
            throw new ArgumentException($"Warband not found: {warband.Id}");

        warband.UpdateLastModified();
        var updatedWarband = await _warbandRepository.SaveAsync(warband);

        // Update in-memory state for UI reactivity
        _state.Warbands[updatedWarband.Id] = updatedWarband;
        _state.NotifyWarbandChanged(updatedWarband.Id);
        await SaveStateAsync();
    }

    // Character management
    public async Task<string> AddCharacterToWarbandAsync(string warbandId, Character character)
    {
        if (!_state.Warbands.TryGetValue(warbandId, out var warband))
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (!warband.CanAddMember)
            throw new InvalidOperationException("Warband is already at maximum size");

        // Deduct special class cost if applicable
        if (!string.IsNullOrEmpty(character.SpecialClassId))
        {
            var specialClass = await _specialClassService.GetSpecialClassByIdAsync(character.SpecialClassId, warband.GameVariant);
            if (specialClass?.Cost.HasValue == true)
            {
                warband.Gold -= specialClass.Cost.Value;
            }
        }

        warband.Members.Add(character);
        warband.UpdateLastModified();

        // Save to repository
        var updatedWarband = await _warbandRepository.SaveAsync(warband);
        _state.Warbands[updatedWarband.Id] = updatedWarband;

        _state.NotifyWarbandChanged(warbandId);
        await SaveStateAsync();

        return character.Id;
    }

    public async Task UpdateCharacterAsync(string warbandId, string characterId, Character character)
    {
        if (!_state.Warbands.TryGetValue(warbandId, out var warband))
            throw new ArgumentException($"Warband not found: {warbandId}");

        var existingCharacter = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (existingCharacter == null)
            throw new ArgumentException($"Character not found: {characterId}");

        // Handle special class cost changes
        var oldSpecialClassId = existingCharacter.SpecialClassId;
        var newSpecialClassId = character.SpecialClassId;

        if (oldSpecialClassId != newSpecialClassId)
        {
            // Refund old special class cost
            if (!string.IsNullOrEmpty(oldSpecialClassId))
            {
                var oldSpecialClass = await _specialClassService.GetSpecialClassByIdAsync(oldSpecialClassId, warband.GameVariant);
                if (oldSpecialClass?.Cost.HasValue == true)
                {
                    warband.Gold += oldSpecialClass.Cost.Value;
                }
            }

            // Deduct new special class cost
            if (!string.IsNullOrEmpty(newSpecialClassId))
            {
                var newSpecialClass = await _specialClassService.GetSpecialClassByIdAsync(newSpecialClassId, warband.GameVariant);
                if (newSpecialClass?.Cost.HasValue == true)
                {
                    warband.Gold -= newSpecialClass.Cost.Value;
                }
            }
        }

        var index = warband.Members.IndexOf(existingCharacter);
        character.Id = characterId; // Preserve ID
        warband.Members[index] = character;
        warband.UpdateLastModified();

        // Save to repository
        var updatedWarband = await _warbandRepository.SaveAsync(warband);
        _state.Warbands[updatedWarband.Id] = updatedWarband;

        _state.NotifyWarbandChanged(warbandId);
        await SaveStateAsync();
    }

    public async Task RemoveCharacterFromWarbandAsync(string warbandId, string characterId, bool isDead = true)
    {
        if (!_state.Warbands.TryGetValue(warbandId, out var warband))
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        // Remove from active members
        warband.Members.Remove(character);

        // Handle equipment based on deletion reason
        if (isDead)
        {
            // Character died - equipment goes to stash, character goes to dead list
            foreach (var equipment in character.Equipment)
            {
                warband.Stash.Add(equipment);
            }
            warband.DeadMembers.Add(character);
            _logger?.LogInformation("Character died: {CharacterName} from warband: {WarbandName}", character.Name, warband.Name);
        }
        else
        {
            // Character was fired - equipment is lost, character goes to fired list
            warband.FiredMembers.Add(character);
            _logger?.LogInformation("Character fired: {CharacterName} from warband: {WarbandName}", character.Name, warband.Name);
        }

        warband.UpdateLastModified();

        // Save to repository
        var updatedWarband = await _warbandRepository.SaveAsync(warband);
        _state.Warbands[updatedWarband.Id] = updatedWarband;

        _state.NotifyWarbandChanged(warbandId);
        await SaveStateAsync();
    }

    // Character builder state
    public async Task StartCharacterBuilderAsync(string warbandId)
    {
        if (!_state.Warbands.ContainsKey(warbandId))
            throw new ArgumentException($"Warband not found: {warbandId}");

        _state.CharacterBuilderWarbandId = warbandId;
        _state.CharacterBeingBuilt = new Character();
        _state.NotifyStateChanged();
        await Task.CompletedTask;
    }

    public async Task SetCharacterBeingBuiltAsync(Character character)
    {
        _state.CharacterBeingBuilt = character;
        _state.NotifyStateChanged();
        await Task.CompletedTask;
    }

    public async Task FinishCharacterBuilderAsync()
    {
        if (_state.CharacterBeingBuilt == null || _state.CharacterBuilderWarbandId == null)
            throw new InvalidOperationException("No character being built");

        await AddCharacterToWarbandAsync(_state.CharacterBuilderWarbandId, _state.CharacterBeingBuilt);

        _state.CharacterBeingBuilt = null;
        _state.CharacterBuilderWarbandId = null;
        _state.NotifyStateChanged();
    }

    public async Task CancelCharacterBuilderAsync()
    {
        _state.CharacterBeingBuilt = null;
        _state.CharacterBuilderWarbandId = null;
        _state.NotifyStateChanged();
        await Task.CompletedTask;
    }

    // Data loading (placeholder - will implement with actual data service)
    public async Task LoadGameDataAsync()
    {
        _state.SetLoading(true);
        try
        {
            // For now, populate with hardcoded game configs based on known variants
            // This will be replaced with actual JSON loading later
            var gameConfigs = new Dictionary<string, GameConfig>
            {
                ["28-psalms"] = new GameConfig
                {
                    GameName = "28 Psalms",
                    Currency = new Currency { Name = "Credits", Symbol = "C", StartingBudget = 50 }
                },
                ["end-times"] = new GameConfig
                {
                    GameName = "Forbidden Psalm: End Times",
                    Currency = new Currency { Name = "Gold", Symbol = "G", StartingBudget = 50 }
                },
                ["last-war"] = new GameConfig
                {
                    GameName = "The Last War",
                    Currency = new Currency { Name = "Resources", Symbol = "R", StartingBudget = 50 }
                }
            };

            _state.GameConfigs.Clear();
            foreach (var config in gameConfigs)
            {
                _state.GameConfigs[config.Key] = config.Value;
            }

            await Task.Delay(100); // Simulate loading
            _state.ClearError();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load game data");
            _state.SetError($"Failed to load game data: {ex.Message}");
        }
        finally
        {
            _state.SetLoading(false);
        }
    }

    public async Task RefreshGameDataAsync()
    {
        await LoadGameDataAsync();
    }

    public async Task<List<ForbiddenPsalmBuilder.Core.Models.Character.StatArray>> GetStatArraysAsync()
    {
        await Task.CompletedTask; // For async consistency

        // Based on character-creation.json data
        return new List<ForbiddenPsalmBuilder.Core.Models.Character.StatArray>
        {
            new ForbiddenPsalmBuilder.Core.Models.Character.StatArray
            {
                Id = "specialist",
                Name = "Specialist",
                Values = new[] { 3, 1, 0, -3 },
                Description = "High specialization with major weakness"
            },
            new ForbiddenPsalmBuilder.Core.Models.Character.StatArray
            {
                Id = "balanced",
                Name = "Balanced",
                Values = new[] { 2, 2, -1, -2 },
                Description = "More balanced distribution"
            }
        };
    }

    // State persistence (placeholder - will implement with localStorage)
    public async Task SaveStateAsync()
    {
        try
        {
            if (_storageService == null)
                return; // No storage service configured, skip persistence

            await _storageService.SetItemAsync(StateStorageKey, _state);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save state");
            _state.SetError($"Failed to save state: {ex.Message}");
        }
    }

    public async Task LoadStateAsync()
    {
        try
        {
            if (_storageService == null)
                return; // No storage service configured, skip persistence

            var savedState = await _storageService.GetItemAsync<GlobalGameState>(StateStorageKey);
            if (savedState != null)
            {
                // Restore warbands
                _state.Warbands = savedState.Warbands;
                _state.ActiveWarbandId = savedState.ActiveWarbandId;
                _state.SelectedGameVariant = savedState.SelectedGameVariant;
                _state.CharacterBeingBuilt = savedState.CharacterBeingBuilt;
                _state.CharacterBuilderWarbandId = savedState.CharacterBuilderWarbandId;

                _state.NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load state");
            _state.SetError($"Failed to load state: {ex.Message}");
        }
    }

    public async Task ClearStateAsync()
    {
        _state.Warbands.Clear();
        _state.ActiveWarbandId = null;
        _state.CharacterBeingBuilt = null;
        _state.CharacterBuilderWarbandId = null;
        _state.ClearError();
        _state.NotifyStateChanged();

        // Remove persisted state
        if (_storageService != null)
        {
            await _storageService.RemoveItemAsync(StateStorageKey);
        }
    }

    // Name generation
    public async Task<string> GenerateWarbandNameAsync()
    {
        return await Task.FromResult(_nameGenerator.Value.GenerateName());
    }

    public async Task<string> GenerateCharacterNameAsync(string gameVariant)
    {
        var generator = new CharacterNameGenerator(_resourceService);
        return await generator.GenerateNameAsync(gameVariant);
    }

    private static WarbandNameGenerator LoadNameGenerator()
    {
        try
        {
            // Try to load from the JSON file
            var dataPath = Path.Combine("data", "shared", "warband-names.json");
            if (File.Exists(dataPath))
            {
                return WarbandNameGenerator.LoadFromFileAsync(dataPath).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // Fall back to hardcoded data if file loading fails
        }

        // Fallback to basic hardcoded data
        var basicData = new WarbandNameData
        {
            Patterns = new List<string>
            {
                "[group] of [adjective] [occupation]",
                "the [adjective] [group]",
                "the [occupation]",
                "the [adjective] [occupation]"
            },
            Group = new List<string> { "Band", "Guild", "Order", "Circle", "Crew", "Pack", "Legion" },
            Occupation = new List<string> { "Seekers", "Wanderers", "Guardians", "Hunters", "Defenders", "Knights", "Scribes" },
            Adjective = new List<string> { "Dark", "Ancient", "Wild", "Lost", "Iron", "Silent", "Forgotten", "Desperate" },
            Location = new List<string> { "of the Woods", "of the Hills", "of the Void" },
            LocationTemplates = new List<string>(),
            Shape = new List<string>(),
            Atmosphere = new List<string>(),
            Color = new List<string>(),
            Animal = new List<string>()
        };

        return new WarbandNameGenerator(basicData);
    }

    // Error handling
    public async Task SetErrorAsync(string? error)
    {
        _state.SetError(error);
        await Task.CompletedTask;
    }

    public async Task ClearErrorAsync()
    {
        _state.ClearError();
        await Task.CompletedTask;
    }

    // Special Classes
    public async Task<List<SpecialClass>> GetSpecialClassesAsync(string gameVariant)
    {
        return await _specialClassService.GetSpecialClassesAsync(gameVariant);
    }

    public async Task<SpecialClass?> GetSpecialClassByIdAsync(string specialClassId, string gameVariant)
    {
        return await _specialClassService.GetSpecialClassByIdAsync(specialClassId, gameVariant);
    }

    public async Task<bool> CanAddSpecialClassAsync(string warbandId, string specialClassId)
    {
        var error = await ValidateSpecialClassSelectionAsync(warbandId, specialClassId);
        return error == null;
    }

    public async Task<string?> ValidateSpecialClassSelectionAsync(string warbandId, string? specialClassId, string? characterIdToExclude = null)
    {
        // Null or empty special class is always valid (no special class selected)
        if (string.IsNullOrEmpty(specialClassId))
            return null;

        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            return "Warband not found";

        // Get the special class definition
        var specialClass = await GetSpecialClassByIdAsync(specialClassId, warband.GameVariant);
        if (specialClass == null)
            return $"Special class '{specialClassId}' not found for game variant '{warband.GameVariant}'";

        // Check if this special class has a limit
        if (!specialClass.Metadata.ContainsKey("limit"))
            return null; // No limit defined, always valid

        var limitValue = specialClass.Metadata["limit"];

        // Handle null limit (unlimited)
        if (limitValue == null)
            return null;

        // Check for canSelectMultiple flag (like Civilian)
        if (specialClass.Metadata.ContainsKey("canSelectMultiple"))
        {
            var canSelectMultiple = specialClass.Metadata["canSelectMultiple"];
            if (canSelectMultiple is bool boolValue && boolValue)
                return null; // Can select multiple, no limit
            if (canSelectMultiple is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
                return null; // Can select multiple, no limit
        }

        // Parse limit value (handle JsonElement from JSON deserialization)
        int limit;
        if (limitValue is JsonElement jsonLimitElement)
        {
            if (jsonLimitElement.ValueKind == JsonValueKind.Number)
                limit = jsonLimitElement.GetInt32();
            else
                return null; // Invalid limit format, skip validation
        }
        else if (limitValue is int intValue)
        {
            limit = intValue;
        }
        else
        {
            return null; // Invalid limit format, skip validation
        }

        // Count existing characters with this special class (excluding the character being edited)
        var existingCount = warband.Members
            .Where(m => m.SpecialClassId == specialClassId && m.Id != characterIdToExclude)
            .Count();

        if (existingCount >= limit)
        {
            return $"Warband already has the maximum number ({limit}) of '{specialClass.DisplayName}' special class members";
        }

        return null; // Validation passed
    }

    // Equipment Management

    public async Task AddEquipmentToCharacterAsync(string warbandId, string characterId, string equipmentId, string equipmentType, bool equipImmediately = false)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        Equipment? equipment = null;
        Equipment? sourceEquipment = null;

        // Handle stash equipment separately (already in Equipment format)
        if (equipmentType.ToLower() == "stash")
        {
            equipment = warband.Stash.FirstOrDefault(e => e.Id == equipmentId);
            if (equipment != null)
            {
                // Remove from stash
                warband.Stash.Remove(equipment);
                // Set equipped status based on parameter
                equipment.IsEquipped = equipImmediately;
            }
        }
        else
        {
            // Load equipment using unified service method
            sourceEquipment = await _equipmentService.GetEquipmentByIdAsync(equipmentId, warband.GameVariant, equipmentType.ToLower());

            if (sourceEquipment == null)
                throw new ArgumentException($"Equipment not found: {equipmentId}");

            // Check if unique relic already acquired
            if (sourceEquipment.IsRelic && sourceEquipment.Unique && warband.AcquiredRelicIds.Contains(equipmentId))
                throw new InvalidOperationException($"The relic '{sourceEquipment.Name}' has already been acquired by this warband");

            // Create a copy for the character
            equipment = new Equipment
            {
                Id = Guid.NewGuid().ToString(),
                Name = sourceEquipment.Name,
                Type = sourceEquipment.Type,
                Category = sourceEquipment.Category,
                Damage = sourceEquipment.Damage,
                Range = sourceEquipment.Range,
                Properties = sourceEquipment.Properties,
                Stat = sourceEquipment.Stat,
                Cost = sourceEquipment.Cost,
                Slots = sourceEquipment.Slots,
                IconClass = sourceEquipment.IconClass,
                Effect = sourceEquipment.Effect,
                RollRange = sourceEquipment.RollRange,
                ArmorValue = sourceEquipment.ArmorValue,
                ArmorType = sourceEquipment.ArmorType,
                Special = sourceEquipment.Special,
                AmmoType = sourceEquipment.AmmoType,
                MaxShots = sourceEquipment.MaxShots,
                // For ranged weapons that use ammo, start with a full load
                CurrentShots = (sourceEquipment.IsWeapon && !string.IsNullOrEmpty(sourceEquipment.AmmoType) && sourceEquipment.MaxShots > 0)
                    ? sourceEquipment.MaxShots
                    : sourceEquipment.CurrentShots,
                IsConsumable = sourceEquipment.IsConsumable,
                ConsumableEffect = sourceEquipment.ConsumableEffect,
                Unique = sourceEquipment.Unique,
                Restrictions = sourceEquipment.Restrictions,
                Effects = sourceEquipment.Effects,
                IsEquipped = equipImmediately // Set equipped status based on parameter
            };

            // Track acquisition for unique relics
            if (sourceEquipment.IsRelic && sourceEquipment.Unique)
                warband.AcquiredRelicIds.Add(equipmentId);
        }

        if (equipment == null)
            throw new ArgumentException($"Equipment not found: {equipmentId}");

        // Extract stat requirements for armor
        int? reqStr = null, reqAgl = null, reqPrs = null, reqTgh = null;
        if (equipmentType.ToLower() == "armor" && sourceEquipment != null)
        {
            // Use unified method to extract stat requirements from Equipment
            var requirements = _equipmentValidator.ExtractStatRequirements(sourceEquipment);
            reqStr = requirements.strength;
            reqAgl = requirements.agility;
            reqPrs = requirements.presence;
            reqTgh = requirements.toughness;
        }

        // For weapons that require ammo, create a separate ammo item that takes inventory slots
        Equipment? startingAmmo = null;
        if (sourceEquipment != null && sourceEquipment.IsWeapon && sourceEquipment.RequiresAmmo && sourceEquipment.MaxShots > 0)
        {
            // Find the ammo type in the catalog
            var ammoEquipment = await _equipmentService.GetEquipmentAsync(warband.GameVariant, "item");
            var ammoTemplate = ammoEquipment.FirstOrDefault(e =>
                e.Type == "ammo" && e.Name.Equals(sourceEquipment.AmmoType, StringComparison.OrdinalIgnoreCase));

            if (ammoTemplate != null)
            {
                // Create starting ammo as a separate equipment item
                startingAmmo = new Equipment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = ammoTemplate.Name,
                    Type = "ammo",
                    Cost = ammoTemplate.Cost,
                    Slots = ammoTemplate.Slots,
                    IconClass = ammoTemplate.IconClass,
                    CurrentShots = sourceEquipment.MaxShots, // One load of ammo
                    MaxShots = ammoTemplate.MaxShots,
                    IsEquipped = false // Ammo is never equipped
                };
            }
        }

        // Always validate slots (inventory items count toward slots too)
        if (equipmentType.ToLower() != "stash")
        {
            // Check slot availability for weapon AND its starting ammo
            var totalSlotsNeeded = equipment.CalculateRequiredSlots();
            if (startingAmmo != null)
            {
                totalSlotsNeeded += startingAmmo.CalculateRequiredSlots();
            }

            var usedSlots = character.Equipment.Sum(e => e.CalculateRequiredSlots());
            var totalSlots = character.GetEffectiveEquipmentSlots();

            if (usedSlots + totalSlotsNeeded > totalSlots)
            {
                throw new InvalidOperationException($"Not enough equipment slots. Character has {totalSlots} slots, {usedSlots} used, needs {totalSlotsNeeded} more (weapon + starting ammo).");
            }

            // Only validate armor limits, stat requirements, and restrictions when equipping immediately
            if (equipImmediately)
            {
                var validationError = _equipmentValidator.ValidateEquipment(character, equipment, reqStr, reqAgl, reqPrs, reqTgh);
                if (validationError != null)
                    throw new InvalidOperationException(validationError);

                // Check for flaw-based restrictions (e.g., Allergic To Metal)
                var flaws = await _featFlawService.GetFlawsAsync(warband.GameVariant);
                if (character.HasFlawRestriction(equipment, flaws))
                {
                    throw new InvalidOperationException($"Cannot equip {equipment.Name}: Character has a flaw that prevents wearing this item");
                }
            }
        }

        // Add equipment
        character.Equipment.Add(equipment);

        // Add starting ammo if we created it
        if (startingAmmo != null)
        {
            await AddAmmoToCharacterAsync(warbandId, characterId, startingAmmo);
        }

        // If it's a spell, also add to spells list
        if (equipment.Type == "spell" && !character.Spells.Contains(equipment.Name))
        {
            character.Spells.Add(equipment.Name);
        }

        // Save warband
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyWarbandChanged(warbandId);
    }

    /// <summary>
    /// Overload for directly adding an Equipment object to a character.
    /// Useful for testing and scenarios where equipment is already instantiated.
    /// Equipment is added as-is without any modifications.
    /// </summary>
    public async Task AddEquipmentToCharacterAsync(string warbandId, string characterId, Equipment equipment)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        // Add equipment as-is (no modifications for test scenarios)
        character.Equipment.Add(equipment);

        // Save warband
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task RemoveEquipmentFromCharacterAsync(string warbandId, string characterId, string equipmentId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        var equipment = character.Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new ArgumentException($"Equipment not found: {equipmentId}");

        // If weapon has loaded ammo, preserve it as a separate ammo item
        if (equipment.IsWeapon && equipment.RequiresAmmo && equipment.CurrentShots > 0)
        {
            // Load ammo details from equipment service to get proper icon and properties
            var ammoItems = await _equipmentService.GetEquipmentAsync(warband.GameVariant, "ammo");
            var ammoTemplate = ammoItems.FirstOrDefault(a =>
                a.Name.Equals(equipment.AmmoType, StringComparison.OrdinalIgnoreCase));

            if (ammoTemplate != null)
            {
                // Create new ammo item with the loaded shots
                var ammoItem = new Equipment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = ammoTemplate.Name,
                    Type = "ammo",
                    CurrentShots = equipment.CurrentShots,
                    MaxShots = equipment.MaxShots,
                    Slots = ammoTemplate.Slots,
                    Cost = ammoTemplate.Cost,
                    IconClass = ammoTemplate.IconClass,
                    Effect = ammoTemplate.Effect
                };

                // Add ammo to character's inventory
                character.Equipment.Add(ammoItem);
            }

            // Clear weapon's ammo
            equipment.CurrentShots = 0;
        }

        // Remove from character
        character.Equipment.Remove(equipment);

        // If it's a spell, also remove from spells list
        if (equipment.Type == "spell")
        {
            character.Spells.Remove(equipment.Name);
        }

        // Add to stash
        warband.Stash.Add(equipment);

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task EquipItemAsync(string warbandId, string characterId, string equipmentId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        var equipment = character.Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new ArgumentException($"Equipment not found: {equipmentId}");

        // Already equipped, nothing to do
        if (equipment.IsEquipped)
            return;

        // Validate equipment (check slots, restrictions, etc.)
        var validationError = _equipmentValidator.ValidateEquipment(character, equipment, null, null, null, null);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        // Check for flaw-based restrictions
        var flaws = await _featFlawService.GetFlawsAsync(warband.GameVariant);
        if (character.HasFlawRestriction(equipment, flaws))
        {
            throw new InvalidOperationException($"Cannot equip {equipment.Name}: Character has a flaw that prevents wearing this item");
        }

        // Equip the item
        character.EquipItem(equipmentId);

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task UnequipItemAsync(string warbandId, string characterId, string equipmentId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new ArgumentException($"Character not found: {characterId}");

        var equipment = character.Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new ArgumentException($"Equipment not found: {equipmentId}");

        // Already unequipped, nothing to do
        if (!equipment.IsEquipped)
            return;

        // Unequip the item
        character.UnequipItem(equipmentId);

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyWarbandChanged(warbandId);
    }

    public bool CanCharacterEquip(string warbandId, string characterId, Equipment equipment)
    {
        if (!_state.Warbands.ContainsKey(warbandId))
            return false;

        var warband = _state.Warbands[warbandId];
        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);

        if (character == null)
            return false;

        // Check slot availability
        if (!character.CanEquip(equipment))
            return false;

        // Check equipment-based restrictions (e.g., Ring of the Mighty prevents armor)
        if (character.HasEquipmentRestriction(equipment))
            return false;

        // Note: Flaw-based restrictions are checked in AddEquipmentToCharacterAsync
        // since they require async flaw loading

        return true;
    }

    public async Task<List<Equipment>> GetAvailableEquipmentForCharacterAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            return new List<Equipment>();

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            return new List<Equipment>();

        // Return all equipment from warband stash (not filtered by CanEquip)
        // The UI will show errors if items can't be equipped
        return warband.Stash.ToList();
    }

    public async Task BuyEquipmentAsync(string warbandId, string equipmentId, string equipmentType, string? traderId = null)
    {
        _logger?.LogInformation("BuyEquipmentAsync called: WarbandId={WarbandId}, EquipmentId={EquipmentId}, Type={Type}, TraderId={TraderId}",
            warbandId, equipmentId, equipmentType, traderId);

        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
        {
            _logger?.LogError("Warband not found: {WarbandId}", warbandId);
            throw new InvalidOperationException("Warband not found");
        }

        _logger?.LogInformation("Warband found: {WarbandName}, Gold={Gold}", warband.Name, warband.Gold);

        // Load trader if specified
        Trader? trader = null;
        if (!string.IsNullOrEmpty(traderId))
        {
            trader = await _traderService.GetTraderByIdAsync(traderId, warband.GameVariant);
            if (trader == null)
            {
                _logger?.LogError("Trader not found: {TraderId}", traderId);
                throw new InvalidOperationException("Trader not found");
            }
            _logger?.LogInformation("Trader loaded: {TraderName}", trader.Name);
        }

        // Load equipment using unified service method
        var equipment = await _equipmentService.GetEquipmentByIdAsync(equipmentId, warband.GameVariant, equipmentType.ToLower());

        if (equipment == null)
            throw new InvalidOperationException($"{equipmentType} not found");

        if (equipment.Cost <= 0)
            throw new InvalidOperationException($"This {equipmentType} cannot be purchased");

        // Special handling for relics
        int buyPrice;
        if (equipment.IsRelic)
        {
            // Check if unique relic already acquired
            if (equipment.Unique && warband.AcquiredRelicIds.Contains(equipmentId))
                throw new InvalidOperationException($"The relic '{equipment.Name}' has already been acquired by this warband");

            // Relics have flat 100G price (no trader modifiers)
            buyPrice = 100;
        }
        else
        {
            // Calculate buy price with trader or use base cost
            buyPrice = trader != null
                ? trader.CalculateBuyPrice(equipment.Cost)
                : equipment.Cost;
        }

        if (!warband.CanAfford(buyPrice))
            throw new InvalidOperationException($"Not enough gold. Need {buyPrice}, have {warband.Gold}");

        // Create a copy of the equipment for the stash (with unique instance ID)
        var newEquipment = new Equipment
        {
            Id = Guid.NewGuid().ToString(), // New instance ID
            Name = equipment.Name,
            Type = equipment.Type,
            Damage = equipment.Damage,
            Range = equipment.Range,
            Properties = equipment.Properties,
            Stat = equipment.Stat,
            ArmorValue = equipment.ArmorValue,
            ArmorType = equipment.ArmorType,
            Special = equipment.Special,
            Effect = equipment.Effect,
            Cost = equipment.Cost,
            Slots = equipment.Slots,
            IconClass = equipment.IconClass,
            AmmoType = equipment.AmmoType,
            MaxShots = equipment.MaxShots,
            // For ranged weapons that use ammo, start with a full load
            CurrentShots = (equipment.IsWeapon && !string.IsNullOrEmpty(equipment.AmmoType) && equipment.MaxShots > 0)
                ? equipment.MaxShots
                : equipment.CurrentShots,
            IsConsumable = equipment.IsConsumable,
            ConsumableEffect = equipment.ConsumableEffect,
            Unique = equipment.Unique,
            Category = equipment.Category
        };

        // Track acquisition for unique relics
        if (equipment.IsRelic && equipment.Unique)
            warband.AcquiredRelicIds.Add(equipmentId);

        // For weapons that require ammo, create a separate starting ammo item
        Equipment? startingAmmo = null;
        if (newEquipment.IsWeapon && !string.IsNullOrEmpty(newEquipment.AmmoType) && newEquipment.MaxShots > 0)
        {
            // Find the ammo type in the catalog
            var ammoEquipment = await _equipmentService.GetEquipmentAsync(warband.GameVariant, "item");
            var ammoTemplate = ammoEquipment.FirstOrDefault(e =>
                e.Type == "ammo" && e.Name.Equals(newEquipment.AmmoType, StringComparison.OrdinalIgnoreCase));

            if (ammoTemplate != null)
            {
                // Create starting ammo as a separate equipment item
                startingAmmo = new Equipment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = ammoTemplate.Name,
                    Type = "ammo",
                    Cost = ammoTemplate.Cost,
                    Slots = ammoTemplate.Slots,
                    IconClass = ammoTemplate.IconClass,
                    CurrentShots = newEquipment.MaxShots, // One load of ammo
                    MaxShots = ammoTemplate.MaxShots,
                    IsEquipped = false // Ammo is never equipped
                };
                _logger?.LogInformation("Created starting ammo: {AmmoName} with {Shots} shots", startingAmmo.Name, startingAmmo.CurrentShots);
            }
        }

        // Deduct gold and add to stash
        _logger?.LogInformation("Buying {EquipmentName} for {Price} gold. Gold before: {GoldBefore}", newEquipment.Name, buyPrice, warband.Gold);
        warband.Gold -= buyPrice;
        warband.Stash.Add(newEquipment);

        // Add starting ammo to stash if created
        if (startingAmmo != null)
        {
            warband.Stash.Add(startingAmmo);
            _logger?.LogInformation("Added starting ammo to stash");
        }

        warband.UpdateLastModified();
        _logger?.LogInformation("Gold after purchase: {GoldAfter}, Stash count: {StashCount}", warband.Gold, warband.Stash.Count);

        await _warbandRepository.SaveAsync(warband);
        _logger?.LogInformation("Warband saved to repository");

        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
        _logger?.LogInformation("State change notifications sent");
    }

    public async Task SellEquipmentAsync(string warbandId, string equipmentId, string? traderId = null)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var equipment = warband.Stash.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new InvalidOperationException("Equipment not found in stash");

        // Load trader if specified
        Trader? trader = null;
        if (!string.IsNullOrEmpty(traderId))
        {
            trader = await _traderService.GetTraderByIdAsync(traderId, warband.GameVariant);
            if (trader == null)
                throw new InvalidOperationException("Trader not found");
        }

        // Calculate sell price with trader or use base cost
        int sellPrice = trader != null
            ? trader.CalculateSellPrice(equipment.Cost)
            : equipment.Cost;

        // Add gold and remove from stash
        warband.Gold += sellPrice;
        warband.Stash.Remove(equipment);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task BuyRelicAsync(string warbandId, string relicId)
    {
        _logger?.LogInformation("BuyRelicAsync called: WarbandId={WarbandId}, RelicId={RelicId}", warbandId, relicId);

        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
        {
            _logger?.LogError("Warband not found: {WarbandId}", warbandId);
            throw new InvalidOperationException("Warband not found");
        }

        // Load relics from equipment service
        var relics = await _equipmentService.GetRelicsAsync(warband.GameVariant);
        var relic = relics.FirstOrDefault(r => r.Id == relicId);

        if (relic == null)
        {
            _logger?.LogError("Relic not found: {RelicId}", relicId);
            throw new InvalidOperationException("Relic not found");
        }

        // Check if unique relic already acquired
        if (relic.Unique && warband.AcquiredRelicIds.Contains(relicId))
        {
            _logger?.LogWarning("Unique relic already acquired: {RelicId}", relicId);
            throw new InvalidOperationException($"The relic '{relic.Name}' has already been acquired by this warband");
        }

        // Relics have flat 100G price (no trader modifiers)
        const int relicPrice = 100;

        if (!warband.CanAfford(relicPrice))
        {
            _logger?.LogWarning("Not enough gold to buy relic. Need {Price}, have {Gold}", relicPrice, warband.Gold);
            throw new InvalidOperationException($"Not enough gold. Need {relicPrice}, have {warband.Gold}");
        }

        // Create equipment copy for stash
        var newRelic = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = relic.Name,
            Type = "relic",
            Cost = relic.Cost,
            Slots = relic.Slots,
            Effect = relic.Effect,
            Damage = relic.Damage,
            Properties = relic.Properties,
            Stat = relic.Stat,
            Special = relic.Special,
            IconClass = relic.IconClass,
            Unique = relic.Unique
        };

        // Deduct gold and add to stash
        warband.Gold -= relicPrice;
        warband.Stash.Add(newRelic);
        warband.AcquiredRelicIds.Add(relicId); // Track acquisition
        warband.UpdateLastModified();

        _logger?.LogInformation("Relic purchased: {RelicName}, Gold remaining: {Gold}", relic.Name, warband.Gold);

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task MarkRelicAsAcquiredAsync(string warbandId, string relicId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        // Add to acquired list if not already there
        if (!warband.AcquiredRelicIds.Contains(relicId))
        {
            warband.AcquiredRelicIds.Add(relicId);
            _logger?.LogInformation("Relic marked as acquired: {RelicId} for warband: {WarbandId}", relicId, warbandId);
        }

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task MarkRelicAsAvailableAsync(string warbandId, string relicId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        // Remove from acquired list
        if (warband.AcquiredRelicIds.Contains(relicId))
        {
            warband.AcquiredRelicIds.Remove(relicId);
            _logger?.LogInformation("Relic marked as available: {RelicId} for warband: {WarbandId}", relicId, warbandId);
        }

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task TransferEquipmentToCharacterAsync(string warbandId, string characterId, string equipmentId, bool equipImmediately = false)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var equipment = warband.Stash.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new InvalidOperationException("Equipment not found in stash");

        // Always check slots (inventory items count toward slots too)
        if (!character.CanEquip(equipment))
        {
            var usedSlots = character.Equipment.Sum(e => e.CalculateRequiredSlots());
            var totalSlots = character.GetEffectiveEquipmentSlots();
            throw new InvalidOperationException($"Not enough equipment slots. Character has {totalSlots} slots, {usedSlots} used, needs {equipment.CalculateRequiredSlots()} more.");
        }

        // Only validate armor limits, stat requirements, and restrictions when equipping immediately
        if (equipImmediately)
        {
            var validationError = _equipmentValidator.ValidateEquipment(character, equipment);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            // Check for flaw-based restrictions
            var flaws = await _featFlawService.GetFlawsAsync(warband.GameVariant);
            if (character.HasFlawRestriction(equipment, flaws))
            {
                throw new InvalidOperationException($"Cannot equip {equipment.Name}: Character has a flaw that prevents wearing this item");
            }
        }

        // Transfer equipment and set equipped status
        warband.Stash.Remove(equipment);
        equipment.IsEquipped = equipImmediately;
        character.Equipment.Add(equipment);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task TransferEquipmentToStashAsync(string warbandId, string characterId, string equipmentId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var equipment = character.Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (equipment == null)
            throw new InvalidOperationException("Equipment not found on character");

        // Transfer equipment
        character.Equipment.Remove(equipment);
        warband.Stash.Add(equipment);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #region Feat & Flaw Management

    public async Task<string> RollFeatAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var feat = await _featFlawService.RollFeatAsync(warband.GameVariant);
        character.Feats.Add(feat.Name);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);

        return feat.Name;
    }

    public async Task<string> RollFlawAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var flaw = await _featFlawService.RollFlawAsync(warband.GameVariant);
        character.Flaws.Add(flaw.Name);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);

        return flaw.Name;
    }

    public async Task AddFeatAsync(string warbandId, string characterId, string featName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        if (character.Feats.Contains(featName))
            throw new InvalidOperationException($"Character already has feat: {featName}");

        character.Feats.Add(featName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task AddFlawAsync(string warbandId, string characterId, string flawName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        if (character.Flaws.Contains(flawName))
            throw new InvalidOperationException($"Character already has flaw: {flawName}");

        character.Flaws.Add(flawName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task RemoveFeatAsync(string warbandId, string characterId, string featName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        character.Feats.Remove(featName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task RemoveFlawAsync(string warbandId, string characterId, string flawName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        character.Flaws.Remove(flawName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Injury Management

    public async Task<string> RollInjuryAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var injury = await _injuryService.RollInjuryAsync(warband.GameVariant);
        character.Injuries.Add(injury.Name);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);

        return injury.Name;
    }

    public async Task AddInjuryAsync(string warbandId, string characterId, string injuryName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        if (character.Injuries.Contains(injuryName))
            throw new InvalidOperationException($"Character already has injury: {injuryName}");

        character.Injuries.Add(injuryName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task RemoveInjuryAsync(string warbandId, string characterId, string injuryName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        character.Injuries.Remove(injuryName);
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Spell Management

    public async Task AddSpellAsync(string warbandId, string characterId, string spellName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Get spell details to create equipment
        var spellOptions = await _spellService.GetSpellsAsync(warband.GameVariant);
        var spellOption = spellOptions.FirstOrDefault(s => s.Name == spellName);

        if (spellOption != null)
        {
            // Create spell equipment to check if it can be equipped
            var spellEquipment = new Equipment
            {
                Name = spellName,
                Type = "spell",
                Category = spellOption.SpellCategory,
                Slots = 1,
                Effect = spellOption.Effect,
                Cost = spellOption.Cost ?? 0,
                IconClass = "fas fa-magic"
            };

            // Check if character has enough equipment slots
            if (!character.CanEquip(spellEquipment))
                throw new InvalidOperationException($"Not enough equipment slots to add spell: {spellName}");

            // Add to spells list
            character.Spells.Add(spellName);

            // Add as equipment item
            character.Equipment.Add(spellEquipment);
        }

        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task RemoveSpellAsync(string warbandId, string characterId, string spellName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Remove from spells list
        character.Spells.Remove(spellName);

        // Remove from equipment and move to stash
        var spellEquipment = character.Equipment.FirstOrDefault(e => e.Name == spellName && e.Type == "spell");
        if (spellEquipment != null)
        {
            character.Equipment.Remove(spellEquipment);
            // Move to warband stash
            warband.Stash.Add(spellEquipment);
        }

        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task DeleteSpellPermanentlyAsync(string warbandId, string characterId, string spellName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(m => m.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Remove from spells list
        character.Spells.Remove(spellName);

        // Remove from equipment (do NOT move to stash)
        var spellEquipment = character.Equipment.FirstOrDefault(e => e.Name == spellName && e.Type == "spell");
        if (spellEquipment != null)
        {
            character.Equipment.Remove(spellEquipment);
        }

        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Level-Up Management

    public async Task<int> GetCharacterXpCostAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Get base XP cost from campaign progression
        var baseCost = await _campaignProgressionService.GetBaseXpCostAsync();

        // Check for Slow Learner flaw
        var flaws = await _featFlawService.GetFlawsAsync(warband.GameVariant);
        var slowLearnerModifier = 0;

        foreach (var flawName in character.Flaws)
        {
            var flaw = flaws.FirstOrDefault(f => f.Name == flawName);
            if (flaw?.Category == "xp_penalty" && flaw.XpModifier.HasValue)
            {
                slowLearnerModifier += flaw.XpModifier.Value;
            }
        }

        return baseCost + slowLearnerModifier;
    }

    public async Task<bool> CanCharacterLevelUpAsync(string warbandId, string characterId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            return false;

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            return false;

        var xpCost = await GetCharacterXpCostAsync(warbandId, characterId);
        return warband.Experience >= xpCost;
    }

    public async Task LevelUpIncrementStatAsync(string warbandId, string characterId, string statName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Check if character can level up
        var xpCost = await GetCharacterXpCostAsync(warbandId, characterId);
        if (warband.Experience < xpCost)
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost} XP, have {warband.Experience} XP");

        // Increment the stat
        switch (statName.ToLower())
        {
            case "agility":
                character.Stats.Agility++;
                break;
            case "presence":
                character.Stats.Presence++;
                break;
            case "strength":
                character.Stats.Strength++;
                break;
            case "toughness":
                character.Stats.Toughness++;
                // Update current HP if max HP increased
                if (character.CurrentHP < character.Stats.HP)
                {
                    character.CurrentHP = character.Stats.HP;
                }
                break;
            default:
                throw new ArgumentException($"Invalid stat name: {statName}");
        }

        // Deduct XP
        warband.Experience -= xpCost;
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task<string> LevelUpRerollFlawAsync(string warbandId, string characterId, string oldFlawName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Check if character can level up
        var xpCost = await GetCharacterXpCostAsync(warbandId, characterId);
        if (warband.Experience < xpCost)
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost} XP, have {warband.Experience} XP");

        // Remove old flaw
        if (character.Flaws.Contains(oldFlawName))
        {
            character.Flaws.Remove(oldFlawName);
        }

        // Roll new flaw using the FeatFlawService directly
        var newFlaw = await _featFlawService.RollFlawAsync(warband.GameVariant);
        character.Flaws.Add(newFlaw.Name);

        // Deduct XP
        warband.Experience -= xpCost;
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);

        return newFlaw.Name;
    }

    public async Task LevelUpGainFeatAsync(string warbandId, string characterId, string featName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Check if character can level up
        var xpCost = await GetCharacterXpCostAsync(warbandId, characterId);
        if (warband.Experience < xpCost)
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost} XP, have {warband.Experience} XP");

        // Add feat
        await AddFeatAsync(warbandId, characterId, featName);

        // Deduct XP
        warband = await GetWarbandAsync(warbandId); // Reload after AddFeatAsync
        warband!.Experience -= xpCost;
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task LevelUpRemoveInjuryAsync(string warbandId, string characterId, string injuryName)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        // Check if character can level up
        var xpCost = await GetCharacterXpCostAsync(warbandId, characterId);
        if (warband.Experience < xpCost)
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost} XP, have {warband.Experience} XP");

        // Remove injury
        await RemoveInjuryAsync(warbandId, characterId, injuryName);

        // Deduct XP
        warband = await GetWarbandAsync(warbandId); // Reload after RemoveInjuryAsync
        warband!.Experience -= xpCost;
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Warband XP Spending

    public async Task ResurrectMemberAsync(string warbandId, string deadMemberId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        // Find the dead member
        var deadMember = warband.DeadMembers.FirstOrDefault(c => c.Id == deadMemberId);
        if (deadMember == null)
            throw new InvalidOperationException("Dead member not found");

        // Check XP cost
        var xpCost = await _campaignProgressionService.GetBaseXpCostAsync();
        if (warband.Experience < xpCost)
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost} XP, have {warband.Experience} XP");

        // Move from DeadMembers to Members
        warband.DeadMembers.Remove(deadMember);
        warband.Members.Add(deadMember);

        // Add a new random flaw (consequence of resurrection)
        // Keep rolling until we get a flaw they don't already have
        var allFlaws = await _featFlawService.GetFlawsAsync(warband.GameVariant);
        var maxAttempts = 100; // Safety limit to prevent infinite loop
        var attempts = 0;

        while (attempts < maxAttempts)
        {
            var newFlaw = await _featFlawService.RollFlawAsync(warband.GameVariant);
            if (!deadMember.Flaws.Contains(newFlaw.Name))
            {
                deadMember.Flaws.Add(newFlaw.Name);
                break;
            }
            attempts++;
        }

        // If we couldn't find a unique flaw after max attempts (character has almost all flaws),
        // just add a random one from the available flaws
        if (attempts >= maxAttempts)
        {
            var availableFlaws = allFlaws.Where(f => !deadMember.Flaws.Contains(f.Name)).ToList();
            if (availableFlaws.Any())
            {
                var random = new Random();
                var newFlaw = availableFlaws[random.Next(availableFlaws.Count)];
                deadMember.Flaws.Add(newFlaw.Name);
            }
        }

        // Deduct XP
        warband.Experience -= xpCost;
        warband.UpdateLastModified();

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Ammo Management

    public async Task AddAmmoToCharacterAsync(string warbandId, string characterId, Equipment ammo)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        if (!ammo.IsAmmo)
            throw new InvalidOperationException("Item is not ammo");

        // Auto-merge: Find existing ammo of same type
        var existingAmmo = character.Equipment.FirstOrDefault(e =>
            e.IsAmmo && e.Name.Equals(ammo.Name, StringComparison.OrdinalIgnoreCase));

        if (existingAmmo != null)
        {
            // Merge shots into existing ammo
            existingAmmo.CurrentShots += ammo.CurrentShots;
            // Slots will auto-calculate via CalculateRequiredSlots()
        }
        else
        {
            // Add as new ammo item
            character.Equipment.Add(ammo);
        }

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task ReloadWeaponAsync(string warbandId, string characterId, string weaponId, string ammoId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var weapon = character.Equipment.FirstOrDefault(e => e.Id == weaponId);
        if (weapon == null || !weapon.IsWeapon)
            throw new InvalidOperationException("Weapon not found");

        if (!weapon.RequiresAmmo)
            throw new InvalidOperationException("Weapon does not require ammo");

        var ammo = character.Equipment.FirstOrDefault(e => e.Id == ammoId);
        if (ammo == null || !ammo.IsAmmo)
            throw new InvalidOperationException("Ammo not found");

        // Check if ammo is compatible
        if (!weapon.AmmoType!.Equals(ammo.Name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Incompatible ammo. Weapon requires {weapon.AmmoType}, but trying to use {ammo.Name}");

        // CRITICAL: Reload adds a FULL magazine (weapon.MaxShots) to the weapon
        // Example: 4/5 + reload = 9/10 (adds 5 shots)
        // This allows stacking multiple magazines in the weapon
        int shotsToTransfer = Math.Min(weapon.MaxShots, ammo.CurrentShots);

        // Transfer shots
        weapon.CurrentShots += shotsToTransfer;
        ammo.CurrentShots -= shotsToTransfer;

        // Remove ammo if depleted
        if (ammo.CurrentShots <= 0)
        {
            character.Equipment.Remove(ammo);
        }

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task ConsumeAmmoShotAsync(string warbandId, string characterId, string weaponId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var weapon = character.Equipment.FirstOrDefault(e => e.Id == weaponId);
        if (weapon == null || !weapon.IsWeapon)
            throw new InvalidOperationException("Weapon not found");

        if (weapon.CurrentShots <= 0)
            throw new InvalidOperationException("Weapon is out of ammo");

        weapon.CurrentShots--;

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task BuyAmmoForWeaponAsync(string warbandId, string characterId, string weaponId, string traderId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var weapon = character.Equipment.FirstOrDefault(e => e.Id == weaponId);
        if (weapon == null || !weapon.IsWeapon)
            throw new InvalidOperationException("Weapon not found");

        if (!weapon.RequiresAmmo)
            throw new InvalidOperationException($"{weapon.Name} does not require ammo");

        // Find ammo equipment in catalog
        var ammoEquipment = await _equipmentService.GetEquipmentAsync(warband.GameVariant, "item");
        var ammoItem = ammoEquipment.FirstOrDefault(e =>
            e.Type == "ammo" && e.Name.Equals(weapon.AmmoType, StringComparison.OrdinalIgnoreCase));

        if (ammoItem == null)
            throw new InvalidOperationException($"Ammo type '{weapon.AmmoType}' not found in equipment catalog");

        // Get trader and calculate price
        var trader = await _traderService.GetTraderByIdAsync(traderId, warband.GameVariant);
        if (trader == null)
            throw new InvalidOperationException($"Trader not found: {traderId}");

        int finalPrice = trader.CalculateBuyPrice(ammoItem.Cost);

        // Check if warband has enough gold
        if (warband.Gold < finalPrice)
            throw new InvalidOperationException($"Insufficient gold. Need {finalPrice}G, have {warband.Gold}G");

        // Create new ammo equipment instance with shots from catalog
        var newAmmo = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = ammoItem.Name,
            Type = "ammo",
            Cost = ammoItem.Cost,
            Slots = ammoItem.Slots,
            IconClass = ammoItem.IconClass,
            CurrentShots = ammoItem.MaxShots, // Use MaxShots as the initial CurrentShots
            MaxShots = ammoItem.MaxShots
        };

        // Try to add to character (with auto-merge) - this saves the warband
        await AddAmmoToCharacterAsync(warbandId, characterId, newAmmo);

        // CRITICAL: Reload warband to get fresh references after AddAmmoToCharacterAsync saved
        warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found after adding ammo");

        character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found after adding ammo");

        // Auto-reload the weapon from the newly purchased ammo
        var characterAmmo = character.Equipment.FirstOrDefault(e =>
            e.Type == "ammo" && e.Name.Equals(weapon.AmmoType, StringComparison.OrdinalIgnoreCase));

        if (characterAmmo != null)
        {
            // Reload weapon - this also saves the warband
            await ReloadWeaponAsync(warbandId, characterId, weaponId, characterAmmo.Id);
        }

        // CRITICAL: Reload AGAIN after ReloadWeaponAsync to get fresh state
        warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found after reload");

        // Deduct cost (we do this after reload so we don't lose the reload changes)
        warband.Gold -= finalPrice;

        // Final save with gold deduction
        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task UnloadAmmoFromWeaponAsync(string warbandId, string characterId, string weaponId, int shotsToUnload)
    {
        if (shotsToUnload <= 0)
            throw new ArgumentException("Must unload at least 1 shot", nameof(shotsToUnload));

        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var weapon = character.Equipment.FirstOrDefault(e => e.Id == weaponId);
        if (weapon == null)
            throw new InvalidOperationException("Weapon not found");

        if (!weapon.IsWeapon || !weapon.RequiresAmmo)
            throw new InvalidOperationException($"{weapon.Name} is not a weapon that uses ammo");

        if (weapon.CurrentShots <= 0)
            throw new InvalidOperationException($"{weapon.Name} has no ammo to unload");

        // Calculate actual shots to unload (can't unload more than available)
        int actualShotsToUnload = Math.Min(shotsToUnload, weapon.CurrentShots);

        // Check if character already has matching ammo item
        var existingAmmo = character.Equipment.FirstOrDefault(e =>
            e.Type == "ammo" && e.Name.Equals(weapon.AmmoType, StringComparison.OrdinalIgnoreCase));

        if (existingAmmo != null)
        {
            // Add to existing ammo stack
            existingAmmo.CurrentShots += actualShotsToUnload;
        }
        else
        {
            // Load ammo details from equipment service to get proper icon and properties
            var ammoItems = await _equipmentService.GetEquipmentAsync(warband.GameVariant, "ammo");
            var ammoTemplate = ammoItems.FirstOrDefault(a =>
                a.Name.Equals(weapon.AmmoType, StringComparison.OrdinalIgnoreCase));

            if (ammoTemplate == null)
                throw new InvalidOperationException($"Ammo type '{weapon.AmmoType}' not found");

            // Create new ammo item
            var newAmmo = new Equipment
            {
                Id = Guid.NewGuid().ToString(),
                Name = ammoTemplate.Name,
                Type = "ammo",
                CurrentShots = actualShotsToUnload,
                MaxShots = ammoTemplate.MaxShots,
                Slots = ammoTemplate.Slots,
                Cost = ammoTemplate.Cost,
                IconClass = ammoTemplate.IconClass,
                Effect = ammoTemplate.Effect
            };

            character.Equipment.Add(newAmmo);
        }

        // Remove shots from weapon
        weapon.CurrentShots -= actualShotsToUnload;

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Consumable Management

    public async Task UseConsumableAsync(string warbandId, string characterId, string equipmentId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        var character = warband.Members.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found");

        var consumable = character.Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (consumable == null)
            throw new InvalidOperationException("Consumable not found");

        if (!consumable.IsConsumable)
            throw new InvalidOperationException("Item is not consumable");

        // Apply effect based on ConsumableEffect
        bool effectApplied = false;
        switch (consumable.ConsumableEffect?.ToLowerInvariant())
        {
            case "cure_bleeding":
                if (character.Injuries.Contains("Bleeding"))
                {
                    character.Injuries.Remove("Bleeding");
                    effectApplied = true;
                }
                break;

            case "cure_poisoned":
                if (character.Injuries.Contains("Poisoned"))
                {
                    character.Injuries.Remove("Poisoned");
                    effectApplied = true;
                }
                break;

            case "cure_disease":
                if (character.Injuries.Contains("Disease"))
                {
                    character.Injuries.Remove("Disease");
                    effectApplied = true;
                }
                break;

            case "heal_hp":
                // Roll D6 for healing (simplified: just heal 3-4 HP)
                var random = new Random();
                int healAmount = random.Next(1, 7); // 1-6 HP
                int maxHP = character.Stats.HP;
                int oldHP = character.CurrentHP;
                character.CurrentHP = Math.Min(oldHP + healAmount, maxHP);
                effectApplied = true;
                break;

            case "prevent_gas":
                // This is a preventive effect - for now just mark as used
                // Could add a temporary status effect system later
                effectApplied = true;
                break;

            default:
                // Unknown effect or null - still consume the item
                break;
        }

        // Remove consumable from equipment (always, whether effect applied or not)
        character.Equipment.Remove(consumable);

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Custom Items Management

    public async Task CreateCustomItemAsync(string warbandId, Equipment customItem)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        // Ensure item is marked as custom and has proper type
        customItem.IsCustom = true;
        if (string.IsNullOrEmpty(customItem.Type))
        {
            customItem.Type = "item";
        }

        // Generate new ID if not set
        if (string.IsNullOrEmpty(customItem.Id))
        {
            customItem.Id = Guid.NewGuid().ToString();
        }

        // Add to warband's custom items collection
        warband.CustomItems.Add(customItem);

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    public async Task<List<Equipment>> GetCustomItemsAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        return warband.CustomItems;
    }

    public async Task DeleteCustomItemAsync(string warbandId, string itemId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException("Warband not found");

        // Check if item is in use (in any character's equipment or in stash)
        bool inUse = warband.Members.Any(m => m.Equipment.Any(e => e.Id == itemId)) ||
                     warband.Stash.Any(e => e.Id == itemId);

        if (inUse)
        {
            throw new InvalidOperationException("Cannot delete custom item that is currently in use. Remove it from all characters and stash first.");
        }

        // Find and remove from custom items
        var customItem = warband.CustomItems.FirstOrDefault(i => i.Id == itemId);
        if (customItem == null)
        {
            throw new InvalidOperationException("Custom item not found");
        }

        warband.CustomItems.Remove(customItem);

        warband.UpdateLastModified();
        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(warbandId);
    }

    #endregion

    #region Import/Export Management

    public async Task<string> ExportWarbandAsJsonAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new InvalidOperationException($"Warband not found: {warbandId}");

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var json = System.Text.Json.JsonSerializer.Serialize(warband, options);
        return json;
    }

    public async Task<string> ImportWarbandFromJsonAsync(string json)
    {
        // Validate JSON first
        var isValid = await ValidateWarbandJsonAsync(json);
        if (!isValid)
        {
            throw new InvalidOperationException("Invalid warband JSON format");
        }

        Warband? importedWarband;
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            importedWarband = System.Text.Json.JsonSerializer.Deserialize<Warband>(json, options);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new System.Text.Json.JsonException("Failed to deserialize warband JSON", ex);
        }

        if (importedWarband == null)
        {
            throw new InvalidOperationException("Failed to deserialize warband");
        }

        // Validate required fields
        if (string.IsNullOrEmpty(importedWarband.Id))
        {
            throw new InvalidOperationException("Warband must have an Id");
        }

        if (string.IsNullOrEmpty(importedWarband.GameVariant))
        {
            throw new InvalidOperationException("Warband must have a GameVariant");
        }

        // Check if warband with this ID already exists - if so, replace it
        var existingWarband = await GetWarbandAsync(importedWarband.Id);
        if (existingWarband != null)
        {
            // Delete existing warband (user confirmed they want to replace)
            _state.Warbands.Remove(importedWarband.Id);
        }

        // Add the imported warband to state
        _state.Warbands[importedWarband.Id] = importedWarband;

        // Update last modified timestamp
        importedWarband.UpdateLastModified();

        // Save to repository
        await _warbandRepository.SaveAsync(importedWarband);

        // Notify state changes
        _state.NotifyStateChanged();
        _state.NotifyWarbandChanged(importedWarband.Id);

        return importedWarband.Id;
    }

    public async Task<bool> ValidateWarbandJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var warband = System.Text.Json.JsonSerializer.Deserialize<Warband>(json, options);

            // Validate required fields
            if (warband == null)
                return false;

            if (string.IsNullOrEmpty(warband.Id))
                return false;

            if (string.IsNullOrEmpty(warband.GameVariant))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Pet Management

    public async Task<List<Equipment>> GetAvailablePetsAsync(string gameVariant)
    {
        return await _petService.GetAvailablePetsAsync(gameVariant);
    }

    public async Task<Equipment?> GetWarbandPetAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        return warband.Pet;
    }

    public async Task<bool> CanWarbandHavePetAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        // A warband can have a pet if it doesn't already have one
        return warband.Pet == null;
    }

    public async Task AddPetToWarbandAsync(string warbandId, string petId, Equipment pet)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        // Check if warband already has a pet
        if (warband.Pet != null)
            throw new InvalidOperationException("Warband already has a pet");

        // Apply animal lover feat discount if applicable
        var petCost = CalculatePetCostWithFeatModifiers(pet, warband);

        // Check if warband has sufficient gold for actual cost (after discounts)
        if (warband.Gold < petCost)
            throw new InvalidOperationException($"Insufficient gold to buy pet. Required: {petCost}, Available: {warband.Gold}");

        // Deduct gold
        warband.Gold -= petCost;

        // Add pet to warband (create a copy with a new ID)
        warband.Pet = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = pet.Name,
            Type = pet.Type,
            Category = pet.Category,
            Damage = pet.Damage,
            Range = pet.Range,
            Properties = pet.Properties,
            Stat = pet.Stat,
            Cost = petCost, // Store actual cost paid (with feat modifiers applied)
            Slots = pet.Slots,
            IconClass = pet.IconClass,
            Effect = pet.Effect,
            RollRange = pet.RollRange,
            ArmorValue = pet.ArmorValue,
            ArmorType = pet.ArmorType,
            Special = pet.Special,
            Restrictions = pet.Restrictions,
            Effects = pet.Effects,
            StatModifierStrength = pet.StatModifierStrength,
            StatModifierAgility = pet.StatModifierAgility,
            StatModifierPresence = pet.StatModifierPresence,
            StatModifierToughness = pet.StatModifierToughness,
            IsEquipped = false,
            EquippedItems = new List<Equipment>()
        };

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public async Task RemovePetFromWarbandAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (warband.Pet == null)
            return; // No pet to remove

        // Refund half the pet cost (rounded down)
        var refund = warband.Pet.Cost / 2;
        warband.Gold += refund;

        // Remove the pet
        warband.Pet = null;

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public async Task MarkPetDeadAsync(string warbandId, Equipment pet)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (warband.Pet == null)
            return; // No active pet to mark dead

        // Move pet to dead pets
        warband.DeadPet = warband.Pet;
        warband.Pet = null;

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public async Task FirePetAsync(string warbandId, Equipment pet)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (warband.Pet == null)
            return; // No active pet to fire

        // Refund half the pet cost (rounded down)
        var refund = warband.Pet.Cost / 2;
        warband.Gold += refund;

        // Move pet to fired pets
        warband.FiredPet = warband.Pet;
        warband.Pet = null;

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public async Task EquipPetArmorAsync(string warbandId, Equipment armor)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (warband.Pet == null)
            throw new InvalidOperationException("Warband has no pet to equip armor on");

        // Check if warband has sufficient gold
        if (warband.Gold < armor.Cost)
            throw new InvalidOperationException($"Insufficient gold to buy pet armor. Required: {armor.Cost}, Available: {warband.Gold}");

        // Deduct gold
        warband.Gold -= armor.Cost;

        // Create armor copy
        var armorCopy = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = armor.Name,
            Type = armor.Type,
            Category = armor.Category,
            Damage = armor.Damage,
            Range = armor.Range,
            Properties = armor.Properties,
            Stat = armor.Stat,
            Cost = armor.Cost,
            Slots = armor.Slots,
            IconClass = armor.IconClass,
            Effect = armor.Effect,
            RollRange = armor.RollRange,
            ArmorValue = armor.ArmorValue,
            ArmorType = armor.ArmorType,
            Special = armor.Special,
            Restrictions = armor.Restrictions,
            Effects = armor.Effects,
            IsEquipped = true
        };

        warband.Pet.EquippedItems.Add(armorCopy);

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public async Task UnequipPetArmorAsync(string warbandId)
    {
        var warband = await GetWarbandAsync(warbandId);
        if (warband == null)
            throw new ArgumentException($"Warband not found: {warbandId}");

        if (warband.Pet == null)
            throw new InvalidOperationException("Warband has no pet");

        if (warband.Pet.EquippedItems.Count == 0)
            return; // No equipment to unequip

        // Remove equipped armor (typically there's only one piece)
        var equippedArmor = warband.Pet.EquippedItems.FirstOrDefault(e => e.IsEquipped);
        if (equippedArmor != null)
        {
            warband.Pet.EquippedItems.Remove(equippedArmor);
            // Refund half the armor cost
            warband.Gold += equippedArmor.Cost / 2;
        }

        await _warbandRepository.SaveAsync(warband);
        _state.NotifyStateChanged();
    }

    public int CalculatePetCostWithFeatModifiers(Equipment pet, Warband warband)
    {
        if (pet == null)
            return 0;

        var baseCost = pet.Cost;

        // Check if any character in the warband has the Animal Lover feat
        var hasAnimalLover = warband.Members.Any(c =>
            c.Feats.Any(f => f.Equals("Animal Lover", StringComparison.OrdinalIgnoreCase)));

        if (hasAnimalLover)
        {
            // Animal Lover feat reduces pet cost by half (rounded down)
            baseCost = baseCost / 2;
        }

        return baseCost;
    }

    #endregion
}