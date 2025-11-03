namespace ForbiddenPsalmBuilder.Core.Models.Warband;

public class ExportOptions
{
    /// <summary>
    /// Include active members in the export
    /// </summary>
    public bool IncludeMembers { get; set; } = true;

    /// <summary>
    /// Include dead members in the export
    /// </summary>
    public bool IncludeDeadMembers { get; set; } = false;

    /// <summary>
    /// Include fired members in the export
    /// </summary>
    public bool IncludeFiredMembers { get; set; } = false;

    /// <summary>
    /// Include warband stash in the export
    /// </summary>
    public bool IncludeStash { get; set; } = false;

    /// <summary>
    /// Include custom items in the export
    /// </summary>
    public bool IncludeCustomItems { get; set; } = false;

    /// <summary>
    /// Include detailed character stats (HP, gold, injuries, etc.)
    /// </summary>
    public bool IncludeDetailedStats { get; set; } = true;

    /// <summary>
    /// Include character equipment in the export
    /// </summary>
    public bool IncludeEquipment { get; set; } = true;

    /// <summary>
    /// Create a default set of export options for print-friendly summary
    /// </summary>
    public static ExportOptions PrintFriendly()
    {
        return new ExportOptions
        {
            IncludeMembers = true,
            IncludeDeadMembers = false,
            IncludeFiredMembers = false,
            IncludeStash = false,
            IncludeCustomItems = false,
            IncludeDetailedStats = true,
            IncludeEquipment = true
        };
    }

    /// <summary>
    /// Create a default set of export options for full campaign data
    /// </summary>
    public static ExportOptions FullCampaign()
    {
        return new ExportOptions
        {
            IncludeMembers = true,
            IncludeDeadMembers = true,
            IncludeFiredMembers = true,
            IncludeStash = true,
            IncludeCustomItems = true,
            IncludeDetailedStats = true,
            IncludeEquipment = true
        };
    }
}
