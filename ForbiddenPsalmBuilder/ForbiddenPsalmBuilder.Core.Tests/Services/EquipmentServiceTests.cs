using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class EquipmentServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly EquipmentService _service;

    public EquipmentServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new EquipmentService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetWeaponsAsync_ShouldReturnEmptyList_WhenGameVariantIsEmpty()
    {
        // Act
        var result = await _service.GetWeaponsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWeaponsAsync_ShouldLoadAndCacheWeapons()
    {
        // Arrange
        var weaponData = new WeaponsData
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
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);

        // Act
        var weapons = await _service.GetWeaponsAsync("28-psalms");

        // Assert
        Assert.NotEmpty(weapons);
        Assert.Contains(weapons, w => w.Name == "Dagger");

        // Verify caching - should not call resource service again
        await _service.GetWeaponsAsync("28-psalms");
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetWeaponByIdAsync_ShouldReturnWeapon_WhenExists()
    {
        // Arrange
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
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
            .ReturnsAsync(weaponData);

        // Act
        var weapon = await _service.GetWeaponByIdAsync("sword", "28-psalms");

        // Assert
        Assert.NotNull(weapon);
        Assert.Equal("Sword", weapon.Name);
    }

    [Fact]
    public async Task GetArmorAsync_ShouldReturnEmptyList_WhenGameVariantIsEmpty()
    {
        // Act
        var result = await _service.GetArmorAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetArmorAsync_ShouldLoadAndCacheArmor()
    {
        // Arrange
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

        // Act
        var armor = await _service.GetArmorAsync("28-psalms");

        // Assert
        Assert.NotEmpty(armor);
        Assert.Contains(armor, a => a.Name == "Light");

        // Verify caching
        await _service.GetArmorAsync("28-psalms");
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<List<object>>("28-psalms", "armor.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetItemsAsync_ShouldReturnEmptyList_WhenGameVariantIsEmpty()
    {
        // Act
        var result = await _service.GetItemsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsAsync_ShouldLoadAndCacheItems()
    {
        // Arrange
        var itemData = new Dictionary<string, List<object>>
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
            },
            ["ammo"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["type"] = "Arrows",
                    ["shots"] = 5,
                    ["cost"] = 2,
                    ["slots"] = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(itemData);

        // Act
        var items = await _service.GetItemsAsync("28-psalms");

        // Assert
        Assert.NotEmpty(items);
        Assert.Contains(items, i => i.Name == "Cheese");
        Assert.Contains(items, i => i.Name == "Arrows" && i.Type == "ammo");

        // Verify caching
        await _service.GetItemsAsync("28-psalms");
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetWeaponsAsync_ShouldHandleErrors_AndReturnEmptyList()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("invalid", "weapons.json"))
            .ThrowsAsync(new Exception("File not found"));

        // Act
        var weapons = await _service.GetWeaponsAsync("invalid");

        // Assert
        Assert.Empty(weapons);
    }

    [Fact]
    public async Task GetWeaponsAsync_ShouldAssignCategoryToWeapons()
    {
        // Arrange
        var weaponData = new WeaponsData
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
                    Properties = new List<string>()
                }
            },
            TwoHandedRanged = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Bow",
                    Damage = "D6",
                    Stat = "Presence",
                    Cost = 5,
                    Slots = 2,
                    Properties = new List<string> { "Ranged" }
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);

        // Act
        var weapons = await _service.GetWeaponsAsync("28-psalms");

        // Assert
        var dagger = weapons.First(w => w.Name == "Dagger");
        var bow = weapons.First(w => w.Name == "Bow");
        Assert.Equal("oneHandedMelee", dagger.Category);
        Assert.Equal("twoHandedRanged", bow.Category);
    }

    [Fact]
    public async Task GetItemsAsync_EndTimes_ShouldLoadAmmoWithTypeField()
    {
        // Arrange - end-times equipment structure with separate ammo array
        var equipmentData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Bandages",
                    ["effect"] = "Cures Bleeding",
                    ["cost"] = 1,
                    ["slots"] = 1,
                    ["roll"] = 1
                }
            },
            ["ammo"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["type"] = "Ammo",
                    ["effect"] = "Five shots per stack of Ammo",
                    ["shots"] = 5,
                    ["cost"] = 1,
                    ["slots"] = 1
                },
                new Dictionary<string, object>
                {
                    ["type"] = "Cannonball",
                    ["effect"] = "One shot of Cannon Ammo",
                    ["shots"] = 1,
                    ["cost"] = 2,
                    ["slots"] = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("end-times", "equipment.json"))
            .ReturnsAsync(equipmentData);

        // Act
        var items = await _service.GetItemsAsync("end-times");

        // Assert
        Assert.NotEmpty(items);

        // Verify regular items
        Assert.Contains(items, i => i.Name == "Bandages" && i.Type == "item");

        // Verify ammo items use "type" field and have Type = "ammo"
        var ammo = items.FirstOrDefault(i => i.Name == "Ammo");
        Assert.NotNull(ammo);
        Assert.Equal("ammo", ammo.Type);

        var cannonball = items.FirstOrDefault(i => i.Name == "Cannonball");
        Assert.NotNull(cannonball);
        Assert.Equal("ammo", cannonball.Type);
    }

    [Fact]
    public async Task GetWeaponsAsync_ShouldLoadAmmoType_ForRangedWeapons()
    {
        // Arrange - end-times ranged weapons with ammoType
        var weaponData = new WeaponsData
        {
            OneHandedRanged = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Flintlock Pistol",
                    Damage = "D8",
                    Stat = "Presence",
                    Cost = 15,
                    Slots = 1,
                    Properties = new List<string> { "Ranged", "Explode", "Reload" },
                    AmmoType = "Ammo"
                }
            },
            TwoHandedRanged = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Cannon",
                    Damage = "D20",
                    Stat = "Strength",
                    Cost = 100,
                    Slots = 2,
                    Properties = new List<string> { "Ranged", "Explode", "Reload" },
                    AmmoType = "Cannonball"
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("end-times", "weapons.json"))
            .ReturnsAsync(weaponData);

        // Act
        var weapons = await _service.GetWeaponsAsync("end-times");

        // Assert
        var pistol = weapons.FirstOrDefault(w => w.Name == "Flintlock Pistol");
        Assert.NotNull(pistol);
        Assert.Equal("Ammo", pistol.AmmoType);
        Assert.True(pistol.RequiresAmmo);

        var cannon = weapons.FirstOrDefault(w => w.Name == "Cannon");
        Assert.NotNull(cannon);
        Assert.Equal("Cannonball", cannon.AmmoType);
        Assert.True(cannon.RequiresAmmo);
    }

    [Fact]
    public async Task GetCompatibleAmmo_ShouldReturnOnlyMatchingAmmoType()
    {
        // Arrange - setup weapon data
        var weaponData = new WeaponsData
        {
            OneHandedRanged = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Flintlock Pistol",
                    Damage = "D8",
                    Stat = "Presence",
                    Cost = 15,
                    Slots = 1,
                    Properties = new List<string> { "Ranged", "Explode", "Reload" },
                    AmmoType = "Ammo"
                }
            }
        };

        // Setup ammo data
        var equipmentData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>(),
            ["ammo"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["type"] = "Ammo",
                    ["effect"] = "Five shots per stack of Ammo",
                    ["shots"] = 5,
                    ["cost"] = 1,
                    ["slots"] = 1
                },
                new Dictionary<string, object>
                {
                    ["type"] = "Cannonball",
                    ["effect"] = "One shot of Cannon Ammo",
                    ["shots"] = 1,
                    ["cost"] = 2,
                    ["slots"] = 1
                },
                new Dictionary<string, object>
                {
                    ["type"] = "Arrows",
                    ["effect"] = "Five arrows",
                    ["shots"] = 5,
                    ["cost"] = 2,
                    ["slots"] = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("end-times", "weapons.json"))
            .ReturnsAsync(weaponData);

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("end-times", "equipment.json"))
            .ReturnsAsync(equipmentData);

        // Get the weapon
        var weapons = await _service.GetWeaponsAsync("end-times");
        var pistol = weapons.First(w => w.Name == "Flintlock Pistol");

        // Act
        var compatibleAmmo = await _service.GetCompatibleAmmo(pistol, "end-times");

        // Assert
        Assert.NotEmpty(compatibleAmmo);
        Assert.Single(compatibleAmmo);
        Assert.Equal("Ammo", compatibleAmmo.First().Name);
    }

    [Fact]
    public async Task GetCompatibleAmmo_ShouldReturnEmpty_WhenWeaponDoesNotRequireAmmo()
    {
        // Arrange - setup weapon data with no ammoType
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
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
            .ReturnsAsync(weaponData);

        // Get the weapon
        var weapons = await _service.GetWeaponsAsync("28-psalms");
        var sword = weapons.First(w => w.Name == "Sword");

        // Act
        var compatibleAmmo = await _service.GetCompatibleAmmo(sword, "28-psalms");

        // Assert
        Assert.Empty(compatibleAmmo);
    }

    [Fact]
    public async Task GetCompatibleAmmo_ShouldReturnEmpty_WhenNoMatchingAmmoExists()
    {
        // Arrange - weapon needs "Plasma Cell" but only "Ammo" is available
        var weaponData = new WeaponsData
        {
            TwoHandedRanged = new List<WeaponDto>
            {
                new WeaponDto
                {
                    Name = "Plas-mar",
                    Damage = "D10",
                    Stat = "Presence",
                    Cost = 25,
                    Slots = 2,
                    Properties = new List<string> { "Reload 2", "Explode", "Ranged", "AP" },
                    AmmoType = "Plasma Cell"
                }
            }
        };

        var equipmentData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>(),
            ["ammo"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["type"] = "Ammo",
                    ["effect"] = "Five shots per stack of Ammo",
                    ["shots"] = 5,
                    ["cost"] = 1,
                    ["slots"] = 1
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(equipmentData);

        // Get the weapon
        var weapons = await _service.GetWeaponsAsync("28-psalms");
        var plasmar = weapons.First(w => w.Name == "Plas-mar");

        // Act
        var compatibleAmmo = await _service.GetCompatibleAmmo(plasmar, "28-psalms");

        // Assert
        Assert.Empty(compatibleAmmo);
    }

    [Fact]
    public async Task GetRelicsAsync_ShouldReturnEmptyList_WhenGameVariantIsEmpty()
    {
        // Act
        var result = await _service.GetRelicsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRelicsAsync_ShouldLoadAndCacheRelics()
    {
        // Arrange
        var relicData = new List<Core.Models.Character.Equipment>
        {
            new Core.Models.Character.Equipment
            {
                Id = "boots-of-unkindness",
                Name = "Boots of Unkindness",
                Type = "relic",
                Effect = "+1 damage.",
                Cost = 100,
                Slots = 1,
                Category = "chapter1",
                Unique = true
            },
            new Core.Models.Character.Equipment
            {
                Id = "scroll-of-forbidden-knowledge",
                Name = "Scroll of Forbidden Knowledge",
                Type = "relic",
                Effect = "+1 Presence, consumed on use.",
                Cost = 100,
                Slots = 1,
                Category = "chapter1",
                Unique = true
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Core.Models.Character.Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicData);

        // Act
        var relics = await _service.GetRelicsAsync("end-times");

        // Assert
        Assert.NotEmpty(relics);
        Assert.Equal(2, relics.Count);
        Assert.Contains(relics, r => r.Name == "Boots of Unkindness");
        Assert.True(relics[0].IsRelic);
        Assert.True(relics[0].Unique);

        // Verify caching - should not call resource service again
        await _service.GetRelicsAsync("end-times");
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<List<Core.Models.Character.Equipment>>("end-times", "relics.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetRelicsAsync_ShouldHandleErrors_AndReturnEmptyList()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Core.Models.Character.Equipment>>("invalid", "relics.json"))
            .ThrowsAsync(new Exception("File not found"));

        // Act
        var relics = await _service.GetRelicsAsync("invalid");

        // Assert
        Assert.Empty(relics);
    }

    [Fact]
    public async Task GetRelicsAsync_ShouldLoadRelicsWithVariousProperties()
    {
        // Arrange - relics can have different properties like damage, range, etc.
        var relicData = new List<Core.Models.Character.Equipment>
        {
            new Core.Models.Character.Equipment
            {
                Id = "vampiric-tongue-shortsword",
                Name = "Vampiric Tongue Shortsword",
                Type = "relic",
                Effect = "D6 damage. Wielder heals D6 damage when it wounds.",
                Damage = "D6",
                Cost = 100,
                Slots = 1,
                Category = "chapter1",
                Unique = true
            },
            new Core.Models.Character.Equipment
            {
                Id = "pillars-of-the-pandemic",
                Name = "Pillars of the Pandemic",
                Type = "relic",
                Effect = "Takes up no Equipment slot and is instead deployed by the controlling player.",
                Cost = 100,
                Slots = 0, // This relic takes 0 slots
                Category = "chapter1",
                Unique = true
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Core.Models.Character.Equipment>>("end-times", "relics.json"))
            .ReturnsAsync(relicData);

        // Act
        var relics = await _service.GetRelicsAsync("end-times");

        // Assert
        Assert.NotEmpty(relics);
        var vampiricSword = relics.First(r => r.Id == "vampiric-tongue-shortsword");
        Assert.Equal("D6", vampiricSword.Damage);

        var pillars = relics.First(r => r.Id == "pillars-of-the-pandemic");
        Assert.Equal(0, pillars.Slots); // Verify 0-slot relic
    }

    #region Unified Equipment Loading Tests (TDD - Phase 1)

    [Fact]
    public async Task GetEquipmentAsync_ShouldLoadWeaponsWithTypeField()
    {
        // Arrange - Weapons loaded as WeaponsData then converted
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
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
            .ReturnsAsync(weaponData);

        // Act
        var equipment = await _service.GetEquipmentAsync("28-psalms", "weapon");

        // Assert
        Assert.NotEmpty(equipment);
        Assert.Single(equipment);
        Assert.Equal("weapon", equipment[0].Type);
        Assert.True(equipment[0].IsWeapon);
        Assert.Equal("Sword", equipment[0].Name);
    }

    [Fact]
    public async Task GetEquipmentAsync_ShouldLoadArmorWithTypeField()
    {
        // Arrange - Armor loaded as List<object> then converted
        var armorData = new List<object>
        {
            new Dictionary<string, object>
            {
                ["name"] = "Light Armor",
                ["armorValue"] = 1,
                ["cost"] = 2,
                ["slots"] = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<object>>("28-psalms", "armor.json"))
            .ReturnsAsync(armorData);

        // Act
        var equipment = await _service.GetEquipmentAsync("28-psalms", "armor");

        // Assert
        Assert.NotEmpty(equipment);
        Assert.Single(equipment);
        Assert.Equal("armor", equipment[0].Type);
        Assert.True(equipment[0].IsArmor);
        Assert.Equal(1, equipment[0].ArmorValue);
    }

    [Fact]
    public async Task GetEquipmentAsync_ShouldLoadItemsWithTypeField()
    {
        // Arrange - Items loaded as Dictionary then converted
        var itemData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Bandages",
                    ["effect"] = "Heals 1 HP",
                    ["cost"] = 1,
                    ["slots"] = 1
                }
            },
            ["ammo"] = new List<object>()
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(itemData);

        // Act
        var equipment = await _service.GetEquipmentAsync("28-psalms", "item");

        // Assert
        Assert.NotEmpty(equipment);
        Assert.Single(equipment);
        Assert.Equal("item", equipment[0].Type);
        Assert.True(equipment[0].IsItem);
    }

    [Fact]
    public async Task GetEquipmentAsync_ShouldLoadAllTypesWhenNoTypeSpecified()
    {
        // Arrange - All equipment types with correct mock data structures
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
                new WeaponDto { Name = "Sword", Damage = "D6", Stat = "Strength", Cost = 3, Slots = 1, Properties = new List<string>() }
            }
        };

        var armorData = new List<object>
        {
            new Dictionary<string, object> { ["name"] = "Light Armor", ["armorValue"] = 1, ["cost"] = 2, ["slots"] = 1 }
        };

        var itemData = new Dictionary<string, List<object>>
        {
            ["items"] = new List<object>
            {
                new Dictionary<string, object> { ["name"] = "Bandages", ["effect"] = "Heals 1 HP", ["cost"] = 1, ["slots"] = 1 }
            },
            ["ammo"] = new List<object>()
        };

        var relics = new List<Core.Models.Character.Equipment>
        {
            new Core.Models.Character.Equipment { Id = "boots", Name = "Boots of Unkindness", Type = "relic", Cost = 100 }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<object>>("28-psalms", "armor.json"))
            .ReturnsAsync(armorData);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<Dictionary<string, List<object>>>("28-psalms", "equipment.json"))
            .ReturnsAsync(itemData);
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Core.Models.Character.Equipment>>("28-psalms", "relics.json"))
            .ReturnsAsync(relics);

        // Act - No type specified, should load all
        var equipment = await _service.GetEquipmentAsync("28-psalms");

        // Assert
        Assert.Equal(4, equipment.Count);
        Assert.Contains(equipment, e => e.Type == "weapon");
        Assert.Contains(equipment, e => e.Type == "armor");
        Assert.Contains(equipment, e => e.Type == "item");
        Assert.Contains(equipment, e => e.Type == "relic");
    }

    [Fact]
    public async Task GetEquipmentAsync_ShouldCacheResults()
    {
        // Arrange
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
                new WeaponDto { Name = "Sword", Damage = "D6", Stat = "Strength", Cost = 3, Slots = 1, Properties = new List<string>() }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);

        // Act - Call twice
        await _service.GetEquipmentAsync("28-psalms", "weapon");
        await _service.GetEquipmentAsync("28-psalms", "weapon");

        // Assert - Should only call resource service once (cached)
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetEquipmentByIdAsync_ShouldFindWeaponById()
    {
        // Arrange
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
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
            .ReturnsAsync(weaponData);

        // Act
        var equipment = await _service.GetEquipmentByIdAsync("sword", "28-psalms", "weapon");

        // Assert
        Assert.NotNull(equipment);
        Assert.Equal("sword", equipment.Id);
        Assert.Equal("weapon", equipment.Type);
    }

    [Fact]
    public async Task GetEquipmentByIdAsync_ShouldReturnNullWhenNotFound()
    {
        // Arrange
        var weaponData = new WeaponsData
        {
            OneHandedMelee = new List<WeaponDto>
            {
                new WeaponDto { Name = "Sword", Damage = "D6", Stat = "Strength", Cost = 3, Slots = 1, Properties = new List<string>() }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<WeaponsData>("28-psalms", "weapons.json"))
            .ReturnsAsync(weaponData);

        // Act
        var equipment = await _service.GetEquipmentByIdAsync("nonexistent", "28-psalms", "weapon");

        // Assert
        Assert.Null(equipment);
    }

    [Fact]
    public async Task GetEquipmentAsync_ShouldReturnEmptyListWhenGameVariantIsEmpty()
    {
        // Act
        var equipment = await _service.GetEquipmentAsync("", "weapon");

        // Assert
        Assert.Empty(equipment);
    }

    #endregion
}