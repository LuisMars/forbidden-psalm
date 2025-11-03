using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Legacy weapon selection model - kept for ISelectableItem compatibility.
/// For new code, use Equipment model with Type = "weapon" instead.
/// </summary>
public class Weapon : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "weapon";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "ra ra-sword";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Weapon-specific properties
    public string Damage { get; set; } = string.Empty;
    public List<string> Properties { get; set; } = new();
    public string Stat { get; set; } = string.Empty;
    public int Slots { get; set; } = 1;
    public string? TechLevel { get; set; }
    public string? AmmoType { get; set; } // Type of ammo this weapon requires (e.g., "Ammo", "Cannonball", "Arrows")

    public bool RequiresAmmo => !string.IsNullOrEmpty(AmmoType);

    public string GetDetailedInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"**Damage:** {Damage}");
        sb.AppendLine();

        sb.AppendLine($"**Stat:** {Stat}");
        sb.AppendLine();

        if (Properties.Any())
        {
            sb.AppendLine("**Properties:**");
            foreach (var property in Properties)
            {
                sb.AppendLine($"- {property}");
            }
            sb.AppendLine();
        }

        if (Cost.HasValue)
        {
            sb.AppendLine($"**Cost:** {Cost}");
            sb.AppendLine();
        }

        sb.AppendLine($"**Slots:** {Slots}");

        return sb.ToString().TrimEnd();
    }
}