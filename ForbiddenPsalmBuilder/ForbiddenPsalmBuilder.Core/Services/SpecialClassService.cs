using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Services;

public class SpecialClassService
{
    private readonly IEmbeddedResourceService _resourceService;
    private Dictionary<string, List<SpecialClass>> _cache = new();

    public SpecialClassService()
    {
        _resourceService = new EmbeddedResourceService();
    }

    public SpecialClassService(IEmbeddedResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<List<SpecialClass>> GetSpecialClassesAsync(string gameVariant)
    {
        if (string.IsNullOrEmpty(gameVariant))
            return new List<SpecialClass>();

        // Check cache first
        if (_cache.ContainsKey(gameVariant))
            return _cache[gameVariant];

        try
        {
            var specialClasses = await _resourceService.GetGameResourceAsync<List<SpecialClass>>(
                gameVariant,
                "special-classes.json"
            );

            if (specialClasses != null)
            {
                _cache[gameVariant] = specialClasses;
                return specialClasses;
            }

            return new List<SpecialClass>();
        }
        catch
        {
            return new List<SpecialClass>();
        }
    }

    public async Task<SpecialClass?> GetSpecialClassByIdAsync(string id, string gameVariant)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(gameVariant))
            return null;

        var specialClasses = await GetSpecialClassesAsync(gameVariant);
        return specialClasses.FirstOrDefault(sc => sc.Id == id);
    }
}