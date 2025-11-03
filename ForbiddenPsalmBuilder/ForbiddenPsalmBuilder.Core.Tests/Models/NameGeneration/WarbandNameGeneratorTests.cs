using ForbiddenPsalmBuilder.Core.Models.NameGeneration;
using System.Text.Json;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.NameGeneration;

public class WarbandNameGeneratorTests
{
    private readonly WarbandNameData _testData;
    private readonly WarbandNameGenerator _generator;

    public WarbandNameGeneratorTests()
    {
        _testData = new WarbandNameData
        {
            Patterns = new List<string>
            {
                "[group] of [adjective] [occupation]",
                "the [adjective] [group]",
                "the [occupation]",
                "[occupation] [location]"
            },
            Group = new List<string> { "Band", "Guild", "Order" },
            Occupation = new List<string> { "Knights", "Wizards", "Thieves" },
            Adjective = new List<string> { "Dark", "Ancient", "Wild" },
            Location = new List<string> { "of the Woods", "of the Mountains" },
            LocationTemplates = new List<string> { "of the [adjective] Forest", "of the [color] [animal]" },
            Color = new List<string> { "Black", "Silver", "Golden" },
            Animal = new List<string> { "Wolf", "Raven", "Dragon" },
            Shape = new List<string> { "Circle", "Cube" },
            Atmosphere = new List<string> { "Silent", "Howling" }
        };

        // Use fixed seed for predictable testing
        _generator = new WarbandNameGenerator(_testData, new Random(42));
    }

    [Fact]
    public void GenerateName_ShouldReturnNonEmptyString()
    {
        var name = _generator.GenerateName();

        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void GenerateName_ShouldReplaceAllTokens()
    {
        var name = _generator.GenerateName();

        // Should not contain any unprocessed tokens
        Assert.DoesNotContain("[", name);
        Assert.DoesNotContain("]", name);
    }

    [Fact]
    public void GenerateName_WithSimplePattern_ShouldUseCorrectTokens()
    {
        var simpleData = new WarbandNameData
        {
            Patterns = new List<string> { "[group] of [occupation]" },
            Group = new List<string> { "Guild" },
            Occupation = new List<string> { "Thieves" }
        };

        var simpleGenerator = new WarbandNameGenerator(simpleData);
        var name = simpleGenerator.GenerateName();

        Assert.Equal("Guild of Thieves", name);
    }

    [Fact]
    public void GenerateName_WithLocationTemplates_ShouldProcessNestedTokens()
    {
        var locationData = new WarbandNameData
        {
            Patterns = new List<string> { "[group] [location]" },
            Group = new List<string> { "Order" },
            Location = new List<string>(), // Empty static locations
            LocationTemplates = new List<string> { "of the [color] [animal]" },
            Color = new List<string> { "Black" },
            Animal = new List<string> { "Wolf" }
        };

        var locationGenerator = new WarbandNameGenerator(locationData);
        var name = locationGenerator.GenerateName();

        Assert.Equal("Order of the Black Wolf", name);
    }

    [Fact]
    public void GenerateName_ShouldHandleEmptyTokenArrays()
    {
        var emptyData = new WarbandNameData
        {
            Patterns = new List<string> { "[group] of [nonexistent]" },
            Group = new List<string> { "Band" }
        };

        var emptyGenerator = new WarbandNameGenerator(emptyData);
        var name = emptyGenerator.GenerateName();

        Assert.Contains("Band", name);
        Assert.Contains("[nonexistent]", name); // Should leave unknown tokens as-is
    }

    [Fact]
    public async Task LoadFromFileAsync_WithValidJson_ShouldCreateGenerator()
    {
        var tempFile = Path.GetTempFileName();
        var testJson = """
        {
            "patterns": ["[group] of [occupation]"],
            "group": ["Guild"],
            "occupation": ["Merchants"]
        }
        """;

        await File.WriteAllTextAsync(tempFile, testJson);

        try
        {
            var generator = await WarbandNameGenerator.LoadFromFileAsync(tempFile);
            var name = generator.GenerateName();

            Assert.Equal("Guild of Merchants", name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_WithInvalidJson_ShouldThrowException()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "invalid json");

        try
        {
            await Assert.ThrowsAsync<JsonException>(() =>
                WarbandNameGenerator.LoadFromFileAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GenerateName_MultipleCalls_ShouldProduceDifferentResults()
    {
        var names = new HashSet<string>();

        // Generate multiple names
        for (int i = 0; i < 50; i++)
        {
            var name = _generator.GenerateName();
            names.Add(name);
        }

        // Should have generated some variety (not all the same)
        Assert.True(names.Count > 1, "Generator should produce varied results");
    }

    [Fact]
    public void GenerateName_WithDeterministicRandom_ShouldBeReproducible()
    {
        var generator1 = new WarbandNameGenerator(_testData, new Random(123));
        var generator2 = new WarbandNameGenerator(_testData, new Random(123));

        var name1 = generator1.GenerateName();
        var name2 = generator2.GenerateName();

        Assert.Equal(name1, name2);
    }

    [Fact]
    public void GenerateName_ShouldCapitalizeFirstLetter()
    {
        var lowercaseData = new WarbandNameData
        {
            Patterns = new List<string> { "[group] of [occupation]" },
            Group = new List<string> { "band" },
            Occupation = new List<string> { "thieves" }
        };

        var generator = new WarbandNameGenerator(lowercaseData);
        var name = generator.GenerateName();

        Assert.StartsWith("B", name); // Should start with capital B
        Assert.Equal("Band of thieves", name); // Only first letter should be capitalized
    }

    [Fact]
    public void GenerateName_WithAlreadyCapitalizedName_ShouldRemainCapitalized()
    {
        var capitalizedData = new WarbandNameData
        {
            Patterns = new List<string> { "[group] of [occupation]" },
            Group = new List<string> { "Band" },
            Occupation = new List<string> { "Thieves" }
        };

        var generator = new WarbandNameGenerator(capitalizedData);
        var name = generator.GenerateName();

        Assert.StartsWith("B", name);
        Assert.Equal("Band of Thieves", name);
    }
}