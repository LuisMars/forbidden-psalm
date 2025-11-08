using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Data.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace ForbiddenPsalmBuilder.Core.Services;

/// <summary>
/// Models for deserializing pets-system.json file structure
/// </summary>
public class PetSystemData
{
    [JsonPropertyName("availablePets")]
    public List<PetDefinition> AvailablePets { get; set; } = new();
}

public class PetDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public object Cost { get; set; } = 0; // Can be int or string like "D4×D4×D4×D4 Gold"

    [JsonPropertyName("stats")]
    public PetStats Stats { get; set; } = new();

    [JsonPropertyName("attack")]
    public PetAttack? Attack { get; set; }

    [JsonPropertyName("special")]
    public List<string> Special { get; set; } = new();

    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;
}

public class PetStats
{
    [JsonPropertyName("strength")]
    public object Strength { get; set; } = 0;

    [JsonPropertyName("agility")]
    public object Agility { get; set; } = 0;

    [JsonPropertyName("presence")]
    public object Presence { get; set; } = 0;

    [JsonPropertyName("toughness")]
    public object Toughness { get; set; } = 0;
}

public class PetAttack
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("damage")]
    public string Damage { get; set; } = string.Empty;

    [JsonPropertyName("stat")]
    public string Stat { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<string> Properties { get; set; } = new();

    [JsonPropertyName("special")]
    public string? Special { get; set; }
}

