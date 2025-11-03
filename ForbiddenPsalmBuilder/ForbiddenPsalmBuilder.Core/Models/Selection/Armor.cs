using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Legacy armor selection model - kept for ISelectableItem compatibility.
/// For new code, use Equipment model with Type = "armor" instead.
/// </summary>
public class Armor : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "armor";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "ra ra-shield";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Armor-specific properties
    public int ArmorValue { get; set; }
    public string? ArmorType { get; set; } // body, accessory, pet
    public string? Special { get; set; }
    public int Slots { get; set; } = 1;
    public List<Dictionary<string, object>> Effects { get; set; } = new();
    public List<Dictionary<string, object>> Restrictions { get; set; } = new();

    public string GetDetailedInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"**Armor Value:** {ArmorValue}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(Special))
        {
            sb.AppendLine($"**Special:** {Special}");
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