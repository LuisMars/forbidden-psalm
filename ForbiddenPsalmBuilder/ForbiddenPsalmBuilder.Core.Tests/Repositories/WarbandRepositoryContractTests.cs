using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Repositories;
using FluentAssertions;

namespace ForbiddenPsalmBuilder.Core.Tests.Repositories;

public abstract class WarbandRepositoryContractTests
{
    protected abstract IWarbandRepository CreateRepository();

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithNewWarband_ShouldReturnWarbandWithId()
    {
        // Arrange
        var repository = CreateRepository();
        var warband = new Warband("Test Warband", "28-psalms")
        {
            Gold = 100
        };

        // Act
        var result = await repository.SaveAsync(warband);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Test Warband");
        result.GameVariant.Should().Be("28-psalms");
        result.Gold.Should().Be(100);
    }

    [Fact]
    public async Task SaveAsync_WithExistingWarband_ShouldUpdateWarband()
    {
        // Arrange
        var repository = CreateRepository();
        var warband = new Warband("Original Name", "28-psalms")
        {
            Gold = 100
        };
        var savedWarband = await repository.SaveAsync(warband);

        // Act
        savedWarband.Name = "Updated Name";
        savedWarband.Gold = 200;
        var result = await repository.SaveAsync(savedWarband);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(savedWarband.Id);
        result.Name.Should().Be("Updated Name");
        result.Gold.Should().Be(200);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnWarband()
    {
        // Arrange
        var repository = CreateRepository();
        var warband = new Warband("Test Warband", "28-psalms");
        var savedWarband = await repository.SaveAsync(warband);

        // Act
        var result = await repository.GetByIdAsync(savedWarband.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(savedWarband.Id);
        result.Name.Should().Be("Test Warband");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldReturnTrueAndRemoveWarband()
    {
        // Arrange
        var repository = CreateRepository();
        var warband = new Warband("Test Warband", "28-psalms");
        var savedWarband = await repository.SaveAsync(warband);

        // Act
        var deleteResult = await repository.DeleteAsync(savedWarband.Id);
        var getResult = await repository.GetByIdAsync(savedWarband.Id);

        // Assert
        deleteResult.Should().BeTrue();
        getResult.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.DeleteAsync("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var repository = CreateRepository();
        var warband = new Warband("Test Warband", "28-psalms");
        var savedWarband = await repository.SaveAsync(warband);

        // Act
        var result = await repository.ExistsAsync(savedWarband.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.ExistsAsync("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleWarbands_ShouldReturnAllWarbands()
    {
        // Arrange
        var repository = CreateRepository();
        var warband1 = new Warband("Warband 1", "28-psalms");
        var warband2 = new Warband("Warband 2", "end-times");
        var warband3 = new Warband("Warband 3", "last-war");

        await repository.SaveAsync(warband1);
        await repository.SaveAsync(warband2);
        await repository.SaveAsync(warband3);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(w => w.Name == "Warband 1");
        result.Should().Contain(w => w.Name == "Warband 2");
        result.Should().Contain(w => w.Name == "Warband 3");
    }
}