namespace ForbiddenPsalmBuilder.Core.Models.GameData;

public class CampaignProgression
{
    public int XpCost { get; set; }
    public List<XpSpendingOption> XpSpending { get; set; } = new();
    public RecruitmentRules? Recruitment { get; set; }
}

public class XpSpendingOption
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int? Amount { get; set; }
    public string? Consequence { get; set; }
}

public class RecruitmentRules
{
    public FreshBloodRule? FreshBlood { get; set; }
}

public class FreshBloodRule
{
    public string Condition { get; set; } = string.Empty;
    public string Cost { get; set; } = string.Empty;
}