/// <summary>
/// Service for managing pet data and operations.
/// Loads pet information from embedded JSON resources and provides caching.
/// </summary>
public class PetService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly ILogger<PetService>? _logger;
    private readonly Dictionary<string, List<Equipment>> _petCache = new();

    /// <summary>
    /// Initializes a new instance of the PetService.
    /// </summary>
    /// <param name="resourceService">Service for loading embedded resources</param>
    /// <param name="logger">Optional logger for debugging</param>
    public PetService(IEmbeddedResourceService resourceService, ILogger<PetService>? logger = null)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _logger = logger;
    }

    /// <summary>
    /// Gets all pets available for a specific game variant.
    /// Results are cached after first load.
    /// </summary>
    /// <param name="gameVariant">The game variant (e.g., "end-times", "28-psalms", "last-war")</param>
    /// <returns>List of available pets, or empty list if none found or error occurs</returns>
    public async Task<List<Equipment>> GetPetsAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Equipment>();

        // Check cache first
        if (_petCache.ContainsKey(gameVariant))
        {
            _logger?.LogDebug("Returning cached pets for variant {GameVariant}", gameVariant);
            return _petCache[gameVariant];
        }

        try
        {
            // Load from pets-system.json
            var petSystem = await _resourceService.GetGameResourceAsync<PetSystemData>(
                gameVariant,
                "pets-system.json"
            );

            if (petSystem?.AvailablePets == null || petSystem.AvailablePets.Count == 0)
            {
                _logger?.LogWarning("No pets data found for variant {GameVariant}", gameVariant);
                return new List<Equipment>();
            }

            // Convert PetDefinitions to Equipment objects
            var pets = new List<Equipment>();
            foreach (var petDef in petSystem.AvailablePets)
            {
                var equipment = ConvertPetDefinitionToEquipment(petDef);
                pets.Add(equipment);
            }

            // Cache the results
            _petCache[gameVariant] = pets;
            _logger?.LogInformation("Loaded and cached {PetCount} pets for variant {GameVariant}",
                pets.Count, gameVariant);

            return pets;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading pets for game variant {GameVariant}", gameVariant);
            return new List<Equipment>();
        }
    }

    /// <summary>
    /// Converts a PetDefinition from pets-system.json to an Equipment object.
    /// </summary>
    private Equipment ConvertPetDefinitionToEquipment(PetDefinition petDef)
    {
        // Extract numeric values from stats (handle both int and string types like "D4")
        int strength = ExtractNumericValue(petDef.Stats.Strength);
        int agility = ExtractNumericValue(petDef.Stats.Agility);
        int presence = ExtractNumericValue(petDef.Stats.Presence);
        int toughness = ExtractNumericValue(petDef.Stats.Toughness);

        // Extract numeric cost (handle "D4×D4×D4×D4 Gold" strings by returning 0)
        int cost = ExtractNumericCost(petDef.Cost);

        return new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = petDef.Name,
            Type = "pet",
            Category = "companion",
            Cost = cost,
            Slots = 1,
            Damage = petDef.Attack?.Damage,
            Stat = petDef.Attack?.Stat,
            Properties = petDef.Attack?.Properties ?? new List<string>(),
            Special = petDef.Attack?.Special,
            Effect = string.Join(", ", petDef.Special),
            IconClass = GetIconClassForPet(petDef.Name),
            ArmorType = "pet",
            ArmorValue = 0,
            StatModifierStrength = strength,
            StatModifierAgility = agility,
            StatModifierPresence = presence,
            StatModifierToughness = toughness,
            IsEquipped = false,
            EquippedItems = new List<Equipment>()
        };
    }

    /// <summary>
    /// Extracts numeric value from stats (handles int or string like "D4").
    /// </summary>
    private int ExtractNumericValue(object value)
    {
        if (value == null)
            return 0;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return (int)longValue;

        if (value is decimal decValue)
            return (int)decValue;

        if (value is string strValue)
        {
            // Try to parse as int
            if (int.TryParse(strValue, out int result))
                return result;

            // For dice strings like "D4", return 0 (will need manual assignment)
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Extracts numeric cost from pets (handles both int and string representations).
    /// </summary>
    private int ExtractNumericCost(object cost)
    {
        if (cost == null)
            return 0;

        if (cost is int intValue)
            return intValue;

        if (cost is long longValue)
            return (int)longValue;

        if (cost is decimal decValue)
            return (int)decValue;

        if (cost is string strValue)
        {
            // Try to parse as int
            if (int.TryParse(strValue, out int result))
                return result;

            // For complex costs like "D4×D4×D4×D4 Gold", return 0 (Chaos Beast - not purchasable directly)
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Gets an appropriate Font Awesome icon class for a pet based on its name.
    /// </summary>
    private string GetIconClassForPet(string petName)
    {
        return petName.ToLower() switch
        {
            var s when s.Contains("dog") => "fas fa-dog",
            var s when s.Contains("snail") || s.Contains("slug") => "fas fa-snail",
            var s when s.Contains("rock") => "fas fa-mountain",
            var s when s.Contains("mount") || s.Contains("horse") => "fas fa-horse",
            var s when s.Contains("hound") => "fas fa-dog",
            var s when s.Contains("eagle") || s.Contains("hawk") => "fas fa-eagle",
            var s when s.Contains("snake") => "fas fa-worm",
            var s when s.Contains("beetle") || s.Contains("bug") => "fas fa-bug",
            var s when s.Contains("rifle") => "fas fa-gun",
            var s when s.Contains("chaos") => "fas fa-skull",
            _ => "fas fa-paw"
        };
    }

    /// <summary>
    /// Gets a specific pet by its ID for a given game variant.
    /// </summary>
    /// <param name="id">The pet's ID</param>
    /// <param name="gameVariant">The game variant</param>
    /// <returns>The pet if found, null otherwise</returns>
    public async Task<Equipment?> GetPetByIdAsync(string id, string gameVariant)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(gameVariant))
            return null;

        var pets = await GetPetsAsync(gameVariant);
        return pets.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Gets all pets available for a specific game variant, filtered by availability rules.
    /// </summary>
    /// <param name="gameVariant">The game variant</param>
    /// <returns>List of available pets</returns>
    public async Task<List<Equipment>> GetAvailablePetsAsync(string gameVariant)
    {
        var pets = await GetPetsAsync(gameVariant);

        // For now, all pets are available once loaded
        // Future: Add chapter restrictions, feat-based availability, etc.
        return pets;
    }

    /// <summary>
    /// Calculates the effective cost of a pet, considering modifiers like feats.
    /// </summary>
    /// <param name="pet">The pet to calculate cost for</param>
    /// <param name="hasAnimalLoverFeat">Whether the character has the Animal Lover feat</param>
    /// <returns>The effective cost of the pet</returns>
    public int CalculatePetCost(Equipment pet, bool hasAnimalLoverFeat)
    {
        if (pet == null)
            return 0;

        var baseCost = pet.Cost;

        if (hasAnimalLoverFeat)
        {
            // Animal Lover feat reduces pet cost by half (rounded down)
            baseCost = baseCost / 2;
        }

        return baseCost;
    }

    /// <summary>
    /// Clears the pet cache (useful for testing or cache invalidation).
    /// </summary>
    public void ClearCache()
    {
        _petCache.Clear();
        _logger?.LogDebug("Pet cache cleared");
    }
}
