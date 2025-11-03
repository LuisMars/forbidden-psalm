namespace ForbiddenPsalmBuilder.Core.Models.Selection;

/// <summary>
/// Represents a trader/shop in the game where players can buy and sell equipment
/// </summary>
public class Trader
{
    /// <summary>
    /// Unique identifier for the trader (e.g., "mad-wizard", "merchant", "quartermaster")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the trader
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the trader and any special notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Game variant this trader belongs to
    /// </summary>
    public string GameVariant { get; set; } = string.Empty;

    /// <summary>
    /// Multiplier for buying items FROM players (usually 0.5 for 50%)
    /// </summary>
    public decimal BuyMultiplier { get; set; } = 0.5m;

    /// <summary>
    /// Flat modifier added after multiplier when buying (The Merchant: +1)
    /// </summary>
    public int BuyModifier { get; set; } = 0;

    /// <summary>
    /// Multiplier for selling items TO players (usually 1.0 for 100%)
    /// </summary>
    public decimal SellMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Flat modifier added after multiplier when selling (The Merchant: -1)
    /// </summary>
    public int SellModifier { get; set; } = 0;

    /// <summary>
    /// Minimum price when selling to players (The Merchant: 1 Gold minimum)
    /// </summary>
    public int MinimumSellPrice { get; set; } = 0;

    /// <summary>
    /// Whether this trader has limited random inventory (The Merchant)
    /// </summary>
    public bool HasLimitedInventory { get; set; } = false;

    /// <summary>
    /// Maximum number of weapons in inventory (if limited)
    /// </summary>
    public int? MaxWeapons { get; set; }

    /// <summary>
    /// Maximum number of armor pieces in inventory (if limited)
    /// </summary>
    public int? MaxArmor { get; set; }

    /// <summary>
    /// Maximum number of equipment/items in inventory (if limited)
    /// </summary>
    public int? MaxEquipment { get; set; }

    /// <summary>
    /// Whether selling special items like relics
    /// </summary>
    public bool SellsRelics { get; set; } = false;

    /// <summary>
    /// Fixed price for relics if sold by this trader
    /// </summary>
    public int? RelicPrice { get; set; }

    /// <summary>
    /// Whether trading with this trader has a risk (The Merchant: D20 roll)
    /// </summary>
    public bool HasRisk { get; set; } = false;

    /// <summary>
    /// Size of die to roll for risk (20 for D20)
    /// </summary>
    public int? RiskDieSize { get; set; }

    /// <summary>
    /// Value that triggers risk penalty (1 for "on a 1")
    /// </summary>
    public int? RiskFailValue { get; set; }

    /// <summary>
    /// Description of what happens on risk failure
    /// </summary>
    public string? RiskPenalty { get; set; }

    /// <summary>
    /// Description of what happens on second risk failure
    /// </summary>
    public string? SecondRiskPenalty { get; set; }

    /// <summary>
    /// Whether this is an upgrade shop (Hogs Head Inn) rather than equipment trader
    /// </summary>
    public bool IsUpgradeShop { get; set; } = false;

    /// <summary>
    /// Minimum campaign chapter to access this trader
    /// </summary>
    public int? MinimumChapter { get; set; }

    /// <summary>
    /// Icon class for displaying the trader in UI
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Calculate the price for a player to buy an item from this trader
    /// (Trader is selling to player)
    /// </summary>
    /// <param name="baseValue">Base value of the item</param>
    /// <returns>Price the player must pay</returns>
    public int CalculateBuyPrice(int baseValue)
    {
        // Apply sell multiplier and modifier (trader selling to player)
        decimal calculatedPrice = (baseValue * SellMultiplier) + SellModifier;

        // Always round down
        int finalPrice = (int)Math.Floor(calculatedPrice);

        // Apply minimum sell price (can't sell for free or negative)
        if (MinimumSellPrice > 0 && finalPrice < MinimumSellPrice)
        {
            return MinimumSellPrice;
        }

        return finalPrice;
    }

    /// <summary>
    /// Calculate the price this trader pays when buying an item from a player
    /// (Player is selling to trader)
    /// </summary>
    /// <param name="baseValue">Base value of the item</param>
    /// <returns>Price the trader pays</returns>
    public int CalculateSellPrice(int baseValue)
    {
        // Apply buy multiplier and modifier (trader buying from player)
        decimal calculatedPrice = (baseValue * BuyMultiplier) + BuyModifier;

        // Round down
        int finalPrice = (int)Math.Floor(calculatedPrice);

        // Trader should never pay more than the item's base value
        if (finalPrice > baseValue)
        {
            return baseValue;
        }

        return finalPrice;
    }
}
