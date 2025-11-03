namespace ForbiddenPsalmBuilder.Core.Models.GameData;

public class GameConfig
{
    public string GameName { get; set; } = string.Empty;
    public Currency Currency { get; set; } = new();
    public List<string> TechLevels { get; set; } = new();
    public WarbandSize WarbandSize { get; set; } = new();
    public CharacterCreationRules CharacterCreation { get; set; } = new();
    public WeaponRules? WeaponRules { get; set; }
    public ArmorRules? ArmorRules { get; set; }
    public Dictionary<string, Service> Services { get; set; } = new();
}

public class Currency
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int StartingBudget { get; set; }
}

public class WarbandSize
{
    public int Max { get; set; } = 5;
    public int? SpellcasterLimit { get; set; }
    public int? SpecialTrooperLimit { get; set; }
    public int? PetLimit { get; set; }
}

public class CharacterCreationRules
{
    public Dictionary<string, object> VariantSpecific { get; set; } = new();
    public OptionalHires? OptionalHires { get; set; }
    public SpecialTroopers? SpecialTroopers { get; set; }
}

public class OptionalHires
{
    public SpellcasterHire? Spellcaster { get; set; }
    public PetHire? Pets { get; set; }
}

public class SpellcasterHire
{
    public int Cost { get; set; }
    public List<string> Abilities { get; set; } = new();
    public StartingMagicItems? StartingMagicItems { get; set; }
}

public class PetHire
{
    public bool AvailableFromStart { get; set; }
    public string? Reference { get; set; }
}

public class StartingMagicItems
{
    public int Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public string? Requirement { get; set; }
}

public class SpecialTroopers
{
    public int Cost { get; set; }
    public int Limit { get; set; }
    public bool FreeStartingEquipment { get; set; }
    public int ReplacementCost { get; set; }
    public List<SpecialTrooperType> Types { get; set; } = new();
}

public class SpecialTrooperType
{
    public string Name { get; set; } = string.Empty;
    public StartingMagicItems? StartingMagicItems { get; set; }
    public List<string> Abilities { get; set; } = new();
    public List<StartingEquipment> StartingEquipment { get; set; } = new();
    public StatBonus? StatBonus { get; set; }
}

public class StartingEquipment
{
    public string Item { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
    public string Source { get; set; } = string.Empty;
}

public class StatBonus
{
    public string Stat { get; set; } = string.Empty;
    public int Modifier { get; set; }
}

public class WeaponRules
{
    public string StartingCondition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ArmorRules
{
    public EquipmentRestrictions? EquipmentRestrictions { get; set; }
}

public class EquipmentRestrictions
{
    public List<string> OnlyOne { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public class Service
{
    public ServiceOption? DeathSaveBypass { get; set; }
    public ServiceOption? InjuryRemoval { get; set; }
}

public class ServiceOption
{
    public int Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool? Repeatable { get; set; }
}