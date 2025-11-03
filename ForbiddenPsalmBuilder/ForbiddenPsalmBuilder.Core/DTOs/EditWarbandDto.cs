using System.ComponentModel.DataAnnotations;

namespace ForbiddenPsalmBuilder.Core.DTOs;

public class EditWarbandDto
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warband name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Warband name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 9999, ErrorMessage = "Gold must be between 0 and 9999")]
    public int Gold { get; set; }

    [Range(0, 999, ErrorMessage = "Experience must be between 0 and 999")]
    public int Experience { get; set; }

    public string GameVariant { get; set; } = string.Empty;
}