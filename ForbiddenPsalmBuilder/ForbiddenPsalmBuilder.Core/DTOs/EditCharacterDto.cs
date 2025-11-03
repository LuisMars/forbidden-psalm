using System.ComponentModel.DataAnnotations;
using ForbiddenPsalmBuilder.Core.Models.Character;

namespace ForbiddenPsalmBuilder.Core.DTOs;

public class EditCharacterDto
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Character name is required")]
    [StringLength(50, ErrorMessage = "Character name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(-3, 3, ErrorMessage = "Agility must be between -3 and 3")]
    public int Agility { get; set; }

    [Range(-3, 3, ErrorMessage = "Presence must be between -3 and 3")]
    public int Presence { get; set; }

    [Range(-3, 3, ErrorMessage = "Strength must be between -3 and 3")]
    public int Strength { get; set; }

    [Range(-3, 3, ErrorMessage = "Toughness must be between -3 and 3")]
    public int Toughness { get; set; }

    [StringLength(50, ErrorMessage = "Special class ID cannot exceed 50 characters")]
    public string? SpecialClassId { get; set; }

    // Calculated properties for display
    public int Movement => 5 + Agility;
    public int HP => 8 + Toughness;
    public int EquipmentSlots => 5 + Strength;
}