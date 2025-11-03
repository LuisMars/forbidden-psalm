using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Services;

public class EquipmentValidator
{
    /// <summary>
    /// Extract stat requirements from armor restrictions
    /// </summary>
    public (int? strength, int? agility, int? presence, int? toughness) ExtractStatRequirements(Armor armor)
    {
        int? requiredStrength = null;
        int? requiredAgility = null;
        int? requiredPresence = null;
        int? requiredToughness = null;

        foreach (var restriction in armor.Restrictions)
        {
            if (restriction.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString() == "stat_requirement" &&
                restriction.TryGetValue("stat", out var statObj) &&
                restriction.TryGetValue("requiredValue", out var valueObj))
            {
                var stat = statObj?.ToString()?.ToLower();

                // Handle JsonElement from deserialization and string values like "+2"
                int value;
                if (valueObj is JsonElement jsonElement)
                {
                    // Handle both string and number types in JSON
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        var strValue = jsonElement.GetString() ?? "0";
                        // Remove + prefix if present
                        strValue = strValue.TrimStart('+');
                        value = int.Parse(strValue);
                    }
                    else
                    {
                        value = jsonElement.GetInt32();
                    }
                }
                else if (valueObj is string strValue)
                {
                    // Remove + prefix if present
                    strValue = strValue.TrimStart('+');
                    value = int.Parse(strValue);
                }
                else
                {
                    value = Convert.ToInt32(valueObj);
                }

                switch (stat)
                {
                    case "strength":
                        requiredStrength = value;
                        break;
                    case "agility":
                        requiredAgility = value;
                        break;
                    case "presence":
                        requiredPresence = value;
                        break;
                    case "toughness":
                        requiredToughness = value;
                        break;
                }
            }
        }

