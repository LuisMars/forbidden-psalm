using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Services;

public class CampaignProgressionService
{
    private readonly IEmbeddedResourceService _resourceService;
    private CampaignProgression? _cachedProgression;

    public CampaignProgressionService(IEmbeddedResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<CampaignProgression> GetCampaignProgressionAsync()
    {
        if (_cachedProgression != null)
        {
            return _cachedProgression;
        }

        var progression = await _resourceService.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json");

        if (progression == null)
        {
            // Return default if file not found
            return new CampaignProgression
            {
                XpCost = 5,
                XpSpending = new List<XpSpendingOption>()
            };
        }

        _cachedProgression = progression;
        return _cachedProgression;
    }

    public async Task<int> GetBaseXpCostAsync()
    {
        var progression = await GetCampaignProgressionAsync();
        return progression.XpCost;
    }

    public async Task<List<XpSpendingOption>> GetXpSpendingOptionsAsync()
    {
        var progression = await GetCampaignProgressionAsync();
        return progression.XpSpending;
    }
}
