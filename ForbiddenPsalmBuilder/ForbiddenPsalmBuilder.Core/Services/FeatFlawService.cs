using System.Text.Json;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Services;

public class FeatFlawService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly Dictionary<string, List<FeatFlawOption>> _featsCache = new();
    private readonly Dictionary<string, List<FeatFlawOption>> _flawsCache = new();
    private readonly Random _random = new();

    public FeatFlawService(IEmbeddedResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<List<FeatFlawOption>> GetFeatsAsync(string gameVariant)
    {
        if (_featsCache.ContainsKey(gameVariant))
        {
            return _featsCache[gameVariant];
        }

        var data = await _resourceService.GetGameResourceAsync<JsonDocument>(gameVariant, "feats-flaws.json");
        if (data == null)
            return new List<FeatFlawOption>();

        var featsArray = data.RootElement.GetProperty("feats");

        var feats = new List<FeatFlawOption>();

        foreach (var featElement in featsArray.EnumerateArray())
        {
            var feat = ParseFeatFlaw(featElement, "feat");
            if (feat != null)
            {
                feats.Add(feat);
            }
        }

        _featsCache[gameVariant] = feats;
        return feats;
    }

    public async Task<List<FeatFlawOption>> GetFlawsAsync(string gameVariant)
    {
        if (_flawsCache.ContainsKey(gameVariant))
        {
            return _flawsCache[gameVariant];
        }

        var data = await _resourceService.GetGameResourceAsync<JsonDocument>(gameVariant, "feats-flaws.json");
        if (data == null)
            return new List<FeatFlawOption>();

        var flawsArray = data.RootElement.GetProperty("flaws");

        var flaws = new List<FeatFlawOption>();

        foreach (var flawElement in flawsArray.EnumerateArray())
        {
            var flaw = ParseFeatFlaw(flawElement, "flaw");
            if (flaw != null)
            {
                flaws.Add(flaw);
            }
        }

        _flawsCache[gameVariant] = flaws;
        return flaws;
    }

    public async Task<FeatFlawOption> RollFeatAsync(string gameVariant)
    {
        var feats = await GetFeatsAsync(gameVariant);

        if (!feats.Any())
        {
            throw new InvalidOperationException($"No feats available for game variant '{gameVariant}'");
        }

        return RollFromList(feats);
    }

    public async Task<FeatFlawOption> RollFlawAsync(string gameVariant)
    {
        var flaws = await GetFlawsAsync(gameVariant);

        if (!flaws.Any())
        {
            throw new InvalidOperationException($"No flaws available for game variant '{gameVariant}'");
        }

        return RollFromList(flaws);
    }

    private FeatFlawOption RollFromList(List<FeatFlawOption> options)
    {
        // Roll D100 (1-100)
        var roll = _random.Next(1, 101);

        // Find matching option based on roll or rollRange
        foreach (var option in options)
        {
            if (option.Roll.HasValue && option.Roll.Value == roll)
            {
                return option;
            }

            if (!string.IsNullOrEmpty(option.RollRange) && IsRollInRange(roll, option.RollRange))
            {
                return option;
            }
        }

        // If no exact match, pick a random one
        return options[_random.Next(options.Count)];
    }

    private bool IsRollInRange(int roll, string rollRange)
    {
        try
        {
            var parts = rollRange.Split('-');
            if (parts.Length == 2)
            {
                var min = int.Parse(parts[0].Trim());
                var max = int.Parse(parts[1].Trim());
                return roll >= min && roll <= max;
            }

            // Single value
            if (int.TryParse(rollRange.Trim(), out var single))
            {
                return roll == single;
            }
        }
        catch
        {
            // Invalid format, skip
        }

        return false;
    }

    private FeatFlawOption? ParseFeatFlaw(JsonElement element, string category)
    {
        try
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var effect = element.GetProperty("effect").GetString() ?? string.Empty;
            var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;

            var option = new FeatFlawOption
            {
                Name = name,
                DisplayName = name,
                Description = effect,
                Effect = effect,
                Type = type ?? "permanent",
                Category = category,
                IconClass = category == "feat" ? "fas fa-star" : "fas fa-minus-circle"
            };

            // Override category if specified in JSON (for special categories like "xp_penalty")
            if (element.TryGetProperty("category", out var categoryElement) && categoryElement.ValueKind != JsonValueKind.Null)
            {
                option.Category = categoryElement.GetString() ?? category;
            }

            if (element.TryGetProperty("roll", out var rollElement) && rollElement.ValueKind != JsonValueKind.Null)
            {
                option.Roll = rollElement.GetInt32();
            }

            if (element.TryGetProperty("rollRange", out var rollRangeElement) && rollRangeElement.ValueKind != JsonValueKind.Null)
            {
                option.RollRange = rollRangeElement.GetString();
            }

            // Read xpModifier for Slow Learner and similar flaws
            if (element.TryGetProperty("xpModifier", out var xpModifierElement) && xpModifierElement.ValueKind != JsonValueKind.Null)
            {
                option.XpModifier = xpModifierElement.GetInt32();
            }

            // Parse effect objects if present
            if (element.TryGetProperty("effects", out var effectsElement) && effectsElement.ValueKind == JsonValueKind.Array)
            {
                option.Effects = ParseEffectsArray(effectsElement);
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
            var effect = new Dictionary<string, object>();

            foreach (var property in effectElement.EnumerateObject())
            {
                var value = ParseJsonValue(property.Value);
                if (value != null)
                {
                    effect[property.Name] = value;
                }
            }

            effects.Add(effect);
        }

        return effects;
    }

    private object? ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => ParseJsonArray(element),
            JsonValueKind.Object => ParseJsonObject(element),
            _ => null
        };
    }

    private List<object> ParseJsonArray(JsonElement arrayElement)
    {
        var list = new List<object>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            var value = ParseJsonValue(item);
            if (value != null)
            {
                list.Add(value);
            }
        }
        return list;
    }

    private Dictionary<string, object> ParseJsonObject(JsonElement objectElement)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in objectElement.EnumerateObject())
        {
            var value = ParseJsonValue(property.Value);
            if (value != null)
            {
                dict[property.Name] = value;
            }
        }
        return dict;
    }
}
