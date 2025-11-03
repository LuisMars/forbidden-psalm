using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Services;
using ForbiddenPsalmBuilder.Data.Services;
using Moq;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Services;

public class CampaignProgressionServiceTests
{
    private readonly Mock<IEmbeddedResourceService> _mockResourceService;
    private readonly CampaignProgressionService _service;

    public CampaignProgressionServiceTests()
    {
        _mockResourceService = new Mock<IEmbeddedResourceService>();
        _service = new CampaignProgressionService(_mockResourceService.Object);
    }

    [Fact]
    public async Task GetCampaignProgressionAsync_ShouldReturnProgression()
    {
        // Arrange
        var progression = new CampaignProgression
        {
            XpCost = 5,
            XpSpending = new List<XpSpendingOption>
            {
                new XpSpendingOption
                {
                    Action = "improve_stat",
                    Description = "Improve warband member's ability by 1",
                    Target = "stat",
                    Amount = 1
                },
                new XpSpendingOption
                {
                    Action = "reroll_flaw",
                    Description = "Reroll warband member's Flaw",
                    Target = "flaw"
                }
            }
        };

        _mockResourceService
            .Setup(x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"))
            .ReturnsAsync(progression);

        // Act
        var result = await _service.GetCampaignProgressionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.XpCost);
        Assert.Equal(2, result.XpSpending.Count);
        Assert.Equal("improve_stat", result.XpSpending[0].Action);
        Assert.Equal("reroll_flaw", result.XpSpending[1].Action);
    }

    [Fact]
    public async Task GetCampaignProgressionAsync_ShouldCacheResult()
    {
        // Arrange
        var progression = new CampaignProgression
        {
            XpCost = 5,
            XpSpending = new List<XpSpendingOption>()
        };

        _mockResourceService
            .Setup(x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"))
            .ReturnsAsync(progression);

        // Act
        var result1 = await _service.GetCampaignProgressionAsync();
        var result2 = await _service.GetCampaignProgressionAsync();

        // Assert
        Assert.Same(result1, result2); // Should return same cached instance
        _mockResourceService.Verify(
            x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"),
            Times.Once); // Should only call once due to caching
    }

    [Fact]
    public async Task GetBaseXpCostAsync_ShouldReturnCorrectCost()
    {
        // Arrange
        var progression = new CampaignProgression
        {
            XpCost = 5,
            XpSpending = new List<XpSpendingOption>()
        };

        _mockResourceService
            .Setup(x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"))
            .ReturnsAsync(progression);

        // Act
        var result = await _service.GetBaseXpCostAsync();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetXpSpendingOptionsAsync_ShouldReturnAllOptions()
    {
        // Arrange
        var progression = new CampaignProgression
        {
            XpCost = 5,
            XpSpending = new List<XpSpendingOption>
            {
                new XpSpendingOption { Action = "improve_stat", Description = "Improve stat", Target = "stat" },
                new XpSpendingOption { Action = "remove_injury", Description = "Remove injury", Target = "injury" },
                new XpSpendingOption { Action = "reroll_flaw", Description = "Reroll flaw", Target = "flaw" },
                new XpSpendingOption { Action = "gain_feat", Description = "Gain feat", Target = "feat" }
            }
        };

        _mockResourceService
            .Setup(x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"))
            .ReturnsAsync(progression);

        // Act
        var result = await _service.GetXpSpendingOptionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Contains(result, o => o.Action == "improve_stat");
        Assert.Contains(result, o => o.Action == "remove_injury");
        Assert.Contains(result, o => o.Action == "reroll_flaw");
        Assert.Contains(result, o => o.Action == "gain_feat");
    }

    [Fact]
    public async Task GetCampaignProgressionAsync_WhenFileNotFound_ShouldReturnDefault()
    {
        // Arrange
        _mockResourceService
            .Setup(x => x.GetSharedResourceAsync<CampaignProgression>("campaign-progression.json"))
            .ReturnsAsync((CampaignProgression?)null);

        // Act
        var result = await _service.GetCampaignProgressionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.XpCost); // Default value
        Assert.Empty(result.XpSpending);
    }
}
