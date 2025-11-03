using ForbiddenPsalmBuilder.Core.Models.Selection;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Models.Character;

public class Character
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Stats Stats { get; set; } = new();
    public List<string> Feats { get; set; } = new();
    public List<string> Flaws { get; set; } = new();
    public List<Equipment> Equipment { get; set; } = new();
    public int CurrentGold { get; set; }
    public int CurrentHP { get; set; }

    public string? SpecialClassId { get; set; }
    public List<string> Injuries { get; set; } = new();
    public List<string> Spells { get; set; } = new();
    public int Tragedies { get; set; }

    public Character()
    {
        CurrentHP = Stats.HP;
    }

    public Character(string name) : this()
    {
        Name = name;
    }

    // Calculate effective stats (currently same as base stats - equipment modifiers not in game rules)
    public Stats EffectiveStats => Stats;

    /// <summary>
    /// Calculate effective stats with feat/flaw/injury modifiers applied
    /// </summary>
    public Stats GetEffectiveStats(List<FeatFlawOption> feats, List<FeatFlawOption> flaws, List<InjuryOption> injuries)
    {
        // Start with base stats
        int agility = Stats.Agility;
        int presence = Stats.Presence;
        int strength = Stats.Strength;
        int toughness = Stats.Toughness;

        // Apply feat modifiers
        foreach (var featName in Feats)
        {
            var feat = feats.FirstOrDefault(f => f.Name == featName);
            if (feat?.Effects != null)
            {
                ApplyStatModifiers(feat.Effects, ref agility, ref presence, ref strength, ref toughness);
            }
        }

        // Apply flaw modifiers
        foreach (var flawName in Flaws)
        {
            var flaw = flaws.FirstOrDefault(f => f.Name == flawName);
            if (flaw?.Effects != null)
            {
                ApplyStatModifiers(flaw.Effects, ref agility, ref presence, ref strength, ref toughness);
            }
        }

        // Apply injury modifiers
        foreach (var injuryName in Injuries)
        {
            var injury = injuries.FirstOrDefault(i => i.Name == injuryName);
            if (injury?.Effects != null)
            {
                ApplyStatModifiers(injury.Effects, ref agility, ref presence, ref strength, ref toughness);
            }
        }

        // Apply equipment modifiers (relics, armor, etc.) - only from equipped items
        foreach (var equippedItem in Equipment.Where(e => e.IsEquipped))
        {
            if (equippedItem.Effects != null && equippedItem.Effects.Count > 0)
            {
                ApplyStatModifiers(equippedItem.Effects, ref agility, ref presence, ref strength, ref toughness);
            }
        }

        // Apply penalties from unmet equipment requirements - only from equipped items
        var validator = new Services.EquipmentValidator();
        foreach (var equippedItem in Equipment.Where(e => e.IsEquipped))
        {
            if (equippedItem.Restrictions != null && equippedItem.Restrictions.Count > 0)
            {
                var penalties = validator.ExtractStatPenalties(equippedItem, this);
                agility += penalties["agility"];
                presence += penalties["presence"];
                strength += penalties["strength"];
                toughness += penalties["toughness"];
            }
        }

        return new Stats(agility, presence, strength, toughness);
    }

    private void ApplyStatModifiers(List<Dictionary<string, object>> effects,
        ref int agility, ref int presence, ref int strength, ref int toughness)
    {
        foreach (var effect in effects)
        {
            // Check if this is a stat_modifier effect
            if (effect.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString() == "stat_modifier" &&
                effect.TryGetValue("stat", out var statObj) &&
                effect.TryGetValue("modifier", out var modifierObj))
            {
                var stat = statObj?.ToString()?.ToLower();

                // Handle JsonElement from deserialization
                int modifier;
                if (modifierObj is JsonElement jsonElement)
                {
                    modifier = jsonElement.GetInt32();
                }
                else
                {
                    modifier = Convert.ToInt32(modifierObj);
                }

                switch (stat)
                {
                    case "agility":
                        agility += modifier;
                        break;
                    case "presence":
                        presence += modifier;
                        break;
                    case "strength":
                        strength += modifier;
                        break;
                    case "toughness":
                        toughness += modifier;
                        break;
                }
            }
        }
    }

    // Get effective equipment slots (base + bonuses from equipped items like backpack)
    public int GetEffectiveEquipmentSlots()
    {
        var baseSlots = Stats.EquipmentSlots;
        var bonusSlots = 0;

        // Only count bonuses from equipped items
        foreach (var equippedItem in Equipment.Where(e => e.IsEquipped))
        {
            foreach (var effect in equippedItem.Effects)
            {
                if (effect.TryGetValue("type", out var typeObj) &&
                    typeObj?.ToString() == "slot_modifier" &&
                    effect.TryGetValue("modifier", out var modifierObj))
                {
                    // Handle JsonElement from deserialization
                    if (modifierObj is JsonElement jsonElement)
                    {
                        bonusSlots += jsonElement.GetInt32();
                    }
                    else
                    {
                        bonusSlots += Convert.ToInt32(modifierObj);
                    }
                }
            }
        }

        return baseSlots + bonusSlots;
    }

    // Check if character can equip more items
    public bool CanEquip(Equipment item)
    {
        // Count ALL items' slots (both equipped and inventory) - use dynamic calculation for ammo
        var usedSlots = Equipment.Sum(e => e.CalculateRequiredSlots());
        var totalAvailableSlots = GetEffectiveEquipmentSlots();

        // If the item is already in the equipment list, don't add its slots again
        var itemAlreadyInList = Equipment.Any(e => e.Id == item.Id);
        var additionalSlotsNeeded = itemAlreadyInList ? 0 : item.CalculateRequiredSlots();

        return usedSlots + additionalSlotsNeeded <= totalAvailableSlots;
    }

    // Check if character has equipment restrictions that prevent equipping an item
    public bool HasEquipmentRestriction(Equipment item)
    {
        // Check only equipped items for restrictions (inventory items don't apply restrictions)
        foreach (var equippedItem in Equipment.Where(e => e.IsEquipped))
        {
            if (equippedItem.Restrictions != null)
            {
                foreach (var restriction in equippedItem.Restrictions)
                {
                    if (restriction.TryGetValue("type", out var typeObj) &&
                        typeObj?.ToString() == "equipment_restriction" &&
                        restriction.TryGetValue("excludedTypes", out var excludedTypesObj))
                    {
                        // Handle both JsonElement arrays and List<object>
                        List<string> excludedTypes = new List<string>();

                        if (excludedTypesObj is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in jsonArray.EnumerateArray())
                            {
                                excludedTypes.Add(element.GetString() ?? "");
                            }
                        }
                        else if (excludedTypesObj is List<object> list)
                        {
                            excludedTypes = list.Select(x => x?.ToString() ?? "").ToList();
                        }

                        // Check if the item type is in the excluded list
                        if (excludedTypes.Any(excludedType =>
                            string.Equals(excludedType, item.Type, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true; // Restriction applies
                        }
                    }
                }
            }
        }

        return false; // No restrictions
    }

    // Check if character has flaw/feat restrictions that prevent equipping an item
    public bool HasFlawRestriction(Equipment item, List<FeatFlawOption> flaws)
    {
        // Check all character flaws for restrictions
        foreach (var flawName in Flaws)
        {
            var flaw = flaws.FirstOrDefault(f => f.Name == flawName);
            if (flaw?.Effects != null)
            {
                foreach (var effect in flaw.Effects)
                {
                    if (effect.TryGetValue("type", out var typeObj) &&
                        typeObj?.ToString() == "equipment_restriction" &&
                        effect.TryGetValue("excludedEquipment", out var excludedEquipmentObj))
                    {
                        // Handle both JsonElement arrays and List<object>
                        List<string> excludedEquipment = new List<string>();

                        if (excludedEquipmentObj is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in jsonArray.EnumerateArray())
                            {
                                excludedEquipment.Add(element.GetString() ?? "");
                            }
                        }
                        else if (excludedEquipmentObj is List<object> list)
                        {
                            excludedEquipment = list.Select(x => x?.ToString() ?? "").ToList();
                        }

                        // Check if the item name is in the excluded list
                        if (excludedEquipment.Any(excludedName =>
                            string.Equals(excludedName, item.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true; // Restriction applies
                        }
                    }
                }
            }
        }

        return false; // No restrictions
    }

    // Get total equipment value
    public int TotalEquipmentValue => Equipment.Sum(e => e.Cost);

    // Get total armor value from equipped items only
    public int GetTotalArmorValue()
    {
        return Equipment.Where(e => e.IsEquipped).Sum(e => e.ArmorValue);
    }

    // Equip an item from inventory
    public void EquipItem(string equipmentId)
    {
        var item = Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (item != null)
        {
            item.IsEquipped = true;
        }
    }

    // Unequip an item to inventory
    public void UnequipItem(string equipmentId)
    {
        var item = Equipment.FirstOrDefault(e => e.Id == equipmentId);
        if (item != null)
        {
            item.IsEquipped = false;
        }
    }
}