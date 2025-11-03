namespace ForbiddenPsalmBuilder.Core.DTOs;

public class WarbandSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GameVariant { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int Gold { get; set; }
    public DateTime LastModified { get; set; }
}