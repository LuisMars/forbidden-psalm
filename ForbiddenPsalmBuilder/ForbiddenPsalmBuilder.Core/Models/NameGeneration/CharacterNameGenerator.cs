using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Models.NameGeneration;

public class CharacterNameData28Psalms
{
    public List<string> Names { get; set; } = new();
}

public class CharacterNameDataEndTimes
{
    public List<string> FirstNames { get; set; } = new();
    public List<string> Titles { get; set; } = new();
    public List<string> CompleteNames { get; set; } = new();
}

public class CharacterNameDataLastWar
{
    public List<string> FirstNames { get; set; } = new();
    public List<string> TitlesPrefixes { get; set; } = new();
    public List<string> TitlesSuffixes { get; set; } = new();
}

public class CharacterNameGenerator
{
    private readonly Random _random;
    private readonly IEmbeddedResourceService _resourceService;

    private CharacterNameData28Psalms? _customData28Psalms;
    private CharacterNameDataEndTimes? _customDataEndTimes;
    private CharacterNameDataLastWar? _customDataLastWar;

    private CharacterNameData28Psalms? _cachedData28Psalms;
    private CharacterNameDataEndTimes? _cachedDataEndTimes;
    private CharacterNameDataLastWar? _cachedDataLastWar;

    public CharacterNameGenerator(IEmbeddedResourceService? resourceService = null, Random? random = null)
    {
        _resourceService = resourceService ?? new EmbeddedResourceService();
        _random = random ?? new Random();
    }

    public CharacterNameGenerator(
        CharacterNameData28Psalms? data28Psalms,
        CharacterNameDataEndTimes? dataEndTimes,
        CharacterNameDataLastWar? dataLastWar,
        Random? random = null,
        IEmbeddedResourceService? resourceService = null)
    {
        _resourceService = resourceService ?? new EmbeddedResourceService();
        _random = random ?? new Random();
        _customData28Psalms = data28Psalms;
        _customDataEndTimes = dataEndTimes;
        _customDataLastWar = dataLastWar;
    }

    public async Task<string> GenerateNameAsync(string gameVariant)
    {
        return gameVariant switch
        {
            "28-psalms" => await Generate28PsalmsNameAsync(),
            "end-times" => await GenerateEndTimesNameAsync(),
            "last-war" => await GenerateLastWarNameAsync(),
            _ => "Unnamed"
        };
    }

    private async Task<string> Generate28PsalmsNameAsync()
    {
        var data = _customData28Psalms ?? await LoadData28PsalmsAsync();
        if (data.Names.Count == 0) return "Unnamed";
        return data.Names[_random.Next(data.Names.Count)];
    }

    private async Task<string> GenerateEndTimesNameAsync()
    {
        var data = _customDataEndTimes ?? await LoadDataEndTimesAsync();

        // 50% chance to use a complete name, 50% chance to combine firstName + title
        if (_random.Next(2) == 0 && data.CompleteNames.Count > 0)
        {
            return data.CompleteNames[_random.Next(data.CompleteNames.Count)];
        }

        // Otherwise always combine firstName + title
        if (data.FirstNames.Count == 0) return "Unnamed";
        var firstName = data.FirstNames[_random.Next(data.FirstNames.Count)];

        if (data.Titles.Count > 0)
        {
            var title = data.Titles[_random.Next(data.Titles.Count)];
            return $"{firstName} {title}";
        }

        return firstName;
    }

    private async Task<string> GenerateLastWarNameAsync()
    {
        var data = _customDataLastWar ?? await LoadDataLastWarAsync();

        if (data.FirstNames.Count == 0) return "Unnamed";
        var firstName = data.FirstNames[_random.Next(data.FirstNames.Count)];

        var hasPrefix = data.TitlesPrefixes.Count > 0 && _random.Next(3) == 0; // 33% chance
        var hasSuffix = data.TitlesSuffixes.Count > 0 && _random.Next(3) == 0; // 33% chance

        var name = firstName;

        if (hasPrefix)
        {
            var prefix = data.TitlesPrefixes[_random.Next(data.TitlesPrefixes.Count)];
            name = $"{prefix} {name}";
        }

        if (hasSuffix)
        {
            var suffix = data.TitlesSuffixes[_random.Next(data.TitlesSuffixes.Count)];
            name = $"{name} {suffix}";
        }

        return name;
    }

    private async Task<CharacterNameData28Psalms> LoadData28PsalmsAsync()
    {
        if (_cachedData28Psalms != null) return _cachedData28Psalms;

        var data = await _resourceService.GetGameResourceAsync<CharacterNameData28Psalms>("28-psalms", "names.json")
            ?? new CharacterNameData28Psalms();
        _cachedData28Psalms = data;
        return data;
    }

    private async Task<CharacterNameDataEndTimes> LoadDataEndTimesAsync()
    {
        if (_cachedDataEndTimes != null) return _cachedDataEndTimes;

        var data = await _resourceService.GetGameResourceAsync<CharacterNameDataEndTimes>("end-times", "names.json")
            ?? new CharacterNameDataEndTimes();
        _cachedDataEndTimes = data;
        return data;
    }

    private async Task<CharacterNameDataLastWar> LoadDataLastWarAsync()
    {
        if (_cachedDataLastWar != null) return _cachedDataLastWar;

        var data = await _resourceService.GetGameResourceAsync<CharacterNameDataLastWar>("last-war", "names.json")
            ?? new CharacterNameDataLastWar();
        _cachedDataLastWar = data;
        return data;
    }
}