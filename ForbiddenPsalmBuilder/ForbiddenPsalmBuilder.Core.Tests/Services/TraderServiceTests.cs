using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class TraderServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly TraderService _service;

    public TraderServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new TraderService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetTradersAsync_ShouldLoadTradersFromJson()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix the Mad Wizard", GameVariant = "end-times" }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act
        var result = await _service.GetTradersAsync("end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("mad-wizard", result[0].Id);
        Assert.Equal("Vriprix the Mad Wizard", result[0].Name);
    }

    [Fact]
    public async Task GetTradersAsync_ShouldCacheResults()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix", GameVariant = "end-times" }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act
        var result1 = await _service.GetTradersAsync("end-times");
        var result2 = await _service.GetTradersAsync("end-times");

        // Assert
        Assert.Same(result1, result2);
        _mockResourceService.Verify(
            x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"),
            Times.Once); // Should only call once due to caching
    }

    [Fact]
    public async Task GetTraderByIdAsync_ShouldReturnTraderById()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix", GameVariant = "end-times" },
            new Trader { Id = "merchant", Name = "The Merchant", GameVariant = "end-times" }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act
        var result = await _service.GetTraderByIdAsync("merchant", "end-times");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("merchant", result.Id);
        Assert.Equal("The Merchant", result.Name);
    }

    [Fact]
    public async Task GetTraderByIdAsync_ShouldReturnNull_WhenTraderNotFound()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix", GameVariant = "end-times" }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act
        var result = await _service.GetTraderByIdAsync("nonexistent", "end-times");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTradersAsync_ShouldReturnEmptyList_WhenFileNotFound()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("invalid-variant", "traders.json"))
            .ReturnsAsync((List<Trader>?)null);

        // Act
        var result = await _service.GetTradersAsync("invalid-variant");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTradersByChapterAsync_ShouldFilterByChapter()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix", GameVariant = "end-times", MinimumChapter = null },
            new Trader { Id = "hogs-head", Name = "Hogs Head Inn", GameVariant = "end-times", MinimumChapter = 2 },
            new Trader { Id = "merchant", Name = "The Merchant", GameVariant = "end-times", MinimumChapter = null }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act - Chapter 1
        var chapter1 = await _service.GetTradersByChapterAsync(1, "end-times");

        // Assert
        Assert.Equal(2, chapter1.Count);
        Assert.Contains(chapter1, t => t.Id == "mad-wizard");
        Assert.Contains(chapter1, t => t.Id == "merchant");
        Assert.DoesNotContain(chapter1, t => t.Id == "hogs-head");
    }

    [Fact]
    public async Task GetTradersByChapterAsync_ShouldIncludeAllWhenChapter2()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader { Id = "mad-wizard", Name = "Vriprix", GameVariant = "end-times", MinimumChapter = null },
            new Trader { Id = "hogs-head", Name = "Hogs Head Inn", GameVariant = "end-times", MinimumChapter = 2 }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        // Act - Chapter 2
        var chapter2 = await _service.GetTradersByChapterAsync(2, "end-times");

        // Assert
        Assert.Equal(2, chapter2.Count);
        Assert.Contains(chapter2, t => t.Id == "mad-wizard");
        Assert.Contains(chapter2, t => t.Id == "hogs-head");
    }

    [Fact]
    public async Task CalculateBuyPrice_ShouldUseTraderMethod()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader
            {
                Id = "mad-wizard",
                Name = "Vriprix",
                GameVariant = "end-times",
                BuyMultiplier = 0.5m,
                SellMultiplier = 1.0m
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        var trader = await _service.GetTraderByIdAsync("mad-wizard", "end-times");

        // Act - Player buying from trader (trader selling to player)
        var buyPrice = _service.CalculateBuyPrice(trader!, 10);

        // Assert - Full price: 10 * 1.0 = 10
        Assert.Equal(10, buyPrice);
    }

    [Fact]
    public async Task CalculateSellPrice_ShouldUseTraderMethod()
    {
        // Arrange
        var traders = new List<Trader>
        {
            new Trader
            {
                Id = "merchant",
                Name = "The Merchant",
                GameVariant = "end-times",
                BuyMultiplier = 0.5m,
                SellMultiplier = 1.0m,
                SellModifier = -1,
                MinimumSellPrice = 1
            }
        };

        _mockResourceService
            .Setup(x => x.GetGameResourceAsync<List<Trader>>("end-times", "traders.json"))
            .ReturnsAsync(traders);

        var trader = await _service.GetTraderByIdAsync("merchant", "end-times");

        // Act - Player selling to trader (trader buying from player)
        var sellPrice = _service.CalculateSellPrice(trader!, 10);

        // Assert - Uses buyMultiplier: 10 * 0.5 = 5 (no buyModifier in this test)
        Assert.Equal(5, sellPrice);
    }
}
