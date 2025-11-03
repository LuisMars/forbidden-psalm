using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Data.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Services;

public class EquipmentService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly ILogger<EquipmentService>? _logger;
    private Dictionary<string, List<Weapon>> _weaponCache = new();
    private Dictionary<string, List<Armor>> _armorCache = new();
    private Dictionary<string, List<Item>> _itemCache = new();
    private Dictionary<string, List<Equipment>> _relicCache = new();
    private Dictionary<string, List<Equipment>> _unifiedEquipmentCache = new();

    public EquipmentService()
    {
        _resourceService = new EmbeddedResourceService();
    }

    public EquipmentService(IEmbeddedResourceService resourceService, ILogger<EquipmentService>? logger = null)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>
    /// Legacy method - use GetEquipmentAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentAsync() instead for unified equipment loading")]
    public async Task<List<Weapon>> GetWeaponsAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Weapon>();

        // Check cache first
        if (_weaponCache.ContainsKey(gameVariant))
            return _weaponCache[gameVariant];

        try
        {
            var weaponData = await _resourceService.GetGameResourceAsync<WeaponsData>(
                gameVariant,
                "weapons.json"
            );

            if (weaponData == null)
                return new List<Weapon>();

            var weapons = new List<Weapon>();

            // Convert each category
            weapons.AddRange(ConvertWeaponDtos(weaponData.OneHandedMelee, "oneHandedMelee"));
            weapons.AddRange(ConvertWeaponDtos(weaponData.TwoHandedMelee, "twoHandedMelee"));
            weapons.AddRange(ConvertWeaponDtos(weaponData.OneHandedRanged, "oneHandedRanged"));
            weapons.AddRange(ConvertWeaponDtos(weaponData.TwoHandedRanged, "twoHandedRanged"));
            weapons.AddRange(ConvertWeaponDtos(weaponData.Throwables, "throwables"));

            _weaponCache[gameVariant] = weapons;
            return weapons;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading weapons for {GameVariant}", gameVariant);
            return new List<Weapon>();
        }
    }

    private List<Weapon> ConvertWeaponDtos(List<WeaponDto> dtos, string category)
    {
        return dtos.Select(dto => new Weapon
        {
            Id = dto.Name.ToLower().Replace(" ", "-"),
            Name = dto.Name,
            DisplayName = dto.Name,
            Description = $"{dto.Name} - {category}",
            Category = category,
            Damage = dto.Damage,
            Properties = dto.Properties,
            Stat = dto.Stat,
            Cost = dto.Cost,
            Slots = dto.Slots,
            TechLevel = dto.TechLevel,
            IconClass = dto.IconClass ?? "ra ra-sword",
            AmmoType = dto.AmmoType
        })
        .OrderBy(w => GetRollRangeStart(dtos.FirstOrDefault(d => d.Name == w.Name)?.RollRange))
        .ToList();
    }

    private int GetRollRangeStart(string? rollRange)
    {
        if (string.IsNullOrEmpty(rollRange))
            return int.MaxValue; // Items without roll range go to the end

        var parts = rollRange.Split('-');
        return int.TryParse(parts[0], out var start) ? start : int.MaxValue;
    }

    /// <summary>
    /// Legacy method - use GetEquipmentByIdAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentByIdAsync(id, gameVariant, \"weapon\") instead for unified equipment loading")]
    public async Task<Weapon?> GetWeaponByIdAsync(string id, string gameVariant)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(gameVariant))
            return null;

        var weapons = await GetWeaponsAsync(gameVariant);
        return weapons.FirstOrDefault(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Legacy method - use GetEquipmentAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentAsync() instead for unified equipment loading")]
    public async Task<List<Armor>> GetArmorAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Armor>();

        // Check cache first
        if (_armorCache.ContainsKey(gameVariant))
            return _armorCache[gameVariant];

        try
        {
            var armorData = await _resourceService.GetGameResourceAsync<List<object>>(
                gameVariant,
                "armor.json"
            );

            if (armorData == null)
                return new List<Armor>();

            var armorList = new List<Armor>();
            foreach (var armorObj in armorData)
            {
                var armor = ParseArmor(armorObj);
                if (armor != null)
                    armorList.Add(armor);
            }

            _armorCache[gameVariant] = armorList;
            return armorList;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading armor for {GameVariant}", gameVariant);
            return new List<Armor>();
        }
    }

    /// <summary>
    /// Legacy method - use GetEquipmentByIdAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentByIdAsync(id, gameVariant, \"armor\") instead for unified equipment loading")]
    public async Task<Armor?> GetArmorByIdAsync(string id, string gameVariant)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(gameVariant))
            return null;

        var armorList = await GetArmorAsync(gameVariant);
        return armorList.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Legacy method - use GetEquipmentAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentAsync() instead for unified equipment loading")]
    public async Task<List<Item>> GetItemsAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Item>();

        // Check cache first
        if (_itemCache.ContainsKey(gameVariant))
            return _itemCache[gameVariant];

        try
        {
            var equipmentData = await _resourceService.GetGameResourceAsync<Dictionary<string, List<object>>>(
                gameVariant,
                "equipment.json"
            );

            if (equipmentData == null)
                return new List<Item>();

            var items = new List<Item>();

            // Parse items
            if (equipmentData.ContainsKey("items"))
            {
                foreach (var itemObj in equipmentData["items"])
                {
                    var item = ParseItem(itemObj, "item");
                    if (item != null)
                        items.Add(item);
                }
            }

            // Parse ammo
            if (equipmentData.ContainsKey("ammo"))
            {
                foreach (var ammoObj in equipmentData["ammo"])
                {
                    var ammo = ParseItem(ammoObj, "ammo");
                    if (ammo != null)
                        items.Add(ammo);
                }
            }

            _itemCache[gameVariant] = items;
            return items;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading items for {GameVariant}", gameVariant);
            return new List<Item>();
        }
    }

    /// <summary>
    /// Legacy method - use GetEquipmentByIdAsync() instead for unified equipment loading
    /// </summary>
    [Obsolete("Use GetEquipmentByIdAsync(id, gameVariant, \"item\") instead for unified equipment loading")]
    public async Task<Item?> GetItemByIdAsync(string id, string gameVariant)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(gameVariant))
            return null;

        var items = await GetItemsAsync(gameVariant);
        return items.FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Item>> GetCompatibleAmmo(Weapon weapon, string gameVariant)
    {
        if (weapon == null || string.IsNullOrEmpty(gameVariant))
            return new List<Item>();

        // If weapon doesn't require ammo, return empty list
        if (!weapon.RequiresAmmo)
            return new List<Item>();

        // Get all items for the game variant
        var items = await GetItemsAsync(gameVariant);

        // Filter to only ammo items that match the weapon's ammo type
        return items
            .Where(i => i.Type == "ammo" && i.Name.Equals(weapon.AmmoType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }


    private Armor? ParseArmor(object armorObj)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(armorObj)
            );

            if (dict == null) return null;

            var name = dict.ContainsKey("name") ? dict["name"].GetString() ?? "" : "";
            var armor = new Armor
            {
                Id = name.ToLower().Replace(" ", "-"),
                Name = name,
                DisplayName = $"{name} Armor",
                Description = $"{name} armor protection",
                ArmorValue = dict.ContainsKey("armorValue") ? dict["armorValue"].GetInt32() : 0,
                Cost = dict.ContainsKey("cost") ? dict["cost"].GetInt32() : 0,
                Slots = dict.ContainsKey("slots") ? dict["slots"].GetInt32() : 1,
                Special = dict.ContainsKey("special") && dict["special"].ValueKind != JsonValueKind.Null
                    ? dict["special"].GetString()
                    : null
            };

            if (dict.ContainsKey("effects"))
            {
                try
                {
                    armor.Effects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                        dict["effects"].GetRawText()
                    ) ?? new List<Dictionary<string, object>>();
                }
                catch { }
            }

            if (dict.ContainsKey("restrictions"))
            {
                try
                {
                    armor.Restrictions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                        dict["restrictions"].GetRawText()
                    ) ?? new List<Dictionary<string, object>>();
                }
                catch { }
            }

            return armor;
        }
        catch
        {
            return null;
        }
    }

    private Item? ParseItem(object itemObj, string type)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(itemObj)
            );

            if (dict == null) return null;

            var name = type == "ammo"
                ? (dict.ContainsKey("type") ? dict["type"].GetString() ?? "" : "")
                : (dict.ContainsKey("name") ? dict["name"].GetString() ?? "" : "");

            var item = new Item
            {
                Id = name.ToLower().Replace(" ", "-"),
                Name = name,
                DisplayName = name,
                Description = type == "ammo" ? $"{name} ammunition" : name,
                Type = type,
                Cost = dict.ContainsKey("cost") ? dict["cost"].GetInt32() : 0,
                Slots = dict.ContainsKey("slots") ? dict["slots"].GetInt32() : 1,
                Effect = dict.ContainsKey("effect") ? dict["effect"].GetString() : null,
                Shots = dict.ContainsKey("shots") ? dict["shots"].GetInt32() : null,
                TechLevel = dict.ContainsKey("techLevel") ? dict["techLevel"].GetString() : null
            };

            return item;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Equipment>> GetRelicsAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Equipment>();

        // Check cache first
        if (_relicCache.ContainsKey(gameVariant))
            return _relicCache[gameVariant];

        try
        {
            var relics = await _resourceService.GetGameResourceAsync<List<Equipment>>(
                gameVariant,
                "relics.json"
            );

            if (relics == null)
                return new List<Equipment>();

            _relicCache[gameVariant] = relics;
            return relics;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading relics for {GameVariant}", gameVariant);
            return new List<Equipment>();
        }
    }

    /// <summary>
    /// Gets equipment of a specific type or all equipment if type is null.
    /// This is the unified method for loading equipment across all types.
    /// </summary>
    /// <param name="gameVariant">The game variant (28-psalms, end-times, last-war)</param>
    /// <param name="type">The equipment type filter (weapon, armor, item, relic) or null for all</param>
    /// <returns>List of equipment matching the criteria</returns>
    public async Task<List<Equipment>> GetEquipmentAsync(string gameVariant, string? type = null)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<Equipment>();

        var cacheKey = $"{gameVariant}:{type ?? "all"}";

        // Check cache first
        if (_unifiedEquipmentCache.ContainsKey(cacheKey))
            return _unifiedEquipmentCache[cacheKey];

        try
        {
            var equipment = new List<Equipment>();

            // If type is specified, load only that type
            if (!string.IsNullOrEmpty(type))
            {
                equipment = await LoadEquipmentByType(gameVariant, type);
            }
            else
            {
                // Load all types
                var weapons = await LoadEquipmentByType(gameVariant, "weapon");
                var armor = await LoadEquipmentByType(gameVariant, "armor");
                var items = await LoadEquipmentByType(gameVariant, "item");
                var relics = await LoadEquipmentByType(gameVariant, "relic");

                equipment.AddRange(weapons);
                equipment.AddRange(armor);
                equipment.AddRange(items);
                equipment.AddRange(relics);
            }

            _unifiedEquipmentCache[cacheKey] = equipment;
            return equipment;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading equipment for {GameVariant}, type: {Type}", gameVariant, type ?? "all");
            return new List<Equipment>();
        }
    }

    /// <summary>
    /// Gets a specific equipment item by ID and type.
    /// </summary>
    /// <param name="id">The equipment ID</param>
    /// <param name="gameVariant">The game variant</param>
    /// <param name="type">The equipment type (weapon, armor, item, relic)</param>
    /// <returns>The equipment item or null if not found</returns>
    public async Task<Equipment?> GetEquipmentByIdAsync(string id, string gameVariant, string type)
    {
        var equipment = await GetEquipmentAsync(gameVariant, type);
        return equipment.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// Helper method to load equipment by type from appropriate JSON file and convert to unified Equipment model.
    /// </summary>
    private async Task<List<Equipment>> LoadEquipmentByType(string gameVariant, string type)
    {
        try
        {
            return type.ToLower() switch
            {
                "weapon" => await LoadWeaponsAsEquipment(gameVariant),
                "armor" => await LoadArmorAsEquipment(gameVariant),
                "item" => await LoadItemsAsEquipment(gameVariant),
                "ammo" => await LoadItemsAsEquipment(gameVariant), // Ammo is loaded as part of items
                "relic" => await LoadRelicsAsEquipment(gameVariant),
                _ => throw new ArgumentException($"Unknown equipment type: {type}")
            };
        }
        catch
        {
            return new List<Equipment>();
        }
    }

    private async Task<List<Equipment>> LoadWeaponsAsEquipment(string gameVariant)
    {
        var weaponData = await _resourceService.GetGameResourceAsync<WeaponsData>(gameVariant, "weapons.json");
        if (weaponData == null) return new List<Equipment>();

        var equipment = new List<Equipment>();

        // Convert all weapon categories to Equipment
        equipment.AddRange(ConvertWeaponDtosToEquipment(weaponData.OneHandedMelee, "oneHandedMelee"));
        equipment.AddRange(ConvertWeaponDtosToEquipment(weaponData.TwoHandedMelee, "twoHandedMelee"));
        equipment.AddRange(ConvertWeaponDtosToEquipment(weaponData.OneHandedRanged, "oneHandedRanged"));
        equipment.AddRange(ConvertWeaponDtosToEquipment(weaponData.TwoHandedRanged, "twoHandedRanged"));
        equipment.AddRange(ConvertWeaponDtosToEquipment(weaponData.Throwables, "throwables"));

        return equipment;
    }

    private List<Equipment> ConvertWeaponDtosToEquipment(List<WeaponDto> dtos, string category)
    {
        return dtos.Select(dto =>
        {
            var equipment = new Equipment
            {
                Id = dto.Name.ToLower().Replace(" ", "-"),
                Name = dto.Name,
                Type = "weapon",
                Category = category,
                Damage = dto.Damage,
                Properties = dto.Properties,
                Stat = dto.Stat,
                Cost = dto.Cost ?? 0,
                Slots = dto.Slots,
                IconClass = dto.IconClass,
                AmmoType = dto.AmmoType,
                RollRange = dto.RollRange,
                Special = dto.Special
            };

            // Set MaxShots for weapons that require ammo
            // All ranged weapons come with 5 Ammo except Cannon (which comes with 1 Cannonball)
            if (!string.IsNullOrEmpty(dto.AmmoType))
            {
                equipment.MaxShots = dto.AmmoType.Equals("Cannonball", StringComparison.OrdinalIgnoreCase) ? 1 : 5;
            }

            return equipment;
        }).ToList();
    }

    private async Task<List<Equipment>> LoadArmorAsEquipment(string gameVariant)
    {
        var armorData = await _resourceService.GetGameResourceAsync<List<object>>(gameVariant, "armor.json");
        if (armorData == null) return new List<Equipment>();

        return armorData.Select(obj =>
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(obj)
            ) ?? new Dictionary<string, JsonElement>();

            var name = dict.ContainsKey("name") ? dict["name"].GetString() ?? "" : "";

            // Parse restrictions if present
            var restrictions = new List<Dictionary<string, object>>();
            if (dict.ContainsKey("restrictions"))
            {
                try
                {
                    restrictions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                        dict["restrictions"].GetRawText()
                    ) ?? new List<Dictionary<string, object>>();
                }
                catch
                {
                    // If deserialization fails, leave restrictions empty
                }
            }

            return new Equipment
            {
                Id = name.ToLower().Replace(" ", "-"),
                Name = name,
                Type = "armor",
                ArmorValue = dict.ContainsKey("armorValue") ? dict["armorValue"].GetInt32() : 0,
                Cost = dict.ContainsKey("cost") && dict["cost"].ValueKind == JsonValueKind.Number
                    ? dict["cost"].GetInt32() : 0,
                Slots = dict.ContainsKey("slots") ? dict["slots"].GetInt32() : 0,
                ArmorType = dict.ContainsKey("armorType") ? dict["armorType"].GetString() : null,
                Special = dict.ContainsKey("special") ? dict["special"].GetString() : null,
                IconClass = dict.ContainsKey("iconClass") ? dict["iconClass"].GetString() : null,
                Restrictions = restrictions
            };
        }).ToList();
    }

    private async Task<List<Equipment>> LoadItemsAsEquipment(string gameVariant)
    {
        var itemData = await _resourceService.GetGameResourceAsync<Dictionary<string, List<object>>>(
            gameVariant, "equipment.json"
        );
        if (itemData == null) return new List<Equipment>();

        var equipment = new List<Equipment>();

        // Process regular items
        if (itemData.ContainsKey("items"))
        {
            equipment.AddRange(itemData["items"].Select(obj =>
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    JsonSerializer.Serialize(obj)
                ) ?? new Dictionary<string, JsonElement>();

                var name = dict.ContainsKey("name") ? dict["name"].GetString() ?? "" : "";

                // Parse effects if present
                var effects = new List<Dictionary<string, object>>();
                if (dict.ContainsKey("effects"))
                {
                    try
                    {
                        effects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            dict["effects"].GetRawText()
                        ) ?? new List<Dictionary<string, object>>();
                    }
                    catch
                    {
                        // If deserialization fails, leave effects empty
                    }
                }

                return new Equipment
                {
                    Id = name.ToLower().Replace(" ", "-"),
                    Name = name,
                    Type = "item",
                    Effect = dict.ContainsKey("effect") ? dict["effect"].GetString() : null,
                    Cost = dict.ContainsKey("cost") && dict["cost"].ValueKind == JsonValueKind.Number
                        ? dict["cost"].GetInt32() : 0,
                    Slots = dict.ContainsKey("slots") ? dict["slots"].GetInt32() : 0,
                    IconClass = dict.ContainsKey("iconClass") ? dict["iconClass"].GetString() : null,
                    RollRange = dict.ContainsKey("roll") && dict["roll"].ValueKind != JsonValueKind.Null
                        ? dict["roll"].ToString() : null,
                    Effects = effects,
                    IsConsumable = dict.ContainsKey("isConsumable") && dict["isConsumable"].GetBoolean(),
                    ConsumableEffect = dict.ContainsKey("consumableEffect") ? dict["consumableEffect"].GetString() : null
                };
            }));
        }

        // Process ammo
        if (itemData.ContainsKey("ammo"))
        {
            equipment.AddRange(itemData["ammo"].Select(obj =>
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    JsonSerializer.Serialize(obj)
                ) ?? new Dictionary<string, JsonElement>();

                var type = dict.ContainsKey("type") ? dict["type"].GetString() ?? "" : "";
                var shots = dict.ContainsKey("shots") ? dict["shots"].GetInt32() : 0;

                return new Equipment
                {
                    Id = type.ToLower().Replace(" ", "-"),
                    Name = type,
                    Type = "ammo", // Ammo is its own equipment type
                    Effect = dict.ContainsKey("effect") ? dict["effect"].GetString() : null,
                    Cost = dict.ContainsKey("cost") && dict["cost"].ValueKind == JsonValueKind.Number
                        ? dict["cost"].GetInt32() : 0,
                    Slots = dict.ContainsKey("slots") ? dict["slots"].GetInt32() : 0,
                    IconClass = dict.ContainsKey("iconClass") ? dict["iconClass"].GetString() : null,
                    MaxShots = shots,
                    CurrentShots = shots // Ammo starts with full shots when available for purchase
                };
            }));
        }

        return equipment;
    }

    private async Task<List<Equipment>> LoadRelicsAsEquipment(string gameVariant)
    {
        // Relics are already in Equipment format
        var relics = await _resourceService.GetGameResourceAsync<List<Equipment>>(gameVariant, "relics.json");
        return relics ?? new List<Equipment>();
    }
}