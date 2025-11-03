using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

public class SpecialClass : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "special-class";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-user";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // SpecialClass specific properties
    public string GameVariant { get; set; } = string.Empty;
    public bool IsSpellcaster { get; set; } = false;
    public List<string> StartingEquipment { get; set; } = new();
    public Dictionary<string, int> StatBonuses { get; set; } = new();
    public List<string> Abilities { get; set; } = new();

    public string GetDetailedInfo()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(Description))
        {
            sb.AppendLine(Description);
            sb.AppendLine();
        }

        if (Cost.HasValue)
        {
            sb.AppendLine($"**Cost:** {Cost}");
            sb.AppendLine();
        }

        if (StartingEquipment.Any())
        {
            sb.AppendLine("**Starting Equipment:**");
            foreach (var item in StartingEquipment)
            {
                sb.AppendLine($"- {item}");
            }
            sb.AppendLine();
        }

        if (Abilities.Any())
        {
            sb.AppendLine("**Abilities:**");
            foreach (var ability in Abilities)
            {
                sb.AppendLine($"- {ability}");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}