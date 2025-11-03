using ForbiddenPsalmBuilder.Core.DTOs;

namespace ForbiddenPsalmBuilder.Core.Validation;

public static class WarbandValidator
{
    public static ValidationResult ValidateCreateWarband(CreateWarbandDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Warband name is required");
        }
        else if (dto.Name.Length < 2 || dto.Name.Length > 50)
        {
            errors.Add("Warband name must be between 2 and 50 characters");
        }

        if (string.IsNullOrWhiteSpace(dto.GameVariant))
        {
            errors.Add("Game variant is required");
        }

        if (dto.StartingGold < 0)
        {
            errors.Add("Starting gold cannot be negative");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}