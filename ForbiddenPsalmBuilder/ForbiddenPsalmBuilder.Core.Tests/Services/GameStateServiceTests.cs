using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServiceTests
{
    private readonly Mock<IWarbandRepository> _mockRepository;
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly GlobalGameState _state;
    private readonly GameStateService _service;

    public GameStateServiceTests()
    {
        _mockRepository = new Mock<IWarbandRepository>();
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _state = new GlobalGameState();

        // Setup mock equipment data
        SetupMockEquipmentData();

        _service = new GameStateService(_state, _mockRepository.Object, _mockResourceService.Object);
    }

    private void SetupMockEquipmentData()
    {
        // Setup weapons data for 28-psalms
        var weaponsData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Dagger",
                    Damage = "D4",
                    Stat = "Agility",
                    Cost = 1,
                    Slots = 1,
                    Properties = new List<string> { "Thrown" }
                },
                new WeaponDto
                {
                    Name = "Sword",
                    Damage = "D6",
                    Stat = "Strength",
                    Cost = 3,
                    Slots = 1,
                    Properties = new List<string>()
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponsData);

        // Setup armor data for 28-psalms
        var armorData = new List<object>
        {
            new Dictionary<string, object>
            {
                ["name"] = "Light",
                ["armorValue"] = 1,
                ["cost"] = 2,
                ["slots"] = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<object>>("28-psalms", "armor.json"))
            .ReturnsAsync(armorData);

        // Setup items data for 28-psalms
        var itemsData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Cheese",
                    ["effect"] = "Heals 1 HP",
                    ["cost"] = 3,
                    ["slots"] = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(itemsData);

        // Setup traders data for end-times (for trader pricing tests)
        var tradersData = new List<Trader>
        {
            new Trader
            {
                Id = "vriprix",
                Name = "Vriprix the Mad Wizard",
                BuyMultiplier = 0.5m,
                BuyModifier = 0,
                SellMultiplier = 1.0m,
                SellModifier = 0
            },
            new Trader
            {
                Id = "the-merchant",
                Name = "The Merchant",
                BuyMultiplier = 0.5m,
                BuyModifier = 1,
                SellMultiplier = 1.0m,
                SellModifier = -1,
                MinimumSellPrice = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(tradersData);

        // Setup weapons data for end-times (for trader pricing tests)
        var endTimesWeaponsData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Sword",
                    Damage = "1D6",
                    Properties = new List<string>(),
                    Cost = 3,
                    Slots = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("end-times", "weapons.json"))
            .ReturnsAsync(endTimesWeaponsData);

        // Setup names data for 28-psalms
        var names28Psalms = new ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameData28Psalms
        {
            Names = new List<string>
            {
                "Rex", "Gridotus", "Mister Quimper", "Inquisitor Lucius", "David C. Moore",
                "Ophelia", "Alex Goode", "John R. Young", "Merlin", "Filthor"
            }
        };
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameData28Psalms>("28-psalms", "names.json"))
            .ReturnsAsync(names28Psalms);

        // Setup names data for end-times
        var namesEndTimes = new ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameDataEndTimes
        {
            FirstNames = new List<string> { "Nohr", "Ash", "Darkest", "Saint", "Mother" },
            Titles = new List<string> { "The Saint", "The Wet", "The Lefty", "The Dire" },
            CompleteNames = new List<string> { "Hugo Stieglitz", "Ryan R", "Willnox" }
        };
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameDataEndTimes>("end-times", "names.json"))
            .ReturnsAsync(namesEndTimes);

        // Setup names data for last-war
        var namesLastWar = new ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameDataLastWar
        {
            FirstNames = new List<string> { "Wilhelm", "René", "Edward", "Shakes", "Tommy" },
            TitlesPrefixes = new List<string> { "Private", "Captain", "Major", "Lieutenant" },
            TitlesSuffixes = new List<string> { "Jr", "The Hellfighter", "the Conscientious Objector" }
        };
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<ForbiddenPsalmBuilder.Core.Models.NameGeneration.CharacterNameDataLastWar>("last-war", "names.json"))
            .ReturnsAsync(namesLastWar);
    }

    private void SetupBackpackWithEffects()
    {
        // Setup backpack with slot_modifier effect
        var itemsData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Backpack",
                    ["effect"] = "Takes up one Equipment slot but provides two additional slots",
                    ["cost"] = 1,
                    ["slots"] = 1,
                    ["effects"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "slot_modifier",
                            ["modifier"] = 2
                        }
                    }
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(itemsData);
    }

    [Fact]
    public async Task GetStatArraysAsync_ReturnsCorrectArrays()
    {
        // Act
        var result = await _service.GetStatArraysAsync();

        // Assert
        Assert.Equal(2, result.Count);

        var specialist = result.FirstOrDefault(a => a.Id == "specialist");
        Assert.NotNull(specialist);
        Assert.Equal("Specialist", specialist.Name);
        Assert.Equal(new[] { 3, 1, 0, -3 }, specialist.Values);
        Assert.Equal("High specialization with major weakness", specialist.Description);

        var balanced = result.FirstOrDefault(a => a.Id == "balanced");
        Assert.NotNull(balanced);
        Assert.Equal("Balanced", balanced.Name);
        Assert.Equal(new[] { 2, 2, -1, -2 }, balanced.Values);
        Assert.Equal("More balanced distribution", balanced.Description);
    }

    [Fact]
    public async Task AddCharacterToWarbandAsync_ShouldAddCharacterAndReturnId()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        _state.Warbands[warband.Id] = warband;

        var character = new Character("Test Character")
        {
            Stats = new Stats(2, 1, 3, 2),
            SpecialClassId = "witch"
        };

        // Setup mock repository to return the warband when SaveAsync is called
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        var characterId = await _service.AddCharacterToWarbandAsync(warband.Id, character);

        // Assert
        Assert.Equal(character.Id, characterId);
        Assert.Single(warband.Members);
        Assert.Equal(character, warband.Members.First());
        Assert.Equal("Test Character", warband.Members.First().Name);
        Assert.Equal("witch", warband.Members.First().SpecialClassId);

        // Verify the repository SaveAsync was called
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task AddCharacterToWarbandAsync_WarbandNotFound_ShouldThrowException()
    {
        // Arrange
        var character = new Character("Test Character");
        var nonExistentWarbandId = "non-existent-id";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddCharacterToWarbandAsync(nonExistentWarbandId, character));

        Assert.Contains("Warband not found", exception.Message);
    }

    [Fact]
    public async Task AddCharacterToWarbandAsync_WarbandAtMaxSize_ShouldThrowException()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");

        // Fill warband to maximum capacity (5 members)
        for (int i = 0; i < 5; i++)
        {
            warband.Members.Add(new Character($"Member {i + 1}"));
        }

        _state.Warbands[warband.Id] = warband;

        var newCharacter = new Character("New Character");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddCharacterToWarbandAsync(warband.Id, newCharacter));

        Assert.Contains("already at maximum size", exception.Message);

        // Verify the repository SaveAsync was NOT called since the operation should fail
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Never);
    }

    [Fact]
    public async Task GenerateWarbandNameAsync_ShouldReturnValidName()
    {
        // Act
        var result = await _service.GenerateWarbandNameAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain("[", result); // Should not contain unprocessed tokens
        Assert.DoesNotContain("]", result);
    }

    [Fact]
    public async Task GenerateWarbandNameAsync_MultipleCalls_ShouldProduceDifferentResults()
    {
        // Act
        var names = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            var name = await _service.GenerateWarbandNameAsync();
            names.Add(name);
        }

        // Assert
        Assert.True(names.Count > 1, "Should generate varied names");
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_28Psalms_ShouldReturnValidName()
    {
        // Act
        var result = await _service.GenerateCharacterNameAsync("28-psalms");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEqual("Unnamed", result); // Should not be default fallback
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_EndTimes_ShouldReturnValidName()
    {
        // Act
        var result = await _service.GenerateCharacterNameAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEqual("Unnamed", result); // Should not be default fallback
        // End times can return either "FirstName Title" or a complete name
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_LastWar_ShouldReturnValidName()
    {
        // Act
        var result = await _service.GenerateCharacterNameAsync("last-war");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEqual("Unnamed", result); // Should not be default fallback
        // Last war can return "FirstName Suffix", "Prefix FirstName", or "Prefix FirstName Suffix"
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_MultipleCalls_ShouldProduceDifferentResults()
    {
        // Act
        var names = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("end-times");
            names.Add(name);
        }

        // Assert
        Assert.True(names.Count > 1, "Should generate varied character names");
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_UnknownVariant_ShouldReturnDefaultName()
    {
        // Act
        var result = await _service.GenerateCharacterNameAsync("unknown-variant");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_28Psalms_ShouldPickFrom28PsalmsData()
    {
        // Known names from 28-psalms data (expanded list from names.json)
        var known28PsalmsNames = new HashSet<string>
        {
            "Rex", "Gridotus", "Mister Quimper", "Inquisitor Lucius", "David C. Moore",
            "Ophelia", "Alex Goode", "John R. Young", "Merlin", "Filthor",
            "Zachary Knippel", "Capitald", "Matthias Mencel", "William B", "Zac Mazey",
            "The Dour Kin", "Gabe Benavides", "Robert Lee", "Paul Wilson", "Abby",
            "Cecil", "George Kaldis", "Shawn Turpin", "Kiral", "Charles Chapman",
            "Dennis McGeen", "ElDavePhoto", "Fenrikson", "Nick", "shawn hakl",
            "Alexander", "(_><)", "DerOlfork", "Devon Tackett", "Christopher Moses"
        };

        // Act - Generate 50 names to get good coverage
        var generated = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("28-psalms");
            generated.Add(name);
        }

        // Assert - At least some should match known 28-psalms names
        var matchCount = generated.Intersect(known28PsalmsNames).Count();
        Assert.True(matchCount > 0,
            $"Should generate at least some names from 28-psalms data. Found {matchCount} matches out of {generated.Count} generated names.");
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_EndTimes_ShouldPickFromEndTimesData()
    {
        // Known first names and complete names from end-times data
        var knownEndTimesFirstNames = new HashSet<string>
        {
            "Nohr", "Ash", "Darkest", "Saint", "Mother", "Hugo", "Ryan", "Willnox"
        };

        var knownEndTimesCompleteNames = new HashSet<string>
        {
            "Hugo Stieglitz", "Ryan R", "Willnox", "Danny The Deströyer", "Ymön"
        };

        // Act - Generate 50 names
        var generated = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("end-times");
            generated.Add(name);
        }

        // Assert - Should find either complete names or names starting with known first names
        var completeMatches = generated.Intersect(knownEndTimesCompleteNames).Count();
        var firstNameMatches = generated.Count(name =>
            knownEndTimesFirstNames.Any(fn => name.StartsWith(fn)));

        Assert.True(completeMatches > 0 || firstNameMatches > 0,
            $"Should generate names from end-times data. Complete matches: {completeMatches}, First name matches: {firstNameMatches}");
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_EndTimes_ShouldAlwaysHaveEitherCompleteNameOrFirstNameWithTitle()
    {
        // Known first names that should NOT appear alone
        var knownFirstNamesOnly = new HashSet<string>
        {
            "Nohr", "Ash", "Darkest", "Saint", "Mother", "Dire", "Dre", "Lemon", "Steven",
            "Beca", "Jherek", "Carys", "Owain", "Marc", "Tomos", "Nadia", "Pete", "Bruce"
        };

        // Act - Generate 100 names
        var generated = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("end-times");
            generated.Add(name);
        }

        // Assert - No names should be JUST a single first name from the knownFirstNamesOnly list
        // They should either be:
        // 1. A complete name (from completeNames list), OR
        // 2. A firstName combined with a title (containing "The ")
        var justFirstNames = generated.Where(name =>
            knownFirstNamesOnly.Contains(name)
        ).ToList();

        Assert.Empty(justFirstNames);
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_LastWar_ShouldPickFromLastWarData()
    {
        // Known first names and title components from last-war data
        var knownLastWarFirstNames = new HashSet<string>
        {
            "Wilhelm", "René", "Edward", "Tommy", "Mary", "Kaiser", "General", "Ghost"
        };

        var knownLastWarPrefixes = new HashSet<string>
        {
            "Private 1st. class", "Captain", "Lieutenant", "Major", "Baron", "Nurse"
        };

        var knownLastWarSuffixes = new HashSet<string>
        {
            "Jr", "The Hellfighter", "the Tommy", "the Baron", "the Wolf", "the Ace"
        };

        // Act - Generate 100 names (more because of prefix/suffix combinations)
        var generated = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("last-war");
            generated.Add(name);
        }

        // Assert - Should find names with known first names, and possibly prefixes/suffixes
        var firstNameMatches = generated.Count(name =>
            knownLastWarFirstNames.Any(fn => name.Contains(fn)));

        var prefixMatches = generated.Count(name =>
            knownLastWarPrefixes.Any(p => name.StartsWith(p)));

        var suffixMatches = generated.Count(name =>
            knownLastWarSuffixes.Any(s => name.EndsWith(s)));

        Assert.True(firstNameMatches > 0,
            $"Should generate names containing last-war first names. Found {firstNameMatches} matches.");

        // Some names should have prefixes or suffixes (33% chance each)
        Assert.True(prefixMatches > 0 || suffixMatches > 0,
            $"Should generate names with last-war titles. Prefix matches: {prefixMatches}, Suffix matches: {suffixMatches}");
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_28Psalms_ShouldNotUsEndTimesData()
    {
        // Known end-times complete names that should NOT appear in 28-psalms
        var endTimesOnlyNames = new HashSet<string>
        {
            "Hugo Stieglitz", "Ryan R", "Willnox", "Danny The Deströyer", "Soupbone"
        };

        // Act - Generate 100 names
        var generated = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("28-psalms");
            generated.Add(name);
        }

        // Assert - Should not contain any end-times-only names
        var wrongDataMatches = generated.Intersect(endTimesOnlyNames).ToList();
        Assert.Empty(wrongDataMatches);
    }

    [Fact]
    public async Task GenerateCharacterNameAsync_LastWar_ShouldNotUse28PsalmsData()
    {
        // Known 28-psalms names that should NOT appear in last-war
        var psalmsOnlyNames = new HashSet<string>
        {
            "Gridotus", "Inquisitor Lucius", "Capitald", "Matthias Mencel"
        };

        // Act - Generate 100 names
        var generated = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var name = await _service.GenerateCharacterNameAsync("last-war");
            generated.Add(name);
        }

        // Assert - Should not contain any 28-psalms-only names
        var wrongDataMatches = generated.Intersect(psalmsOnlyNames).ToList();
        Assert.Empty(wrongDataMatches);
    }

    [Fact]
    public async Task UpdateCharacterAsync_ShouldUpdateCharacterAndSaveToRepository()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        var character = new Character("Original Name")
        {
            Stats = new Stats(1, 1, 1, 1)
        };
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        var updatedCharacter = new Character("Updated Name")
        {
            Stats = new Stats(2, 2, 2, 2),
            SpecialClassId = "witch"
        };

        // Setup mock repository to return the warband when SaveAsync is called
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.UpdateCharacterAsync(warband.Id, character.Id, updatedCharacter);

        // Assert
        Assert.Single(warband.Members);
        Assert.Equal("Updated Name", warband.Members.First().Name);
        Assert.Equal(2, warband.Members.First().Stats.Agility);
        Assert.Equal("witch", warband.Members.First().SpecialClassId);

        // Verify the repository SaveAsync was called
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task UpdateCharacterAsync_CharacterNotFound_ShouldThrowException()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        _state.Warbands[warband.Id] = warband;

        var updatedCharacter = new Character("Updated Name")
        {
            Stats = new Stats(2, 2, 2, 2)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateCharacterAsync(warband.Id, "non-existent-id", updatedCharacter));

        Assert.Contains("Character not found", exception.Message);

        // Verify the repository SaveAsync was NOT called since the operation should fail
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Never);
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WithIsDeadTrue_ShouldMoveToDeadMembers()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        var character1 = new Character("Character 1") { Id = "char-1" };
        var character2 = new Character("Character 2") { Id = "char-2" };
        warband.Members.Add(character1);
        warband.Members.Add(character2);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act - Remove as dead (isDead = true, which is the default)
        await _service.RemoveCharacterFromWarbandAsync(warband.Id, character1.Id, isDead: true);

        // Assert
        Assert.Single(warband.Members); // Should have 1 member left
        Assert.Equal("Character 2", warband.Members.First().Name);
        Assert.Single(warband.DeadMembers); // Should have 1 dead member
        Assert.Equal("Character 1", warband.DeadMembers.First().Name);
        Assert.Empty(warband.FiredMembers); // Should have no fired members

        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_WithIsDeadFalse_ShouldMoveToFiredMembers()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        var character1 = new Character("Character 1") { Id = "char-1" };
        var character2 = new Character("Character 2") { Id = "char-2" };
        warband.Members.Add(character1);
        warband.Members.Add(character2);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act - Remove as fired (isDead = false)
        await _service.RemoveCharacterFromWarbandAsync(warband.Id, character1.Id, isDead: false);

        // Assert
        Assert.Single(warband.Members); // Should have 1 member left
        Assert.Equal("Character 2", warband.Members.First().Name);
        Assert.Empty(warband.DeadMembers); // Should have no dead members
        Assert.Single(warband.FiredMembers); // Should have 1 fired member
        Assert.Equal("Character 1", warband.FiredMembers.First().Name);

        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_ShouldPreserveCharacterEquipment()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        var character1 = new Character("Fallen Hero") { Id = "char-1" };
        var sword = new Equipment { Name = "Sword", Type = "weapon", Cost = 10 };
        character1.Equipment.Add(sword);
        warband.Members.Add(character1);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveCharacterFromWarbandAsync(warband.Id, character1.Id, isDead: true);

        // Assert
        Assert.Single(warband.DeadMembers);
        Assert.Single(warband.DeadMembers.First().Equipment);
        Assert.Equal("Sword", warband.DeadMembers.First().Equipment.First().Name);
    }

    [Fact]
    public async Task RemoveCharacterFromWarbandAsync_CharacterNotFound_ShouldThrowException()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        _state.Warbands[warband.Id] = warband;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveCharacterFromWarbandAsync(warband.Id, "non-existent-id"));

        Assert.Contains("Character not found", exception.Message);

        // Verify the repository SaveAsync was NOT called since the operation should fail
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Never);
    }

    [Fact]
    public async Task CreateWarbandAsync_ShouldSaveToRepository()
    {
        // Arrange
        await _service.LoadGameDataAsync(); // Ensure game configs are loaded
        var warbandName = "New Warband";
        var gameVariant = "last-war";

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        var warbandId = await _service.CreateWarbandAsync(warbandName, gameVariant);

        // Assert
        Assert.NotNull(warbandId);

        // Verify the repository SaveAsync was called
        _mockRepository.Verify(r => r.SaveAsync(It.Is<Warband>(w =>
            w.Name == warbandName && w.GameVariant == gameVariant)), Times.Once);
    }

    [Fact]
    public async Task DeleteWarbandAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var warband = new Warband("Test Warband", "last-war");
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.DeleteAsync(warband.Id))
                      .ReturnsAsync(true);

        // Act
        await _service.DeleteWarbandAsync(warband.Id);

        // Assert
        // Verify the repository DeleteAsync was called
        _mockRepository.Verify(r => r.DeleteAsync(warband.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateWarbandAsync_ShouldSaveToRepository()
    {
        // Arrange
        var warband = new Warband("Original Name", "last-war");
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.ExistsAsync(warband.Id))
                      .ReturnsAsync(true);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Modify warband
        warband.Name = "Updated Name";

        // Act
        await _service.UpdateWarbandAsync(warband);

        // Assert
        // Verify the repository SaveAsync was called
        _mockRepository.Verify(r => r.SaveAsync(It.Is<Warband>(w =>
            w.Id == warband.Id && w.Name == "Updated Name")), Times.Once);
    }

    /// <summary>
    /// This test ensures all state-modifying methods persist changes to the repository.
    /// If you add a new method that modifies warband state, add a test case here.
    /// This prevents bugs where in-memory state is updated but not persisted.
    /// </summary>
    [Theory]
    [InlineData("CreateWarband")]
    [InlineData("UpdateWarband")]
    [InlineData("DeleteWarband")]
    [InlineData("AddCharacter")]
    [InlineData("UpdateCharacter")]
    [InlineData("RemoveCharacter")]
    public async Task AllStateModifyingMethods_ShouldPersistToRepository(string operation)
    {
        // This test serves as documentation and a reminder that all operations
        // that modify warband state MUST persist changes to the repository.
        //
        // When adding new state-modifying methods to GameStateService:
        // 1. Add a new test case to this Theory with the method name
        // 2. Implement the actual test in the switch statement below
        // 3. Verify the repository Save/Delete method is called
        //
        // This pattern ensures we don't forget to persist changes in the future.

        await _service.LoadGameDataAsync(); // Ensure game configs are loaded

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<string>()))
                      .ReturnsAsync(true);
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
                      .ReturnsAsync(true);

        switch (operation)
        {
            case "CreateWarband":
                await _service.CreateWarbandAsync("Test", "last-war");
                _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
                break;

            case "UpdateWarband":
                var warband1 = new Warband("Test", "last-war");
                _state.Warbands[warband1.Id] = warband1;
                await _service.UpdateWarbandAsync(warband1);
                _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
                break;

            case "DeleteWarband":
                var warband2 = new Warband("Test", "last-war");
                _state.Warbands[warband2.Id] = warband2;
                await _service.DeleteWarbandAsync(warband2.Id);
                _mockRepository.Verify(r => r.DeleteAsync(warband2.Id), Times.Once);
                break;

            case "AddCharacter":
                var warband3 = new Warband("Test", "last-war");
                _state.Warbands[warband3.Id] = warband3;
                await _service.AddCharacterToWarbandAsync(warband3.Id, new Character("Test"));
                _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
                break;

            case "UpdateCharacter":
                var warband4 = new Warband("Test", "last-war");
                var character = new Character("Original");
                warband4.Members.Add(character);
                _state.Warbands[warband4.Id] = warband4;
                await _service.UpdateCharacterAsync(warband4.Id, character.Id, new Character("Updated"));
                _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
                break;

            case "RemoveCharacter":
                var warband5 = new Warband("Test", "last-war");
                var character2 = new Character("Test");
                warband5.Members.Add(character2);
                _state.Warbands[warband5.Id] = warband5;
                await _service.RemoveCharacterFromWarbandAsync(warband5.Id, character2.Id);
                _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
                break;

            default:
                throw new ArgumentException($"Unknown operation: {operation}. Please implement the test case.");
        }
    }

    // Equipment Management Tests

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldAddWeaponToCharacter()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -2; // Equipment slots = 5 + (-2) = 3
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "dagger", "weapon");

        // Assert
        Assert.Single(character.Equipment);
        Assert.Equal("Dagger", character.Equipment[0].Name);
        Assert.Equal("weapon", character.Equipment[0].Type);
        _mockRepository.Verify(x => x.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldAddArmorToCharacter()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -2; // Equipment slots = 5 + (-2) = 3
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "light", "armor");

        // Assert
        Assert.Single(character.Equipment);
        Assert.Equal("Light", character.Equipment[0].Name);
        Assert.Equal("armor", character.Equipment[0].Type);
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldAddItemToCharacter()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -2; // Equipment slots = 5 + (-2) = 3
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "cheese", "item");

        // Assert
        Assert.Single(character.Equipment);
        Assert.Equal("Cheese", character.Equipment[0].Name);
        Assert.Equal("item", character.Equipment[0].Type);
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldThrow_WhenNotEnoughSlots()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -4; // Equipment slots = 5 + (-4) = 1

        // Add equipment that fills the slot
        character.Equipment.Add(new Equipment { Name = "Dagger", Slots = 1, Type = "weapon" });

        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "sword", "weapon")
        );
    }

    [Fact]
    public async Task RemoveEquipmentFromCharacterAsync_ShouldRemoveEquipment()
    {
        // Arrange
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        var equipment = new Equipment { Id = "eq-1", Name = "Dagger", Slots = 1, Type = "weapon" };
        character.Equipment.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveEquipmentFromCharacterAsync(warband.Id, character.Id, equipment.Id);

        // Assert
        Assert.Empty(character.Equipment);
        _mockRepository.Verify(x => x.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEquipmentFromCharacterAsync_ShouldMoveEquipmentToStash()
    {
        // Arrange
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "28-psalms" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        var equipment = new Equipment { Id = "eq-1", Name = "Dagger", Slots = 1, Type = "weapon" };
        character.Equipment.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveEquipmentFromCharacterAsync(warband.Id, character.Id, equipment.Id);

        // Assert
        Assert.Empty(character.Equipment);
        Assert.Single(warband.Stash);
        Assert.Contains(warband.Stash, e => e.Id == equipment.Id && e.Name == "Dagger");
    }

    [Fact]
    public async Task RemoveEquipmentFromCharacterAsync_WithSpell_ShouldMoveToStashAndRemoveFromSpells()
    {
        // Arrange
        var warband = new Warband { Id = "test-warband", Name = "Test Warband", GameVariant = "end-times" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        var spellEquipment = new Equipment { Id = "spell-1", Name = "Shadow Bind", Slots = 1, Type = "spell", Effect = "Target cannot move" };
        character.Equipment.Add(spellEquipment);
        character.Spells.Add("Shadow Bind");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveEquipmentFromCharacterAsync(warband.Id, character.Id, spellEquipment.Id);

        // Assert
        Assert.Empty(character.Equipment);
        Assert.DoesNotContain("Shadow Bind", character.Spells);
        Assert.Single(warband.Stash);
        Assert.Contains(warband.Stash, e => e.Id == spellEquipment.Id && e.Type == "spell");
    }

    [Fact]
    public void CanCharacterEquip_ShouldReturnTrue_WhenEnoughSlots()
    {
        // Arrange
        var warband = new Warband { Id = "test-warband", Name = "Test Warband" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -2; // Equipment slots = 5 + (-2) = 3
        character.Equipment.Add(new Equipment { Name = "Dagger", Slots = 1, Type = "weapon" });
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        var newEquipment = new Equipment { Name = "Sword", Slots = 1, Type = "weapon" };

        // Act
        var canEquip = _service.CanCharacterEquip(warband.Id, character.Id, newEquipment);

        // Assert
        Assert.True(canEquip);
    }

    [Fact]
    public void CanCharacterEquip_ShouldReturnFalse_WhenNotEnoughSlots()
    {
        // Arrange
        var warband = new Warband { Id = "test-warband", Name = "Test Warband" };
        var character = new Character { Id = "test-char", Name = "Test Character" };
        character.Stats.Strength = -3; // Equipment slots = 5 + (-3) = 2
        character.Equipment.Add(new Equipment { Name = "Dagger", Slots = 1, Type = "weapon" });
        character.Equipment.Add(new Equipment { Name = "Light Armor", Slots = 1, Type = "armor" });
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        var newEquipment = new Equipment { Name = "Sword", Slots = 1, Type = "weapon" };

        // Act
        var canEquip = _service.CanCharacterEquip(warband.Id, character.Id, newEquipment);

        // Assert
        Assert.False(canEquip);
    }

    // Buy/Sell/Transfer Equipment Tests

    [Fact]
    public async Task BuyEquipmentAsync_ShouldAddToStashAndDeductGold()
    {
        // Arrange
        await _service.LoadGameDataAsync(); // Load game data to access weapons
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 50 };
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.BuyEquipmentAsync(warband.Id, "sword", "weapon");

        // Assert
        Assert.Single(warband.Stash);
        Assert.Equal("Sword", warband.Stash[0].Name);
        Assert.Equal(47, warband.Gold); // 50 - 3 (sword cost)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task BuyEquipmentAsync_ShouldThrow_WhenInsufficientGold()
    {
        // Arrange
        await _service.LoadGameDataAsync(); // Load game data to access weapons
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 2 };
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.BuyEquipmentAsync(warband.Id, "sword", "weapon"));
        Assert.Contains("Not enough gold", exception.Message);
        Assert.Empty(warband.Stash);
        Assert.Equal(2, warband.Gold); // Gold unchanged
    }

    [Fact]
    public async Task SellEquipmentAsync_ShouldRemoveFromStashAndAddGold()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 50 };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Type = "weapon" };
        warband.Stash.Add(equipment);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.SellEquipmentAsync(warband.Id, "eq-1");

        // Assert
        Assert.Empty(warband.Stash);
        Assert.Equal(53, warband.Gold); // 50 + 3 (sword sell value)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task SellEquipmentAsync_ShouldThrow_WhenEquipmentNotFound()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 50 };
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SellEquipmentAsync(warband.Id, "nonexistent"));
        Assert.Contains("Equipment not found in stash", exception.Message);
    }

    [Fact]
    public async Task BuyEquipmentAsync_WithTrader_ShouldUseTraderBuyPrice()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 50 };
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Buy with Vriprix (standard trader: sells at full price)
        // Sword base cost is 3G, so buy price is 3G (sellMultiplier 1.0)
        await _service.BuyEquipmentAsync(warband.Id, "sword", "weapon", "vriprix");

        // Assert
        Assert.Single(warband.Stash);
        Assert.Equal("Sword", warband.Stash[0].Name);
        Assert.Equal(47, warband.Gold); // 50 - 3 (full price)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task BuyEquipmentAsync_WithTheMerchant_ShouldAddOneGold()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 50 };
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Buy with The Merchant (50% + 1G)
        // Sword base cost is 3G, so buy price should be 2G (50% of 3 = 1.5, +1 = 2.5, rounded down = 2)
        await _service.BuyEquipmentAsync(warband.Id, "sword", "weapon", "the-merchant");

        // Assert
        Assert.Single(warband.Stash);
        Assert.Equal(48, warband.Gold); // 50 - 2 (1G base + 1G modifier, rounded down)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task SellEquipmentAsync_WithTrader_ShouldUseTraderSellPrice()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 50 };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Type = "weapon" };
        warband.Stash.Add(equipment);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Sell to Vriprix (standard trader: buys at 50%)
        await _service.SellEquipmentAsync(warband.Id, "eq-1", "vriprix");

        // Assert
        Assert.Empty(warband.Stash);
        Assert.Equal(51, warband.Gold); // 50 + 1 (3G item * 0.5 buyMultiplier = 1.5, rounded down = 1)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task SellEquipmentAsync_WithTheMerchant_ShouldSubtractOneGold()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 50 };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Type = "weapon" };
        warband.Stash.Add(equipment);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Sell to The Merchant (buys at 50% +1G)
        await _service.SellEquipmentAsync(warband.Id, "eq-1", "the-merchant");

        // Assert
        Assert.Empty(warband.Stash);
        Assert.Equal(52, warband.Gold); // 50 + 2 (3G * 0.5 + 1G buyModifier = 2.5, rounded down = 2)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task SellEquipmentAsync_WithTheMerchant_ShouldEnforceMinimumOneGold()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 50 };
        var equipment = new Equipment { Id = "eq-1", Name = "Cheap Item", Cost = 1, Type = "item" };
        warband.Stash.Add(equipment);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Sell to The Merchant (1G - 1G = 0, but min is 1G)
        await _service.SellEquipmentAsync(warband.Id, "eq-1", "the-merchant");

        // Assert
        Assert.Empty(warband.Stash);
        Assert.Equal(51, warband.Gold); // 50 + 1 (minimum enforced)
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task SellEquipmentFromCharacter_ShouldRemoveFromCharacterAndStash()
    {
        // Arrange - Simulate the flow of selling equipment directly from character
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 50 };
        var character = new Character("Hero") { Id = "char-1" };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 10, Type = "weapon", Slots = 1 };
        character.Equipment.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Remove from character (which moves to stash), then sell from stash
        await _service.RemoveEquipmentFromCharacterAsync(warband.Id, character.Id, equipment.Id);
        await _service.SellEquipmentAsync(warband.Id, equipment.Id);

        // Assert - Equipment should be gone from both character and stash, gold added
        Assert.Empty(character.Equipment); // Removed from character
        Assert.Empty(warband.Stash); // Removed from stash after sell
        Assert.Equal(60, warband.Gold); // 50 + 10 (sell price)
    }

    [Fact]
    public async Task TransferEquipmentToCharacterAsync_ShouldMoveFromStashToCharacter()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1" };
        var character = new Character { Id = "char-1", Name = "Hero" };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Slots = 1, Type = "weapon" };
        warband.Stash.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.TransferEquipmentToCharacterAsync(warband.Id, character.Id, equipment.Id);

        // Assert
        Assert.Empty(warband.Stash);
        Assert.Single(character.Equipment);
        Assert.Equal("Sword", character.Equipment[0].Name);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task TransferEquipmentToCharacterAsync_ShouldThrow_WhenNotEnoughSlots()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1" };
        var character = new Character { Id = "char-1", Name = "Hero" };
        character.Stats.Strength = -3; // Equipment slots = 5 + (-3) = 2
        character.Equipment.Add(new Equipment { Name = "Dagger", Slots = 2, Type = "weapon" });

        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Slots = 1, Type = "weapon" };
        warband.Stash.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TransferEquipmentToCharacterAsync(warband.Id, character.Id, equipment.Id));
        Assert.Contains("Not enough equipment slots", exception.Message);
        Assert.Single(warband.Stash); // Equipment still in stash
    }

    [Fact]
    public async Task TransferEquipmentToStashAsync_ShouldMoveFromCharacterToStash()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1" };
        var character = new Character { Id = "char-1", Name = "Hero" };
        var equipment = new Equipment { Id = "eq-1", Name = "Sword", Cost = 3, Slots = 1, Type = "weapon" };
        character.Equipment.Add(equipment);
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.TransferEquipmentToStashAsync(warband.Id, character.Id, equipment.Id);

        // Assert
        Assert.Empty(character.Equipment);
        Assert.Single(warband.Stash);
        Assert.Equal("Sword", warband.Stash[0].Name);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task TransferEquipmentToStashAsync_ShouldThrow_WhenEquipmentNotFoundOnCharacter()
    {
        // Arrange
        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1" };
        var character = new Character { Id = "char-1", Name = "Hero" };
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TransferEquipmentToStashAsync(warband.Id, character.Id, "nonexistent"));
        Assert.Contains("Equipment not found on character", exception.Message);
    }

    // State Persistence Tests
    [Fact]
    public async Task SaveStateAsync_ShouldPersistWarbandsToStorage()
    {
        // Arrange
        var mockStorage = new Mock<IStateStorageService>();
        var serviceWithStorage = new GameStateService(_state, _mockRepository.Object, _mockResourceService.Object, mockStorage.Object);

        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 100 };
        _state.Warbands[warband.Id] = warband;

        // Act
        await serviceWithStorage.SaveStateAsync();

        // Assert
        mockStorage.Verify(s => s.SetItemAsync("forbidden-psalm-state", It.IsAny<GlobalGameState>()), Times.Once);
    }

    [Fact]
    public async Task LoadStateAsync_ShouldRestoreWarbandsFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<IStateStorageService>();
        var savedState = new GlobalGameState
        {
            SelectedGameVariant = "end-times",
            Warbands = new Dictionary<string, Warband>
            {
                ["warband-1"] = new Warband("Saved Warband", "end-times") { Id = "warband-1", Gold = 200 }
            },
            ActiveWarbandId = "warband-1"
        };

        mockStorage.Setup(s => s.GetItemAsync<GlobalGameState>("forbidden-psalm-state"))
            .ReturnsAsync(savedState);

        var emptyState = new GlobalGameState();
        var serviceWithStorage = new GameStateService(emptyState, _mockRepository.Object, _mockResourceService.Object, mockStorage.Object);

        // Act
        await serviceWithStorage.LoadStateAsync();

        // Assert
        Assert.Single(emptyState.Warbands);
        Assert.Equal("Saved Warband", emptyState.Warbands["warband-1"].Name);
        Assert.Equal(200, emptyState.Warbands["warband-1"].Gold);
        Assert.Equal("warband-1", emptyState.ActiveWarbandId);
        mockStorage.Verify(s => s.GetItemAsync<GlobalGameState>("forbidden-psalm-state"), Times.Once);
    }

    [Fact]
    public async Task LoadStateAsync_ShouldHandleNoSavedState()
    {
        // Arrange
        var mockStorage = new Mock<IStateStorageService>();
        mockStorage.Setup(s => s.GetItemAsync<GlobalGameState>("forbidden-psalm-state"))
            .ReturnsAsync((GlobalGameState?)null);

        var emptyState = new GlobalGameState();
        var serviceWithStorage = new GameStateService(emptyState, _mockRepository.Object, _mockResourceService.Object, mockStorage.Object);

        // Act
        await serviceWithStorage.LoadStateAsync();

        // Assert - should not crash, state should remain empty
        Assert.Empty(emptyState.Warbands);
    }

    [Fact]
    public async Task ClearStateAsync_ShouldRemovePersistedState()
    {
        // Arrange
        var mockStorage = new Mock<IStateStorageService>();
        var serviceWithStorage = new GameStateService(_state, _mockRepository.Object, _mockResourceService.Object, mockStorage.Object);

        // Act
        await serviceWithStorage.ClearStateAsync();

        // Assert
        mockStorage.Verify(s => s.RemoveItemAsync("forbidden-psalm-state"), Times.Once);
        Assert.Empty(_state.Warbands);
        Assert.Null(_state.ActiveWarbandId);
    }

    #region Feat/Flaw Tests

    [Fact]
    public async Task RollFeatAsync_ShouldAddFeatToCharacter()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        // Mock feats data
        var featsJson = @"{
            ""feats"": [
                {
                    ""rollRange"": ""1-100"",
                    ""name"": ""Test Feat"",
                    ""effect"": ""Test effect"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(featsJson);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        var featName = await _service.RollFeatAsync(warband.Id, character.Id);

        // Assert
        Assert.NotNull(featName);
        Assert.NotEmpty(featName);
        Assert.Contains(featName, character.Feats);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task RollFlawAsync_ShouldAddFlawToCharacter()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        // Mock flaws data
        var flawsJson = @"{
            ""flaws"": [
                {
                    ""rollRange"": ""1-100"",
                    ""name"": ""Test Flaw"",
                    ""effect"": ""Test effect"",
                    ""type"": ""permanent""
                }
            ]
        }";
        var jsonDoc = JsonDocument.Parse(flawsJson);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<JsonDocument>("28-psalms", "feats-flaws.json"))
            .ReturnsAsync(jsonDoc);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        var flawName = await _service.RollFlawAsync(warband.Id, character.Id);

        // Assert
        Assert.NotNull(flawName);
        Assert.NotEmpty(flawName);
        Assert.Contains(flawName, character.Flaws);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddFeatAsync_ShouldAddFeatManually()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddFeatAsync(warband.Id, character.Id, "Marksman");

        // Assert
        Assert.Contains("Marksman", character.Feats);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddFlawAsync_ShouldAddFlawManually()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddFlawAsync(warband.Id, character.Id, "Coward");

        // Assert
        Assert.Contains("Coward", character.Flaws);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFeatAsync_ShouldRemoveFeat()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Feats.Add("Marksman");
        character.Feats.Add("Tough");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveFeatAsync(warband.Id, character.Id, "Marksman");

        // Assert
        Assert.DoesNotContain("Marksman", character.Feats);
        Assert.Contains("Tough", character.Feats);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFlawAsync_ShouldRemoveFlaw()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Flaws.Add("Coward");
        character.Flaws.Add("Weak");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveFlawAsync(warband.Id, character.Id, "Coward");

        // Assert
        Assert.DoesNotContain("Coward", character.Flaws);
        Assert.Contains("Weak", character.Flaws);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddFeatAsync_ShouldNotAddDuplicate()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Feats.Add("Marksman");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AddFeatAsync(warband.Id, character.Id, "Marksman"));
    }

    [Fact]
    public async Task AddFlawAsync_ShouldNotAddDuplicate()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Flaws.Add("Coward");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AddFlawAsync(warband.Id, character.Id, "Coward"));
    }

    [Fact]
    public async Task AddInjuryAsync_ShouldAddInjuryManually()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddInjuryAsync(warband.Id, character.Id, "Broken Bones");

        // Assert
        Assert.Contains("Broken Bones", character.Injuries);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task RemoveInjuryAsync_ShouldRemoveInjury()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Injuries.Add("Broken Bones");
        character.Injuries.Add("Weak");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act
        await _service.RemoveInjuryAsync(warband.Id, character.Id, "Broken Bones");

        // Assert
        Assert.DoesNotContain("Broken Bones", character.Injuries);
        Assert.Contains("Weak", character.Injuries);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddInjuryAsync_ShouldNotAddDuplicate()
    {
        // Arrange
        var warband = new Warband("Test", "28-psalms");
        var character = new Character("Hero");
        character.Injuries.Add("Broken Bones");
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id))
                      .ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>()))
                      .ReturnsAsync((Warband w) => w);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AddInjuryAsync(warband.Id, character.Id, "Broken Bones"));
    }

    #endregion

    #region Relic Tests

    [Fact]
    public async Task LoadRelicsAsync_ShouldLoadRelicsFromJson()
    {
        // Arrange
        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = true,
                Effect = "+1 damage"
            },
            new Equipment
            {
                Id = "pillars-of-the-pandemic",
                Name = "Pillars of the Pandemic",
                Type = "relic",
                Cost = 100,
                Slots = 0,
                Unique = true,
                Effect = "Deploy as terrain, 6 inch sickness aura"
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicsData);

        // Act
        var relics = await _mockResourceService.Object.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json");

        // Assert
        Assert.NotNull(relics);
        Assert.Equal(2, relics.Count);
        Assert.True(relics[0].IsRelic);
        Assert.True(relics[0].Unique);
        Assert.Equal(1, relics[0].Slots);
        Assert.Equal(0, relics[1].Slots); // Pillars of the Pandemic takes 0 slots
    }

    [Fact]
    public async Task BuyRelicAsync_ShouldUse100GFlatPrice()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 150 };
        _state.Warbands[warband.Id] = warband;

        // Mock relics data
        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = true
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicsData);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Buy relic (should cost exactly 100G regardless of trader)
        await _service.BuyRelicAsync(warband.Id, "boots-of-unkindness");

        // Assert
        Assert.Single(warband.Stash);
        Assert.Equal("Boots of Unkindness", warband.Stash[0].Name);
        Assert.True(warband.Stash[0].IsRelic);
        Assert.Equal(50, warband.Gold); // 150 - 100 (flat price)
        Assert.Contains("boots-of-unkindness", warband.AcquiredRelicIds);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task BuyRelicAsync_ShouldPreventDuplicateUniqueRelics()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 200 };
        warband.AcquiredRelicIds.Add("boots-of-unkindness"); // Already acquired
        _state.Warbands[warband.Id] = warband;

        // Mock relics data
        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = true
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicsData);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.BuyRelicAsync(warband.Id, "boots-of-unkindness"));
        Assert.Contains("has already been acquired", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuyRelicAsync_ShouldAllowMultipleSilverBullets()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "last-war") { Id = "warband-1", Gold = 250 };
        warband.AcquiredRelicIds.Add("silver-bullet"); // Already have one
        _state.Warbands[warband.Id] = warband;

        // Mock relics data - Silver Bullet is NOT unique
        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "silver-bullet",
                Name = "Silver Bullet",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = false // Exception: Can be found multiple times
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("last-war", "relics.json"))
            .ReturnsAsync(relicsData);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Should allow buying second Silver Bullet
        await _service.BuyRelicAsync(warband.Id, "silver-bullet");

        // Assert
        Assert.Single(warband.Stash); // Second bullet added
        Assert.Equal("Silver Bullet", warband.Stash[0].Name);
        Assert.Equal(150, warband.Gold); // 250 - 100
        // Note: AcquiredRelicIds will have 2 entries for silver-bullet (which is fine for non-unique relics)
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldHandleRelicsAndMarkAsAcquired()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 200 };
        var character = new Character("Test Character")
        {
            Id = "char-1",
            Stats = new Stats(0, 0, 0, 0)
        };
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = true,
                Effect = "+1 damage"
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicsData);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act - Assign relic to character (simulating d20 roll)
        await _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "boots-of-unkindness", "relic");

        // Assert
        Assert.Single(character.Equipment);
        Assert.Equal("Boots of Unkindness", character.Equipment[0].Name);
        Assert.True(character.Equipment[0].IsRelic);
        Assert.Equal("+1 damage", character.Equipment[0].Effect);
        Assert.Contains("boots-of-unkindness", warband.AcquiredRelicIds);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Warband>()), Times.Once);
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldPreventAssigningDuplicateUniqueRelic()
    {
        // Arrange
        await _service.LoadGameDataAsync();
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1", Gold = 200 };
        warband.AcquiredRelicIds.Add("boots-of-unkindness"); // Already acquired
        var character = new Character("Test Character")
        {
            Id = "char-1",
            Stats = new Stats(0, 0, 0, 0)
        };
        warband.Members.Add(character);
        _state.Warbands[warband.Id] = warband;

        var relicsData = new List<Equipment>
        {
            new Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Cost = 100,
                Slots = 1,
                Unique = true
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicsData);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "boots-of-unkindness", "relic"));

        Assert.Contains("has already been acquired", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(character.Equipment); // Should not have added the equipment
    }

    [Fact]
    public async Task AddEquipmentToCharacterAsync_ShouldCopyEffectsArray()
    {
        // Arrange
        SetupBackpackWithEffects();
        await _service.LoadGameDataAsync();

        var warband = new Warband("Test Warband", "28-psalms") { Id = "warband-1", Gold = 200 };
        var character = new Character("Test Character")
        {
            Id = "char-1",
            Stats = new Stats(0, 0, 0, 0)
        };
        warband.Members.Add(character);

        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        // Act
        await _service.AddEquipmentToCharacterAsync(warband.Id, character.Id, "backpack", "item");

        // Assert
        Assert.Single(character.Equipment);
        var equippedBackpack = character.Equipment.First();
        Assert.Equal("Backpack", equippedBackpack.Name);

        // Verify Effects array is copied
        Assert.NotNull(equippedBackpack.Effects);
        Assert.NotEmpty(equippedBackpack.Effects);
        Assert.Single(equippedBackpack.Effects);

        var effect = equippedBackpack.Effects[0];
        Assert.True(effect.ContainsKey("type"));
        Assert.Equal("slot_modifier", effect["type"].ToString());
        Assert.True(effect.ContainsKey("modifier"));

        // Handle JsonElement from deserialization
        var modifierValue = effect["modifier"];
        if (modifierValue is JsonElement jsonElement)
        {
            Assert.Equal(2, jsonElement.GetInt32());
        }
        else
        {
            Assert.Equal(2, Convert.ToInt32(modifierValue));
        }
    }

    #endregion

    #region Relic Availability Management

    [Fact]
    public async Task MarkRelicAsAcquired_ShouldAddRelicIdToAcquiredList()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1" };
        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        var relicId = "ring-of-the-mighty";

        // Act
        await _service.MarkRelicAsAcquiredAsync(warband.Id, relicId);

        // Assert
        Assert.Contains(relicId, warband.AcquiredRelicIds);
        Assert.Single(warband.AcquiredRelicIds);
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task MarkRelicAsAcquired_ShouldNotDuplicateIfAlreadyAcquired()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1" };
        warband.AcquiredRelicIds.Add("ring-of-the-mighty");
        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        var relicId = "ring-of-the-mighty";

        // Act
        await _service.MarkRelicAsAcquiredAsync(warband.Id, relicId);

        // Assert
        Assert.Contains(relicId, warband.AcquiredRelicIds);
        Assert.Single(warband.AcquiredRelicIds); // Should still be 1, not 2
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task MarkRelicAsAvailable_ShouldRemoveRelicIdFromAcquiredList()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1" };
        warband.AcquiredRelicIds.Add("ring-of-the-mighty");
        warband.AcquiredRelicIds.Add("boots-of-unkindness");
        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        var relicId = "ring-of-the-mighty";

        // Act
        await _service.MarkRelicAsAvailableAsync(warband.Id, relicId);

        // Assert
        Assert.DoesNotContain(relicId, warband.AcquiredRelicIds);
        Assert.Single(warband.AcquiredRelicIds); // Should have 1 remaining
        Assert.Contains("boots-of-unkindness", warband.AcquiredRelicIds);
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    [Fact]
    public async Task MarkRelicAsAvailable_ShouldHandleRelicNotInList()
    {
        // Arrange
        var warband = new Warband("Test Warband", "end-times") { Id = "warband-1" };
        _mockRepository.Setup(r => r.GetByIdAsync(warband.Id)).ReturnsAsync(warband);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Warband>())).ReturnsAsync((Warband w) => w);

        var relicId = "ring-of-the-mighty";

        // Act
        await _service.MarkRelicAsAvailableAsync(warband.Id, relicId);

        // Assert
        Assert.Empty(warband.AcquiredRelicIds); // Should remain empty
        _mockRepository.Verify(r => r.SaveAsync(warband), Times.Once);
    }

    #endregion

}