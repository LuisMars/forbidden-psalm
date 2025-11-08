using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class GameStateServicePetTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly Mock<IWarbandRepository> _mockWarbandRepository;
    private readonly PetService _petService;
    private readonly IGameStateService _gameStateService;
    private readonly GlobalGameState _globalGameState;

    public GameStateServicePetTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _mockWarbandRepository = new Mock<IWarbandRepository>();
        _petService = new PetService(_mockResourceService.Object);
        _globalGameState = new GlobalGameState();

        _gameStateService = new GameStateService(
            _globalGameState,
            _mockWarbandRepository.Object,
            _mockResourceService.Object,
            null
        );
    }

    #region Pet Management Tests

    [Fact]
    public async Task AddPetToWarbandAsync_ShouldAddPet_WhenValidPetAndSufficientGold()
    {
        // Arrange
        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Gold = 100,
            Members = new List<Character>
            {
                new Character
                {
                    Id = "char-1",
                    Name = "Hero"
                }
            }
        };

        var pet = new Equipment
        {
            Id = "goodest-doggo",
            Name = "The Goodest Doggo",
            Type = "pet",
            Cost = 15,
            Slots = 1
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);
        _mockWarbandRepository
            .Setup(x => x.SaveAsync(It.IsAny<Warband>()))
            .ReturnsAsync((Warband w) => w);

        // Act
        await _gameStateService.AddPetToWarbandAsync("test-warband", "goodest-doggo", pet);

        // Assert
        _mockWarbandRepository.Verify(x => x.SaveAsync(It.IsAny<Warband>()), Times.Once);
        Assert.NotNull(warband.Pet);
        Assert.Equal("The Goodest Doggo", warband.Pet.Name);
        Assert.Equal(85, warband.Gold); // 100 - 15
    }

    [Fact]
    public async Task AddPetToWarbandAsync_ShouldThrow_WhenInsufficientGold()
    {
        // Arrange
        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Gold = 5,
            Members = new List<Character>()
        };

        var pet = new Equipment
        {
            Id = "fire-hawk",
            Name = "Fire Hawk",
            Type = "pet",
            Cost = 20,
            Slots = 1
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gameStateService.AddPetToWarbandAsync("test-warband", "fire-hawk", pet)
        );
    }

    [Fact]
    public async Task RemovePetFromWarbandAsync_ShouldRemovePet_AndRefundHalfCost()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "goodest-doggo",
            Name = "The Goodest Doggo",
            Type = "pet",
            Cost = 30,
            Slots = 1
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Gold = 50,
            Pet = pet,
            Members = new List<Character>()
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);
        _mockWarbandRepository
            .Setup(x => x.SaveAsync(It.IsAny<Warband>()))
            .ReturnsAsync((Warband w) => w);

        // Act
        await _gameStateService.RemovePetFromWarbandAsync("test-warband");

        // Assert
        _mockWarbandRepository.Verify(x => x.SaveAsync(It.IsAny<Warband>()), Times.Once);
        Assert.Null(warband.Pet);
        Assert.Equal(65, warband.Gold); // 50 + (30/2)
    }

    [Fact]
    public async Task RemovePetFromWarbandAsync_ShouldDoNothing_WhenNoPet()
    {
        // Arrange
        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Gold = 50,
            Pet = null,
            Members = new List<Character>()
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act
        await _gameStateService.RemovePetFromWarbandAsync("test-warband");

        // Assert
        Assert.Null(warband.Pet);
        Assert.Equal(50, warband.Gold);
    }

    #endregion

    #region Pet Validation Tests

    [Fact]
    public async Task CanWarbandHavePetAsync_ShouldReturnTrue_WhenNoPet()
    {
        // Arrange
        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Pet = null
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act
        var result = await _gameStateService.CanWarbandHavePetAsync("test-warband");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanWarbandHavePetAsync_ShouldReturnFalse_WhenHasPet()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "goodest-doggo",
            Name = "The Goodest Doggo",
            Type = "pet",
            Cost = 15
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Pet = pet
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act
        var result = await _gameStateService.CanWarbandHavePetAsync("test-warband");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetWarbandPetAsync_ShouldReturnPet_WhenExists()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "goodest-doggo",
            Name = "The Goodest Doggo",
            Type = "pet",
            Cost = 15
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Pet = pet
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act
        var result = await _gameStateService.GetWarbandPetAsync("test-warband");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("The Goodest Doggo", result.Name);
    }

    [Fact]
    public async Task GetWarbandPetAsync_ShouldReturnNull_WhenNoPet()
    {
        // Arrange
        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Pet = null
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);

        // Act
        var result = await _gameStateService.GetWarbandPetAsync("test-warband");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Pet Equipment Tests

    [Fact]
    public async Task EquipPetArmorAsync_ShouldEquipArmor_OnPet()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "goodest-doggo",
            Name = "The Goodest Doggo",
            Type = "pet",
            Cost = 15
        };

        var petArmor = new Equipment
        {
            Id = "pet-armor",
            Name = "Pet Armor",
            Type = "armor",
            ArmorType = "pet",
            ArmorValue = 1,
            Cost = 10
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Name = "Test Warband",
            Pet = pet,
            Gold = 10,
            Members = new List<Character>()
        };

        _mockWarbandRepository
            .Setup(x => x.GetByIdAsync("test-warband"))
            .ReturnsAsync(warband);
        _mockWarbandRepository
            .Setup(x => x.SaveAsync(It.IsAny<Warband>()))
            .ReturnsAsync((Warband w) => w);

        // Act
        await _gameStateService.EquipPetArmorAsync("test-warband", petArmor);

        // Assert
        _mockWarbandRepository.Verify(x => x.SaveAsync(It.IsAny<Warband>()), Times.Once);
        Assert.NotNull(warband.Pet?.EquippedItems);
        // Note: The armor gets a new ID when copied, so search by Name instead
        var equippedArmor = warband.Pet.EquippedItems?.FirstOrDefault(e => e.Name == "Pet Armor");
        Assert.NotNull(equippedArmor);
        Assert.True(equippedArmor.IsEquipped);
    }

    #endregion

    #region Pet Cost Tests

    [Fact]
    public void CalculatePetCostWithFeatModifiers_ShouldApplyAnimalLoverDiscount()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "fire-hawk",
            Name = "Fire Hawk",
            Type = "pet",
            Cost = 20
        };

        var character = new Character
        {
            Id = "char-1",
            Name = "Adventurer",
            Feats = new List<string> { "Animal Lover" }
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Members = new List<Character> { character }
        };

        // Act
        var cost = _gameStateService.CalculatePetCostWithFeatModifiers(pet, warband);

        // Assert
        Assert.Equal(10, cost); // 20 / 2 = 10
    }

    [Fact]
    public void CalculatePetCostWithFeatModifiers_ShouldNotApplyDiscount_WithoutFeat()
    {
        // Arrange
        var pet = new Equipment
        {
            Id = "fire-hawk",
            Name = "Fire Hawk",
            Type = "pet",
            Cost = 20
        };

        var warband = new Warband
        {
            Id = "test-warband",
            Members = new List<Character>()
        };

        // Act
        var cost = _gameStateService.CalculatePetCostWithFeatModifiers(pet, warband);

        // Assert
        Assert.Equal(20, cost);
    }

    #endregion

    #region Pet Stats Tests

    [Fact]
    public async Task GetAvailablePetsAsync_ShouldReturnPets_ForGameVariant()
    {
        // Arrange
        var petSystemData = new PetSystemData
        {
            AvailablePets = new List<PetDefinition>
            {
                new PetDefinition
                {
                    Name = "The Goodest Doggo",
                    Cost = 15,
                    Stats = new PetStats
                    {
                        Agility = 2,
                        Presence = 0,
                        Strength = 0,
                        Toughness = -2
                    },
                    Attack = new PetAttack
                    {
                        Name = "Bite",
                        Damage = "D6",
                        Stat = "Strength"
                    },
                    Special = new List<string> { "Can search for Treasure", "Can pick up and carry 1 Item" }
                },
                new PetDefinition
                {
                    Name = "Pet Rock",
                    Cost = 0,
                    Stats = new PetStats
                    {
                        Agility = -10,
                        Presence = 0,
                        Strength = 0,
                        Toughness = 10
                    },
                    Special = new List<string> { "It's a rock", "Can be thrown at enemies" }
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<PetSystemData>("end-times", "pets-system.json"))
            .ReturnsAsync(petSystemData);

        // Act
        var pets = await _gameStateService.GetAvailablePetsAsync("end-times");

        // Assert
        Assert.NotEmpty(pets);
        Assert.Equal(2, pets.Count);
    }

    #endregion
}
