using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class PetServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly PetService _service;

    public PetServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new PetService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetPetsAsync_ShouldReturnEmptyList_WhenGameVariantIsEmpty()
    {
        // Act
        var result = await _service.GetPetsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPetsAsync_ShouldReturnEmptyList_WhenGameVariantIsNull()
    {
        // Act
        var result = await _service.GetPetsAsync(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPetsAsync_ShouldLoadPets_WhenDataExists()
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Category = "companion",
                Cost = 15,
                Slots = 1,
                Damage = "D6",
                Stat = "Strength",
                Properties = new List<string> { "Can search for Treasure" },
                IconClass = "fas fa-dog"
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync(petData);

        // Act
        var pets = await _service.GetPetsAsync("end-times");

        // Assert
        Assert.NotEmpty(pets);
        Assert.Single(pets);
        Assert.Equal("The Goodest Doggo", pets[0].Name);
        Assert.Equal("pet", pets[0].Type);
    }

    [Fact]
    public async Task GetPetsAsync_ShouldCachePets_AfterFirstLoad()
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Cost = 15,
                Slots = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync(petData);

        // Act - Call twice
        await _service.GetPetsAsync("end-times");
        await _service.GetPetsAsync("end-times");

        // Assert - ResourceService should only be called once due to caching
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPetsAsync_ShouldReturnEmptyList_WhenJsonIsNull()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync((List<Equipment>?)null);

        // Act
        var pets = await _service.GetPetsAsync("end-times");

        // Assert
        Assert.Empty(pets);
    }

    [Fact]
    public async Task GetPetsAsync_ShouldHandleException_Gracefully()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ThrowsAsync(new Exception("Resource loading failed"));

        // Act
        var pets = await _service.GetPetsAsync("end-times");

        // Assert
        Assert.Empty(pets);
    }

    [Fact]
    public async Task GetPetByIdAsync_ShouldReturnPet_WhenIdExists()
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Cost = 15,
                Slots = 1
            },
            new Equipment
            {
                Id = "pet-rock",
                Name = "Pet Rock",
                Type = "pet",
                Cost = 0,
                Slots = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync(petData);

        // Act
        var pet = await _service.GetPetByIdAsync("goodest-doggo", "end-times");

        // Assert
        Assert.NotNull(pet);
        Assert.Equal("The Goodest Doggo", pet.Name);
        Assert.Equal(15, pet.Cost);
    }

    [Fact]
    public async Task GetPetByIdAsync_ShouldReturnNull_WhenIdNotFound()
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Cost = 15,
                Slots = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync(petData);

        // Act
        var pet = await _service.GetPetByIdAsync("nonexistent-pet", "end-times");

        // Assert
        Assert.Null(pet);
    }

    [Fact]
    public async Task GetAvailablePetsAsync_ShouldExcludePetsNotYetAvailable()
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Category = "companion",
                Cost = 15,
                Slots = 1
            },
            new Equipment
            {
                Id = "fire-hawk",
                Name = "Fire Hawk",
                Type = "pet",
                Category = "flyer",
                Cost = 20,
                Slots = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>("end-times", "pets.json"))
            .ReturnsAsync(petData);

        // Act - Get available pets in Chapter 1 (all pets are available)
        var pets = await _service.GetAvailablePetsAsync("end-times");

        // Assert
        Assert.NotEmpty(pets);
        Assert.Equal(2, pets.Count);
    }

    [Theory]
    [InlineData("28-psalms")]
    [InlineData("end-times")]
    [InlineData("last-war")]
    public async Task GetPetsAsync_ShouldWorkForAllGameVariants(string gameVariant)
    {
        // Arrange
        var petData = new List<Equipment>
        {
            new Equipment
            {
                Id = "goodest-doggo",
                Name = "The Goodest Doggo",
                Type = "pet",
                Cost = 15,
                Slots = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Equipment>>(gameVariant, "pets.json"))
            .ReturnsAsync(petData);

        // Act
        var pets = await _service.GetPetsAsync(gameVariant);

        // Assert
        Assert.NotEmpty(pets);
        Assert.Equal("The Goodest Doggo", pets[0].Name);
    }
}
