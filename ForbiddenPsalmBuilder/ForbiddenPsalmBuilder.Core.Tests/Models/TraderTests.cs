using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models;

public class TraderTests
{
    [Fact]
    public void Trader_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix the Mad Wizard",
            BuyMultiplier = 0.5m,
            SellMultiplier = 1.0m,
            GameVariant = "end-times"
        };

        // Assert
        Assert.Equal("mad-wizard", trader.Id);
        Assert.Equal("Vriprix the Mad Wizard", trader.Name);
        Assert.Equal(0.5m, trader.BuyMultiplier);
        Assert.Equal(1.0m, trader.SellMultiplier);
        Assert.Equal("end-times", trader.GameVariant);
    }

    [Fact]
    public void Trader_CalculateBuyPrice_ShouldReturnFullPrice()
    {
        // Arrange - Vriprix sells at full price
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix",
            BuyMultiplier = 0.5m,
            SellMultiplier = 1.0m
        };

        // Act - Player buying from trader
        var buyPrice = trader.CalculateBuyPrice(10);

        // Assert - Full price
        Assert.Equal(10, buyPrice);
    }

    [Fact]
    public void Trader_CalculateBuyPrice_ShouldRoundDown()
    {
        // Arrange
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix",
            BuyMultiplier = 0.5m,
            SellMultiplier = 1.0m
        };

        // Act - Player buying from trader
        var buyPrice = trader.CalculateBuyPrice(7); // 7 * 1.0 = 7

        // Assert
        Assert.Equal(7, buyPrice);
    }

    [Fact]
    public void Trader_CalculateSellPrice_ShouldStartAtHalfCost()
    {
        // Arrange - Base sell price is 50% of item cost
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix",
            BuyMultiplier = 0.5m,
            SellMultiplier = 1.0m // No modifier, so base 50%
        };

        // Act
        var sellPrice = trader.CalculateSellPrice(10);

        // Assert - Should be 50% of 10 = 5
        Assert.Equal(5, sellPrice);
    }

    [Fact]
    public void Trader_CalculateSellPrice_ShouldApplyBuyMultiplier()
    {
        // Arrange - Generous trader that buys at 50% of item cost
        var trader = new Trader
        {
            Id = "generous-trader",
            Name = "Generous Trader",
            BuyMultiplier = 0.5m, // Buys at 50%
            SellMultiplier = 1.0m
        };

        // Act - Player selling to trader
        var sellPrice = trader.CalculateSellPrice(10);

        // Assert - Should be 10 * 0.5 = 5
        Assert.Equal(5, sellPrice);
    }

    [Fact]
    public void TheMerchant_CalculateBuyPrice_ShouldSubtractOneGold()
    {
        // Arrange - The Merchant sells for 1 Gold less (player buys cheaper)
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            BuyMultiplier = 0.5m,
            BuyModifier = 1,
            SellMultiplier = 1.0m,
            SellModifier = -1 // -1 Gold when selling to player
        };

        // Act - Player buying from merchant
        var buyPrice = merchant.CalculateBuyPrice(10); // (10 * 1.0) - 1 = 9

        // Assert
        Assert.Equal(9, buyPrice);
    }

    [Fact]
    public void TheMerchant_CalculateSellPrice_ShouldAddOneGold()
    {
        // Arrange - The Merchant buys for +1 Gold more (pays player more)
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            BuyMultiplier = 0.5m,
            BuyModifier = 1, // +1 Gold when buying from player
            SellMultiplier = 1.0m
        };

        // Act - Player selling to merchant
        var sellPrice = merchant.CalculateSellPrice(10); // (10 * 0.5) + 1 = 6

        // Assert
        Assert.Equal(6, sellPrice);
    }

    [Fact]
    public void TheMerchant_CalculateSellPrice_ShouldNotExceedItemCost()
    {
        // Arrange - The Merchant buys for +1G, but shouldn't pay more than item cost
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            BuyMultiplier = 0.5m,
            BuyModifier = 1,
            SellMultiplier = 1.0m,
            SellModifier = -1,
            MinimumSellPrice = 1
        };

        // Act - Player selling 1G item: (1 * 0.5) + 1 = 1.5, but should cap at 1G
        var sellPrice = merchant.CalculateSellPrice(1);

        // Assert - Should not exceed item cost
        Assert.Equal(1, sellPrice);
    }

    [Fact]
    public void TheMerchant_CalculateBuyPrice_ShouldRespectMinimumSellPrice()
    {
        // Arrange - The Merchant sells for -1G, but has minimum 1G
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            BuyMultiplier = 0.5m,
            BuyModifier = 1,
            SellMultiplier = 1.0m,
            SellModifier = -1,
            MinimumSellPrice = 1
        };

        // Act - Player buying 1G item: (1 * 1.0) - 1 = 0, but minimum is 1G
        var buyPrice = merchant.CalculateBuyPrice(1);

        // Assert - Should not go below minimum
        Assert.Equal(1, buyPrice);
    }

    [Fact]
    public void Trader_CalculateSellPrice_ShouldReturnZeroForUnsellableItems()
    {
        // Arrange - Standard trader buying 1G item
        var trader = new Trader
        {
            Id = "mad-wizard",
            Name = "Vriprix",
            BuyMultiplier = 0.5m,
            SellMultiplier = 1.0m
        };

        // Act - Player selling 1G item: 1 * 0.5 = 0.5, rounded down = 0 (can't sell)
        var sellPrice = trader.CalculateSellPrice(1);

        // Assert - Returns 0 to indicate item cannot be sold
        Assert.Equal(0, sellPrice);
    }

    [Fact]
    public void Trader_ShouldSupportInventoryLimits()
    {
        // Arrange
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasLimitedInventory = true,
            MaxWeapons = 3,
            MaxArmor = 1,
            MaxEquipment = 1
        };

        // Assert
        Assert.True(merchant.HasLimitedInventory);
        Assert.Equal(3, merchant.MaxWeapons);
        Assert.Equal(1, merchant.MaxArmor);
        Assert.Equal(1, merchant.MaxEquipment);
    }

    [Fact]
    public void Trader_ShouldSupportRiskMechanic()
    {
        // Arrange
        var merchant = new Trader
        {
            Id = "merchant",
            Name = "The Merchant",
            HasRisk = true,
            RiskDieSize = 20,
            RiskFailValue = 1,
            RiskPenalty = "Lost Limb injury"
        };

        // Assert
        Assert.True(merchant.HasRisk);
        Assert.Equal(20, merchant.RiskDieSize);
        Assert.Equal(1, merchant.RiskFailValue);
        Assert.Equal("Lost Limb injury", merchant.RiskPenalty);
    }

    [Fact]
    public void Trader_ShouldSupportChapterRestrictions()
    {
        // Arrange
        var hogsHead = new Trader
        {
            Id = "hogs-head",
            Name = "Hogs Head Inn",
            MinimumChapter = 2,
            IsUpgradeShop = true
        };

        // Assert
        Assert.Equal(2, hogsHead.MinimumChapter);
        Assert.True(hogsHead.IsUpgradeShop);
    }
}