        return (requiredStrength, requiredAgility, requiredPresence, requiredToughness);
    }

    /// <summary>
    /// Extract stat requirements from equipment restrictions (unified method)
    /// </summary>
    public (int? strength, int? agility, int? presence, int? toughness) ExtractStatRequirements(Equipment equipment)
    {
        int? requiredStrength = null;
        int? requiredAgility = null;
        int? requiredPresence = null;
        int? requiredToughness = null;

        foreach (var restriction in equipment.Restrictions)
        {
            if (restriction.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString() == "stat_requirement" &&
                restriction.TryGetValue("stat", out var statObj) &&
                restriction.TryGetValue("requiredValue", out var valueObj))
            {
                var stat = statObj?.ToString()?.ToLower();

                // Handle JsonElement from deserialization and string values like "+2"
                int value;
                if (valueObj is JsonElement jsonElement)
                {
                    // Handle both string and number types in JSON
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        var strValue = jsonElement.GetString() ?? "0";
                        // Remove + prefix if present
                        strValue = strValue.TrimStart('+');
                        value = int.Parse(strValue);
                    }
                    else
                    {
                        value = jsonElement.GetInt32();
                    }
                }
                else if (valueObj is string strValue)
                {
                    // Remove + prefix if present
                    strValue = strValue.TrimStart('+');
                    value = int.Parse(strValue);
                }
                else
                {
                    value = Convert.ToInt32(valueObj);
                }

                switch (stat)
                {
                    case "strength":
                        requiredStrength = value;
                        break;
                    case "agility":
                        requiredAgility = value;
                        break;
                    case "presence":
                        requiredPresence = value;
                        break;
                    case "toughness":
                        requiredToughness = value;
                        break;
                }
            }
        }

        return (requiredStrength, requiredAgility, requiredPresence, requiredToughness);
    }

    /// <summary>
    /// Check if character can equip armor (only 1 body armor piece allowed, accessories like shields/helmets allowed)
    /// </summary>
    public bool CanEquipArmor(Character character, Equipment armor)
    {
        if (!armor.IsArmor)
            return true;

        // Accessories (shields, helmets, boots) can stack with body armor
        if (armor.ArmorType == "accessory" || armor.ArmorType == "pet")
            return true;

        // Only 1 body armor piece allowed - check only equipped items
        return !character.Equipment.Any(e => e.IsEquipped && e.IsArmor && e.ArmorType == "body");
    }

    /// <summary>
    /// Check if character meets stat requirements for equipment
    /// </summary>
    public bool MeetsStatRequirements(Character character, Equipment equipment,
        int? requiredStrength = null,
        int? requiredAgility = null,
        int? requiredPresence = null,
        int? requiredToughness = null)
    {
        if (requiredStrength.HasValue && character.Stats.Strength < requiredStrength.Value)
            return false;

        if (requiredAgility.HasValue && character.Stats.Agility < requiredAgility.Value)
            return false;

        if (requiredPresence.HasValue && character.Stats.Presence < requiredPresence.Value)
            return false;

        if (requiredToughness.HasValue && character.Stats.Toughness < requiredToughness.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Extract stat penalties from equipment restrictions when requirements are not met
    /// Returns a dictionary mapping stat names to penalty values
    /// </summary>
    public Dictionary<string, int> ExtractStatPenalties(Equipment equipment, Character character)
    {
        var penalties = new Dictionary<string, int>
        {
            { "strength", 0 },
            { "agility", 0 },
            { "presence", 0 },
            { "toughness", 0 }
        };

        foreach (var restriction in equipment.Restrictions)
        {
            if (restriction.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString() == "stat_requirement" &&
                restriction.TryGetValue("stat", out var statObj) &&
                restriction.TryGetValue("requiredValue", out var valueObj))
            {
                var stat = statObj?.ToString()?.ToLower();

                // Handle JsonElement from deserialization and string values like "+2"
                int requiredValue;
                if (valueObj is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        var strValue = jsonElement.GetString() ?? "0";
                        // Remove + prefix if present
                        strValue = strValue.TrimStart('+');
                        requiredValue = int.Parse(strValue);
                    }
                    else
                    {
                        requiredValue = jsonElement.GetInt32();
                    }
                }
                else if (valueObj is string strValue)
                {
                    // Remove + prefix if present
                    strValue = strValue.TrimStart('+');
                    requiredValue = int.Parse(strValue);
                }
                else
                {
                    requiredValue = Convert.ToInt32(valueObj);
                }

                // Check if character meets the requirement
                bool requirementMet = stat switch
                {
                    "strength" => character.Stats.Strength >= requiredValue,
                    "agility" => character.Stats.Agility >= requiredValue,
                    "presence" => character.Stats.Presence >= requiredValue,
                    "toughness" => character.Stats.Toughness >= requiredValue,
                    _ => true
                };

                // If requirement not met and there's an alternative penalty, extract it
                // Support both "penalty" and "alternative" keys for backwards compatibility
                object? penaltyObj = null;
                if (!requirementMet)
                {
                    if (!restriction.TryGetValue("alternative", out penaltyObj))
                    {
                        restriction.TryGetValue("penalty", out penaltyObj);
                    }
                }

                if (penaltyObj != null)
                {
                    Dictionary<string, object>? penaltyDict = null;

                    // Handle JsonElement from deserialization
                    if (penaltyObj is JsonElement penaltyElement && penaltyElement.ValueKind == JsonValueKind.Object)
                    {
                        penaltyDict = new Dictionary<string, object>();
                        foreach (var property in penaltyElement.EnumerateObject())
                        {
                            penaltyDict[property.Name] = property.Value;
                        }
                    }
                    else if (penaltyObj is Dictionary<string, object> dict)
                    {
                        penaltyDict = dict;
                    }

                    if (penaltyDict != null &&
                        penaltyDict.TryGetValue("stat", out var penaltyStatObj))
                    {
                        var penaltyStat = penaltyStatObj?.ToString()?.ToLower();

                        // Try "modifier" first (JSON format), then "value" (test format)
                        object? penaltyValueObj = null;
                        if (!penaltyDict.TryGetValue("modifier", out penaltyValueObj))
                        {
                            penaltyDict.TryGetValue("value", out penaltyValueObj);
                        }

                        if (penaltyValueObj != null)
                        {
                            // Handle JsonElement for penalty value
                            int penaltyValue;
                            if (penaltyValueObj is JsonElement penaltyValueElement)
                            {
                                if (penaltyValueElement.ValueKind == JsonValueKind.String)
                                {
                                    penaltyValue = int.Parse(penaltyValueElement.GetString() ?? "0");
                                }
                                else
                                {
                                    penaltyValue = penaltyValueElement.GetInt32();
                                }
                            }
                            else
                            {
                                penaltyValue = Convert.ToInt32(penaltyValueObj);
                            }

                            if (penaltyStat != null && penalties.ContainsKey(penaltyStat))
                            {
                                penalties[penaltyStat] += penaltyValue;
                            }
                        }
                    }
                }
            }
        }

        return penalties;
    }

    /// <summary>
    /// Validate equipment and return error message if invalid, null if valid
    /// </summary>
    public string? ValidateEquipment(Character character, Equipment equipment,
        int? requiredStrength = null,
        int? requiredAgility = null,
        int? requiredPresence = null,
        int? requiredToughness = null)
    {
        // Check slot availability
        if (!character.CanEquip(equipment))
        {
            var usedSlots = character.Equipment.Sum(e => e.CalculateRequiredSlots());
            var totalSlots = character.GetEffectiveEquipmentSlots();
            var itemAlreadyInList = character.Equipment.Any(e => e.Id == equipment.Id);
            var additionalSlotsNeeded = itemAlreadyInList ? 0 : equipment.CalculateRequiredSlots();
            return $"Not enough equipment slots. Character has {totalSlots} slots, {usedSlots} used, needs {additionalSlotsNeeded} more.";
        }

        // Check armor limit
        if (equipment.IsArmor && !CanEquipArmor(character, equipment))
        {
            return "Character can only wear one body armor piece at a time. Accessories like shields and helmets can be worn with body armor.";
        }

        // Check stat requirements
        if (!MeetsStatRequirements(character, equipment, requiredStrength, requiredAgility, requiredPresence, requiredToughness))
        {
            var requirements = new List<string>();

            if (requiredStrength.HasValue && character.Stats.Strength < requiredStrength.Value)
                requirements.Add($"Strength {requiredStrength.Value}+");

            if (requiredAgility.HasValue && character.Stats.Agility < requiredAgility.Value)
                requirements.Add($"Agility {requiredAgility.Value}+");

            if (requiredPresence.HasValue && character.Stats.Presence < requiredPresence.Value)
                requirements.Add($"Presence {requiredPresence.Value}+");

            if (requiredToughness.HasValue && character.Stats.Toughness < requiredToughness.Value)
                requirements.Add($"Toughness {requiredToughness.Value}+");

            return $"Requires {string.Join(", ", requirements)}";
        }

        return null;
    }
}
