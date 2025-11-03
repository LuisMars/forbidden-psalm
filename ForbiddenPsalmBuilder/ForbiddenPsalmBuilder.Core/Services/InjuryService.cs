using System.Text.Json;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Services;

public class InjuryService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly Dictionary<string, List<InjuryOption>> _injuriesCache = new();
    private readonly Random _random = new();

    public InjuryService(IEmbeddedResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<List<InjuryOption>> GetInjuriesAsync(string gameVariant)
    {
        if (_injuriesCache.ContainsKey(gameVariant))
        {
            return _injuriesCache[gameVariant];
        }

        var data = await _resourceService.GetGameResourceAsync<JsonDocument>(gameVariant, "injuries.json");
        if (data == null)
            return new List<InjuryOption>();

        var injuries = new List<InjuryOption>();

        foreach (var injuryElement in data.RootElement.EnumerateArray())
        {
            var injury = ParseInjury(injuryElement);
            if (injury != null)
            {
                injuries.Add(injury);
            }
        }

        _injuriesCache[gameVariant] = injuries;
        return injuries;
    }

    public async Task<InjuryOption> RollInjuryAsync(string gameVariant)
    {
        var injuries = await GetInjuriesAsync(gameVariant);

        if (!injuries.Any())
        {
            throw new InvalidOperationException($"No injuries available for game variant '{gameVariant}'");
        }

        return RollFromList(injuries);
    }

    private InjuryOption RollFromList(List<InjuryOption> options)
    {
        // Roll D20 (1-20)
        var roll = _random.Next(1, 21);

        // Find matching option based on roll
        var match = options.FirstOrDefault(o => o.Roll.HasValue && o.Roll.Value == roll);

        // If no exact match, pick a random one
        return match ?? options[_random.Next(options.Count)];
    }

    private InjuryOption? ParseInjury(JsonElement element)
    {
        try
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var effect = element.GetProperty("effect").GetString() ?? string.Empty;
            var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var category = element.TryGetProperty("category", out var categoryElement) ? categoryElement.GetString() : null;

            var option = new InjuryOption
            {
                Name = name,
                DisplayName = name,
                Description = effect,
                Effect = effect,
                Type = type ?? "permanent",
                Category = category ?? "injury",
                IconClass = "fas fa-heart-broken"
            };

            if (element.TryGetProperty("roll", out var rollElement) && rollElement.ValueKind != JsonValueKind.Null)
            {
                option.Roll = rollElement.GetInt32();
            }

            if (element.TryGetProperty("duration", out var durationElement) && durationElement.ValueKind != JsonValueKind.Null)
            {
                option.Duration = durationElement.GetString();
            }

            // Parse effects array
            if (element.TryGetProperty("effects", out var effectsElement) && effectsElement.ValueKind == JsonValueKind.Array)
            {
                option.Effects = ParseEffectsArray(effectsElement);
            }

            // Parse penalties array (for backward compatibility or additional info)
            if (element.TryGetProperty("penalties", out var penaltiesElement) && penaltiesElement.ValueKind == JsonValueKind.Array)
            {
                option.Penalties = ParseEffectsArray(penaltiesElement);
            }

            return option;
        }
        catch
        {
            return null;
        }
    }

    private List<Dictionary<string, object>> ParseEffectsArray(JsonElement arrayElement)
    {
        var effects = new List<Dictionary<string, object>>();

        foreach (var effectElement in arrayElement.EnumerateArray())
        {
            var effectDict = new Dictionary<string, object>();

            foreach (var property in effectElement.EnumerateObject())
            {
                effectDict[property.Name] = ParseJsonValue(property.Value);
            }

            effects.Add(effectDict);
        }

        return effects;
    }

    private object ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => ParseJsonArray(element),
            JsonValueKind.Object => ParseJsonObject(element),
            _ => string.Empty
        };
    }

    private List<object> ParseJsonArray(JsonElement arrayElement)
    {
        var list = new List<object>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            list.Add(ParseJsonValue(item));
        }
        return list;
    }

    private Dictionary<string, object> ParseJsonObject(JsonElement objectElement)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in objectElement.EnumerateObject())
        {
            dict[property.Name] = ParseJsonValue(property.Value);
        }
        return dict;
    }
}
