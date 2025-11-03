using ForbiddenPsalmBuilder.Core.Models.Selection;
using Xunit;

namespace ForbiddenPsalmBuilder.Core.Tests.Models.Selection;

public class SelectableItemTests
{
    [Fact]
    public void SelectableItem_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var item = new SelectableItem();

        // Assert
        Assert.NotNull(item.Id);
        Assert.NotEmpty(item.Id);
        Assert.Equal(string.Empty, item.Name);
        Assert.Equal(string.Empty, item.DisplayName);
        Assert.Equal(string.Empty, item.Description);
        Assert.Equal(string.Empty, item.Category);
        Assert.Null(item.Cost);
        Assert.Equal("fas fa-circle", item.IconClass);
        Assert.NotNull(item.Metadata);
        Assert.Empty(item.Metadata);
    }

    [Fact]
    public void SelectableItem_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var item = new SelectableItem
        {
            Id = "test-id",
            Name = "Test Item",
            DisplayName = "Test Display Name",
            Description = "Test description",
            Category = "test-category",
            Cost = 100,
            IconClass = "fas fa-test"
        };

        // Assert
        Assert.Equal("test-id", item.Id);
        Assert.Equal("Test Item", item.Name);
        Assert.Equal("Test Display Name", item.DisplayName);
        Assert.Equal("Test description", item.Description);
        Assert.Equal("test-category", item.Category);
        Assert.Equal(100, item.Cost);
        Assert.Equal("fas fa-test", item.IconClass);
    }

    [Fact]
    public void SelectableItem_Metadata_ShouldAllowAddingValues()
    {
        // Arrange
        var item = new SelectableItem();

        // Act
        item.Metadata["test-key"] = "test-value";
        item.Metadata["numeric-key"] = 42;

        // Assert
        Assert.Equal(2, item.Metadata.Count);
        Assert.Equal("test-value", item.Metadata["test-key"]);
        Assert.Equal(42, item.Metadata["numeric-key"]);
    }

    [Fact]
    public void GetDetailedInfo_ShouldReturnFormattedString()
    {
        // Arrange
        var item = new SelectableItem
        {
            Name = "Test Item",
            Description = "Test description",
            Cost = 50
        };

        // Act
        var info = item.GetDetailedInfo();

        // Assert
        Assert.Contains("Test Item", info);
        Assert.Contains("Test description", info);
        Assert.Contains("50", info);
    }

    [Fact]
    public void CreateNone_ShouldCreateNoneItemWithCategory()
    {
        // Arrange & Act
        var noneItem = SelectableItem.CreateNone("special-class");

        // Assert
        Assert.Equal("none", noneItem.Id);
        Assert.Equal("None", noneItem.Name);
        Assert.Contains("special-class", noneItem.DisplayName);
        Assert.Equal(0, noneItem.Cost);
        Assert.Equal("fas fa-times-circle", noneItem.IconClass);
        Assert.Equal("none", noneItem.Category);
    }

    [Fact]
    public void CreateNone_WithDifferentCategories_ShouldGenerateUniqueDisplayNames()
    {
        // Arrange & Act
        var noneSpecialClass = SelectableItem.CreateNone("special-class");
        var noneWeapon = SelectableItem.CreateNone("weapon");
        var noneFeat = SelectableItem.CreateNone("feat");

        // Assert
        Assert.Contains("special-class", noneSpecialClass.DisplayName);
        Assert.Contains("weapon", noneWeapon.DisplayName);
        Assert.Contains("feat", noneFeat.DisplayName);
    }
}