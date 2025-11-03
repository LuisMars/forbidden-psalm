namespace ForbiddenPsalmBuilder.Core.Models.Character;

public class Stats
{
    public int Agility { get; set; }
    public int Presence { get; set; }
    public int Strength { get; set; }
    public int Toughness { get; set; }

    public Stats() { }

    public Stats(int agility, int presence, int strength, int toughness)
    {
        Agility = agility;
        Presence = presence;
        Strength = strength;
        Toughness = toughness;
    }

    // Derived stats based on character creation rules
    public int Movement => 5 + Agility;
    public int HP => 8 + Toughness;
    public int EquipmentSlots => 5 + Strength;
}

public class StatArray
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int[] Values { get; set; } = Array.Empty<int>();
    public string Description { get; set; } = string.Empty;
}