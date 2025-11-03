using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Data.Services;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class InjuryServiceTests
{
    private readonly InjuryService _injuryService;

    public InjuryServiceTests()
    {
        _injuryService = new InjuryService(new EmbeddedResourceService());
    }

    [Fact]
    public async Task GetInjuriesAsync_28Psalms_ReturnsInjuries()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert
        Assert.NotNull(injuries);
        Assert.NotEmpty(injuries);
        Assert.Equal(20, injuries.Count);
    }

    [Fact]
    public async Task GetInjuriesAsync_EndTimes_ReturnsInjuries()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("end-times");

        // Assert
        Assert.NotNull(injuries);
        Assert.NotEmpty(injuries);
        Assert.Equal(20, injuries.Count);
    }

    [Fact]
    public async Task GetInjuriesAsync_LastWar_ReturnsInjuries()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("last-war");

        // Assert
        Assert.NotNull(injuries);
        Assert.NotEmpty(injuries);
        Assert.Equal(20, injuries.Count);
    }

    [Fact]
    public async Task GetInjuriesAsync_CachesResults()
    {
        // Act
        var injuries1 = await _injuryService.GetInjuriesAsync("28-psalms");
        var injuries2 = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert - Should return same instance from cache
        Assert.Same(injuries1, injuries2);
    }

    [Fact]
    public async Task GetInjuriesAsync_AllInjuriesHaveRequiredProperties()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert
        foreach (var injury in injuries)
        {
            Assert.NotEmpty(injury.Name);
            Assert.NotEmpty(injury.Effect);
            Assert.NotEmpty(injury.Type);
            Assert.NotEmpty(injury.Category);
            Assert.True(injury.Roll.HasValue);
            Assert.InRange(injury.Roll.Value, 1, 20);
        }
    }

    [Fact]
    public async Task GetInjuriesAsync_MostInjuriesHaveEffects()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert - Most injuries should have effects populated
        var injuriesWithEffects = injuries.Where(i => i.Effects != null && i.Effects.Any()).ToList();

        // At least 14 out of 20 injuries should have structured effects (after removing gameplay-only effects)
        Assert.True(injuriesWithEffects.Count >= 14,
            $"Expected at least 14 injuries with effects, but got {injuriesWithEffects.Count}");
    }

    [Fact]
    public async Task GetInjuriesAsync_EffectsAreProperlyParsed()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Find a stat modifier injury
        var brokenBones = injuries.FirstOrDefault(i => i.Name == "Broken Bones");

        // Assert
        Assert.NotNull(brokenBones);
        Assert.NotNull(brokenBones.Effects);
        Assert.Single(brokenBones.Effects);

        var effect = brokenBones.Effects[0];
        Assert.Equal("stat_modifier", effect["type"]);
        Assert.Equal("agility", effect["stat"]);

        // modifier might be int or double, check both
        Assert.True(effect.ContainsKey("modifier"));
        var modifier = effect["modifier"];
        Assert.True(modifier is int || modifier is double);
        Assert.Equal(-1, Convert.ToInt32(modifier));
    }

    [Fact]
    public async Task RollInjuryAsync_ReturnsValidInjury()
    {
        // Act
        var injury = await _injuryService.RollInjuryAsync("28-psalms");

        // Assert
        Assert.NotNull(injury);
        Assert.NotEmpty(injury.Name);
        Assert.True(injury.Roll.HasValue);
        Assert.InRange(injury.Roll.Value, 1, 20);
    }

    [Fact]
    public async Task RollInjuryAsync_AllVariants_ReturnsValidInjury()
    {
        // Test all variants
        var variants = new[] { "28-psalms", "end-times", "last-war" };

        foreach (var variant in variants)
        {
            // Act
            var injury = await _injuryService.RollInjuryAsync(variant);

            // Assert
            Assert.NotNull(injury);
            Assert.NotEmpty(injury.Name);
        }
    }

    [Fact]
    public async Task RollInjuryAsync_MultipleRolls_ReturnsDifferentResults()
    {
        // Act - Roll multiple times
        var rolls = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            var injury = await _injuryService.RollInjuryAsync("28-psalms");
            rolls.Add(injury.Name);
        }

        // Assert - Should have multiple different injuries
        Assert.True(rolls.Count > 5, "Expected varied results from multiple rolls");
    }

    [Fact]
    public async Task GetInjuriesAsync_PositiveInjuriesExist()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert - Should have positive injuries
        var positiveInjuries = injuries.Where(i => i.Type == "positive").ToList();
        Assert.NotEmpty(positiveInjuries);
        Assert.Contains(positiveInjuries, i => i.Name == "Learn through Failure");
        Assert.Contains(positiveInjuries, i => i.Name == "What Doesn't Kill You");
    }

    [Fact]
    public async Task GetInjuriesAsync_TemporaryInjuriesHaveDuration()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Assert
        var temporaryInjuries = injuries.Where(i => i.Type == "temporary").ToList();
        foreach (var injury in temporaryInjuries)
        {
            Assert.NotNull(injury.Duration);
            Assert.NotEmpty(injury.Duration);
        }
    }

    // Behavioral effects removed as they are gameplay-only, not character-building
    // [Fact]
    // public async Task GetInjuriesAsync_BehavioralEffects_AreProperlyParsed()
    // {
    //     // Behavioral effects have been removed from injuries.json
    //     // as they only affect gameplay and don't modify character stats
    // }

    [Fact]
    public async Task GetInjuriesAsync_EquipmentRestrictionEffects_AreProperlyParsed()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Find equipment restriction injury
        var lostLimb = injuries.FirstOrDefault(i => i.Name == "Lost Limb");

        // Assert
        Assert.NotNull(lostLimb);
        Assert.NotNull(lostLimb.Effects);
        Assert.Single(lostLimb.Effects);

        var effect = lostLimb.Effects[0];
        Assert.Equal("equipment_restriction", effect["type"]);
        Assert.True(effect.ContainsKey("weaponTypes"));
        Assert.True(effect.ContainsKey("slotModifier"));
        Assert.Equal(-1, Convert.ToInt32(effect["slotModifier"]));
    }

    [Fact]
    public async Task GetInjuriesAsync_EffectsWithArrays_AreProperlyParsed()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("end-times");

        // Find injury with array in effects
        var lostLimb = injuries.FirstOrDefault(i => i.Name == "Lost Limb");

        // Assert
        Assert.NotNull(lostLimb);
        Assert.NotNull(lostLimb.Effects);
        Assert.NotEmpty(lostLimb.Effects);

        var effect = lostLimb.Effects[0];
        Assert.True(effect.ContainsKey("weaponTypes"));

        // The weaponTypes should be parsed as a List<object>
        var weaponTypes = effect["weaponTypes"];
        Assert.NotNull(weaponTypes);
    }

    [Fact]
    public async Task GetInjuriesAsync_AllEffectTypes_PreserveStructure()
    {
        // Act - Test all variants to ensure all effect types are preserved
        var variants = new[] { "28-psalms", "end-times", "last-war" };

        foreach (var variant in variants)
        {
            var injuries = await _injuryService.GetInjuriesAsync(variant);
            var injuriesWithEffects = injuries.Where(i => i.Effects != null && i.Effects.Any()).ToList();

            // Assert - Each injury with effects should have properly structured effects
            foreach (var injury in injuriesWithEffects)
            {
                Assert.NotNull(injury.Effects);
                Assert.NotEmpty(injury.Effects);

                foreach (var effect in injury.Effects)
                {
                    // Each effect must have a type
                    Assert.True(effect.ContainsKey("type"),
                        $"Injury '{injury.Name}' in variant '{variant}' has effect without 'type' property");
                    Assert.NotNull(effect["type"]);
                    Assert.NotEmpty(effect["type"]?.ToString() ?? "");
                }
            }
        }
    }

    [Fact]
    public async Task GetInjuriesAsync_ComplexEffects_AllPropertiesPreserved()
    {
        // Act
        var injuries = await _injuryService.GetInjuriesAsync("28-psalms");

        // Get injuries with complex effects
        var injuriesWithEffects = injuries.Where(i => i.Effects != null && i.Effects.Any()).ToList();

        // Assert - Verify effects preserve all their properties
        foreach (var injury in injuriesWithEffects)
        {
            foreach (var effect in injury.Effects)
            {
                // Verify that dictionary values are not null
                foreach (var kvp in effect)
                {
                    Assert.NotNull(kvp.Value);
                }
            }
        }
    }
}
