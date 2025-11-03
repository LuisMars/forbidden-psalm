using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Legacy item selection model - kept for ISelectableItem compatibility.
/// For new code, use Equipment model with Type = "item" instead.
/// </summary>
public class Item : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "item";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-box";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Item-specific properties
    public string? Effect { get; set; }
    public int Slots { get; set; } = 1;
    public string Type { get; set; } = "item"; // "item" or "ammo"
    public int? Shots { get; set; } // For ammo
    public string? TechLevel { get; set; }

    public string GetDetailedInfo()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(Effect))
        {
            sb.AppendLine($"**Effect:** {Effect}");
            sb.AppendLine();
        }

        if (Type == "ammo" && Shots.HasValue)
        {
            sb.AppendLine($"**Shots:** {Shots}");
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