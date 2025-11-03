using System.Text;

namespace ForbiddenPsalmBuilder.Core.Models.Selection;

public class SpellOption : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "spell";
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-magic";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Spell-specific properties
    public string Effect { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // offensive, defensive, support, utility, etc.
    public string SpellCategory { get; set; } = string.Empty; // clean, unclean, sanctified, heathen, etc.

    // Effect objects (similar to feats/flaws/armor pattern)
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

        return sb.ToString().TrimEnd();
    }
}
