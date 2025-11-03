using System.Text.Json;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Services;

public class SpellService
{
    private readonly IEmbeddedResourceService _resourceService;
    private readonly Dictionary<string, List<SpellOption>> _spellsCache = new();

    public SpellService(IEmbeddedResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<List<SpellOption>> GetSpellsAsync(string gameVariant)
    {
        if (_spellsCache.ContainsKey(gameVariant))
        {
            return _spellsCache[gameVariant];
        }

        var data = await _resourceService.GetGameResourceAsync<JsonDocument>(gameVariant, "spells.json");
        if (data == null)
            return new List<SpellOption>();

        var spellsArray = data.RootElement.GetProperty("spells");

        var spells = new List<SpellOption>();

        foreach (var spellElement in spellsArray.EnumerateArray())
        {
            var spell = ParseSpell(spellElement);
            if (spell != null)
            {
                spells.Add(spell);
            }
        }

        _spellsCache[gameVariant] = spells;
        return spells;
    }

    public async Task<List<SpellOption>> GetSpellsByCategoryAsync(string gameVariant, string spellCategory)
    {
        var allSpells = await GetSpellsAsync(gameVariant);
        return allSpells.Where(s => s.SpellCategory == spellCategory).ToList();
    }

    public async Task<SpellOption?> RollRandomSpellAsync(string gameVariant, string spellCategory)
    {
        var categorySpells = await GetSpellsByCategoryAsync(gameVariant, spellCategory);

        if (categorySpells.Count == 0)
            return null;

        var random = new Random();
        var index = random.Next(categorySpells.Count);
        return categorySpells[index];
    }

    private SpellOption? ParseSpell(JsonElement element)
    {
        try
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var effect = element.GetProperty("effect").GetString() ?? string.Empty;
            var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var category = element.TryGetProperty("category", out var categoryElement) ? categoryElement.GetString() : null;

            var spell = new SpellOption
            {
                Name = name,
                DisplayName = name,
                Description = effect,
                Effect = effect,
                Type = type ?? "utility",
                SpellCategory = category ?? string.Empty,
                Category = "spell",
                IconClass = "fas fa-magic"
            };

            if (element.TryGetProperty("cost", out var costElement) && costElement.ValueKind != JsonValueKind.Null)
            {
                spell.Cost = costElement.GetInt32();
            }

            // Parse effect objects if present
            if (element.TryGetProperty("effects", out var effectsElement) && effectsElement.ValueKind == JsonValueKind.Array)
            {
                spell.Effects = ParseEffectsArray(effectsElement);
            }

            return spell;
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
