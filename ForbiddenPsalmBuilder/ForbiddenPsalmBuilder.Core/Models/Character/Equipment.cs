namespace ForbiddenPsalmBuilder.Core.Models.Character;

public class Equipment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // weapon, armor, item
    public string? Category { get; set; } // oneHandedMelee, twoHandedMelee, oneHandedRanged, twoHandedRanged, throwables, etc.
    public string? Damage { get; set; }
    public string? Range { get; set; } // Ranged distance for weapons
    public List<string> Properties { get; set; } = new();
    public string? Stat { get; set; } // What stat this uses
    public int Cost { get; set; }
    public int Slots { get; set; } = 1;
    public int ArmorValue { get; set; }
    public string? ArmorType { get; set; } // body, accessory, pet (for armor only)
    public string? Special { get; set; }
    public string? Effect { get; set; }
    public string? RollRange { get; set; }
    public string? IconClass { get; set; } // Icon class for displaying in UI
    public string? AmmoType { get; set; } // Type of ammo this weapon requires (e.g., "Ammo", "Cannonball", "Arrows")
    public bool Unique { get; set; } // For relics/artefacts - only one of each can exist in a campaign
    public List<Dictionary<string, object>> Restrictions { get; set; } = new(); // Stat requirements and other restrictions
    public List<Dictionary<string, object>> Effects { get; set; } = new(); // Equipment effects (slot modifiers, etc.)
    public bool IsEquipped { get; set; } = false; // Whether this item is equipped (provides benefits) or just in inventory

    // Ammo tracking
    public int CurrentShots { get; set; } = 0; // Remaining ammo in weapon OR shots in ammo item
    public int MaxShots { get; set; } = 0; // Maximum shots for reference

    // Consumable tracking
    public bool IsConsumable { get; set; } = false; // Flag for consumable items
    public string? ConsumableEffect { get; set; } // Effect type: "heal_hp", "cure_bleeding", "cure_poisoned", "cure_disease", "prevent_gas"

    // Custom item tracking
    public bool IsCustom { get; set; } = false; // Flag for user-created custom items

    // Computed properties
    public bool IsWeapon => Type.Equals("weapon", StringComparison.OrdinalIgnoreCase);
    public bool IsArmor => Type.Equals("armor", StringComparison.OrdinalIgnoreCase);
    public bool IsItem => Type.Equals("item", StringComparison.OrdinalIgnoreCase);
    public bool IsRelic => Type.Equals("relic", StringComparison.OrdinalIgnoreCase);
    public bool IsAmmo => Type.Equals("ammo", StringComparison.OrdinalIgnoreCase);
    public bool RequiresAmmo => !string.IsNullOrEmpty(AmmoType);

    // Calculate slots required dynamically
    // - For ammo: 1 slot per 5 shots (rounded up)
    // - For weapons with ammo: base slots + 1 slot per magazine loaded
    //   Example: Bow (2 slots, MaxShots=5) with 10 shots = 2 + 2 = 4 slots
    public int CalculateRequiredSlots()
    {
        if (IsAmmo && CurrentShots > 0)
        {
            // Ammo: 1 slot per 5 shots
            return (int)Math.Ceiling(CurrentShots / 5.0);
        }
        else if (IsWeapon && RequiresAmmo && MaxShots > 0 && CurrentShots > 0)
        {
            // Weapon with loaded ammunition
            // Each magazine takes 1 slot, and we count ALL magazines loaded
            int totalMagazines = (int)Math.Ceiling((double)CurrentShots / MaxShots);
            return Slots + totalMagazines;
        }

        return Slots;
    }
}