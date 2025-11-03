using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Data.Services;
using Microsoft.Extensions.Logging;

namespace ForbiddenPsalmBuilder.Core.Services;

/// <summary>
/// Service for loading and managing traders
/// </summary>
public class TraderService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly ILogger<TraderService>? _logger;
    private readonly Dictionary<string, List<Trader>> _cache = new();

    public TraderService(IEmbeddedResourceService resourceService, ILogger<TraderService>? logger = null)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all traders for a game variant
    /// </summary>
    public async Task<List<Trader>> GetTradersAsync(string gameVariant)
    {
        if (_cache.TryGetValue(gameVariant, out var cached))
        {
            return cached;
        }

        var traders = await _resourceService.GetGameResourceAsync<List<Trader>>(gameVariant, "traders.json");

        if (traders == null)
        {
            return new List<Trader>();
        }

        _cache[gameVariant] = traders;
        return traders;
    }

    /// <summary>
    /// Get a specific trader by ID
    /// </summary>
    public async Task<Trader?> GetTraderByIdAsync(string traderId, string gameVariant)
    {
        var traders = await GetTradersAsync(gameVariant);
        return traders.FirstOrDefault(t => t.Id == traderId);
    }

    /// <summary>
    /// Get traders available at a specific campaign chapter
    /// </summary>
    public async Task<List<Trader>> GetTradersByChapterAsync(int chapter, string gameVariant)
    {
        var traders = await GetTradersAsync(gameVariant);
        return traders
            .Where(t => t.MinimumChapter == null || t.MinimumChapter <= chapter)
            .ToList();
    }

    /// <summary>
    /// Calculate the price a trader will pay when buying an item from a player
    /// </summary>
    public int CalculateBuyPrice(Trader trader, int baseValue)
    {
        return trader.CalculateBuyPrice(baseValue);
    }

    /// <summary>
    /// Calculate the price a trader charges when selling an item to a player
    /// </summary>
    public int CalculateSellPrice(Trader trader, int baseValue)
    {
        return trader.CalculateSellPrice(baseValue);
    }
}
