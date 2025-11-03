using System.ComponentModel.DataAnnotations;

namespace ForbiddenPsalmBuilder.Core.DTOs;

public class CreateWarbandDto
{
    [Required(ErrorMessage = "Warband name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Warband name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Game variant is required")]
    public string GameVariant { get; set; } = string.Empty;

    public int StartingGold { get; set; } = 0;
}