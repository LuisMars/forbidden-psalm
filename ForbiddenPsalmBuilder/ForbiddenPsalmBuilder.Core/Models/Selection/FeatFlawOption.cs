using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

public class FeatFlawOption : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "feat"; // "feat" or "flaw"
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-star";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Feat/Flaw-specific properties
    public string Effect { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // permanent, temporary, etc.
    public int? Roll { get; set; } // Single roll value (e.g., 1)
    public string? RollRange { get; set; } // Range (e.g., "1-4", "97-100")
    public int? XpModifier { get; set; } // For "Slow Learner" etc. - adds to XP cost for level-up

    // Effect objects (following armor/injury pattern)
    public List<Dictionary<string, object>> Effects { get; set; } = new();

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

        if (Roll.HasValue)
        {
            sb.AppendLine($"**Roll:** {Roll}");
        }
        else if (!string.IsNullOrEmpty(RollRange))
        {
            sb.AppendLine($"**Roll Range:** {RollRange}");
        }

        return sb.ToString().TrimEnd();
    }
}
