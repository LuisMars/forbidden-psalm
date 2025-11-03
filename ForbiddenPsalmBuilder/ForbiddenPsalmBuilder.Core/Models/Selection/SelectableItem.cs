namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Base implementation of ISelectableItem for use in InfoSelector component
/// </summary>
public class SelectableItem : ISelectableItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int? Cost { get; set; }
    public string IconClass { get; set; } = "fas fa-circle";
    public Dictionary<string, object> Metadata { get; set; } = new();

    public virtual string GetDetailedInfo()
    {
        var info = $"{Name}\n{Description}";
        if (Cost.HasValue && Cost.Value > 0)
        {
            info += $"\nCost: {Cost}";
        }
        return info;
    }

    /// <summary>
    /// Factory method to create a "None" item for any category
    /// </summary>
    public static SelectableItem CreateNone(string category)
    {
        return new SelectableItem
        {
            Id = "none",
            Name = "None",
            DisplayName = $"No {category}",
            Description = $"No {category} selected",
            Category = "none",
            Cost = 0,
            IconClass = "fas fa-times-circle"
        };
    }
}