using System.Text.Json.Serialization;

namespace ForbiddenPsalmBuilder.Core.Models.GameData;

public class WeaponsData
{
    [JsonPropertyName("oneHandedMelee")]
    public List<WeaponDto> OneHandedMelee { get; set; } = new();

    [JsonPropertyName("twoHandedMelee")]
    public List<WeaponDto> TwoHandedMelee { get; set; } = new();

    [JsonPropertyName("oneHandedRanged")]
    public List<WeaponDto> OneHandedRanged { get; set; } = new();

    [JsonPropertyName("twoHandedRanged")]
    public List<WeaponDto> TwoHandedRanged { get; set; } = new();

    [JsonPropertyName("throwables")]
    public List<WeaponDto> Throwables { get; set; } = new();

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class WeaponDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("damage")]
    public string Damage { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<string> Properties { get; set; } = new();

    [JsonPropertyName("stat")]
    public string Stat { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public int? Cost { get; set; }

    [JsonPropertyName("slots")]
    public int Slots { get; set; } = 1;

    [JsonPropertyName("techLevel")]
    public string? TechLevel { get; set; }

    [JsonPropertyName("iconClass")]
    public string? IconClass { get; set; }

    [JsonPropertyName("rollRange")]
    public string? RollRange { get; set; }

    [JsonPropertyName("special")]
    public string? Special { get; set; }

    [JsonPropertyName("effects")]
    public List<string>? Effects { get; set; }

    [JsonPropertyName("ammoType")]
    public string? AmmoType { get; set; }
}
