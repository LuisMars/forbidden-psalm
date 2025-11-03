namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Generic interface for selectable items in the InfoSelector component.
/// Implemented by Special Classes, Weapons, Feats, Flaws, Equipment, etc.
/// </summary>
public interface ISelectableItem
{
    /// <summary>
    /// Unique identifier for this item
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Internal name of the item
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Display name shown in UI
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Brief description of the item
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category of the item (e.g., "special-class", "weapon", "feat")
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Cost in currency (null if no cost)
    /// </summary>
    int? Cost { get; }

    /// <summary>
    /// CSS icon class (e.g., "fas fa-sword", "ra ra-crystal-ball")
    /// </summary>
    string IconClass { get; }

    /// <summary>
    /// Additional metadata specific to the item type
    /// </summary>
    Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Render detailed information about this item
    /// </summary>
    string GetDetailedInfo();
}