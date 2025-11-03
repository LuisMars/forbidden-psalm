using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

public class InjuryOption : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "injury";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-heart-broken";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Injury-specific properties
    public string Effect { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // permanent, temporary, curable, positive, none
    public string? Duration { get; set; } // "next scenario", etc.
    public int? Roll { get; set; } // Roll value (1-20)

    // Effect objects (following armor pattern)
    public List<Dictionary<string, object>> Effects { get; set; } = new();
    public List<Dictionary<string, object>> Penalties { get; set; } = new();

    public string GetDetailedInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"**Effect:** {Effect}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(Type))
        {
            sb.AppendLine($"**Type:** {Type}");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(Duration))
        {
            sb.AppendLine($"**Duration:** {Duration}");
            sb.AppendLine();
        }

        if (Roll.HasValue)
        {
            sb.AppendLine($"**D20 Roll:** {Roll}");
        }

        return sb.ToString().TrimEnd();
    }
}
