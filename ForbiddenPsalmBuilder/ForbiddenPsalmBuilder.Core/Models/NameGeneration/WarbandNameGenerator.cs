using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForbiddenPsalmBuilder.Core.Models.NameGeneration;

public class WarbandNameData
{
    public List<string> Patterns { get; set; } = new();
    public List<string> Group { get; set; } = new();
    public List<string> Occupation { get; set; } = new();
    public List<string> Adjective { get; set; } = new();
    public List<string> Location { get; set; } = new();
    public List<string> LocationTemplates { get; set; } = new();
    public List<string> Shape { get; set; } = new();
    public List<string> Atmosphere { get; set; } = new();
    public List<string> Color { get; set; } = new();
    public List<string> Animal { get; set; } = new();
}

public class WarbandNameGenerator
{
    private readonly WarbandNameData _data;
    private readonly Random _random;

    public WarbandNameGenerator(WarbandNameData data, Random? random = null)
    {
        _data = data;
        _random = random ?? new Random();
    }

    public static async Task<WarbandNameGenerator> LoadFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<WarbandNameData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data == null)
            throw new InvalidOperationException("Failed to deserialize warband name data");

        return new WarbandNameGenerator(data);
    }

    public string GenerateName()
    {
        var pattern = GetRandomItem(_data.Patterns);
        var result = ProcessPattern(pattern);
        return CapitalizeFirstLetter(result);
    }

    private string ProcessPattern(string pattern)
    {
        var result = Regex.Replace(pattern, @"\[(\w+)\]", match =>
        {
            var tokenType = match.Groups[1].Value.ToLowerInvariant();
            return GetRandomToken(tokenType);
        });

        return result;
    }

    private string GetRandomToken(string tokenType)
    {
        return tokenType switch
        {
            "group" => GetRandomItem(_data.Group),
            "occupation" => GetRandomItem(_data.Occupation),
            "adjective" => GetRandomItem(_data.Adjective),
            "location" => GetRandomLocation(),
            "shape" => GetRandomItem(_data.Shape),
            "atmosphere" => GetRandomItem(_data.Atmosphere),
            "color" => GetRandomItem(_data.Color),
            "animal" => GetRandomItem(_data.Animal),
            _ => $"[{tokenType}]"
        };
    }

    private string GetRandomLocation()
    {
        var allLocations = new List<string>(_data.Location);

        foreach (var template in _data.LocationTemplates)
        {
            var processedTemplate = ProcessPattern(template);
            allLocations.Add(processedTemplate);
        }

        return GetRandomItem(allLocations);
    }

    private string GetRandomItem(List<string> items)
    {
        if (items.Count == 0) return "";
        return items[_random.Next(items.Count)];
    }

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.Length == 1) return input.ToUpper();
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}