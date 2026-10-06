using FluentAssertions;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Inventories.Events;

namespace OrderFlow.Domain.UnitTests;

public class InventoryTests
{
    [Fact]
    public void Reserve_WithSufficientStock_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var inventory = Inventory.Create(ProductId.New(), 10).Value;

        // Act
        var result = inventory.Reserve(3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(7);
        inventory.QuantityReserved.Should().Be(3);
        inventory.DomainEvents.Should().ContainSingle(e => e is InventoryReservedDomainEvent);
    }

    [Fact]
    public void Reserve_WhenInsufficientStock_ShouldFailWithConflict()
    {
        // Arrange
        var inventory = Inventory.Create(ProductId.New(), 2).Value;

        // Act
        var result = inventory.Reserve(5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Inventory.InsufficientStock");
        inventory.QuantityAvailable.Should().Be(2);
        inventory.QuantityReserved.Should().Be(0);
    }

    [Fact]
    public void ReleaseReservation_ShouldReturnStockToAvailable()
    {
        // Arrange
        var inventory = Inventory.Create(ProductId.New(), 10).Value;
        inventory.Reserve(5);

        // Act
        var result = inventory.ReleaseReservation(2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(7);
        inventory.QuantityReserved.Should().Be(3);
        inventory.DomainEvents.Should().Contain(e => e is InventoryReservationReleasedDomainEvent);
    }
}
