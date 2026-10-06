using FluentAssertions;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Domain.UnitTests;

public class OrderTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnOrderAndRaiseOrderCreatedEvent()
    {
        // Arrange
        var customerId = CustomerId.New();
        var productId = ProductId.New();
        var price = Money.Create(100.50m, Currency.Usd).Value;

        // Act
        var result = Order.Create(customerId, [(productId, 2, price)]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.Status.Should().Be(OrderStatus.Pending);
        order.CustomerId.Should().Be(customerId);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Amount.Should().Be(201.00m);
        order.TotalAmount.Currency.Should().Be(Currency.Usd);

        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedDomainEvent);
        var createdEvent = (OrderCreatedDomainEvent)order.DomainEvents.Single(e => e is OrderCreatedDomainEvent);
        createdEvent.OrderId.Should().Be(order.Id);
        createdEvent.TotalAmount.Amount.Should().Be(201.00m);
    }

    [Fact]
    public void Create_WithEmptyItems_ShouldReturnFailure()
    {
        // Arrange
        var customerId = CustomerId.New();

        // Act
        var result = Order.Create(customerId, []);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyItems");
    }

    [Fact]
    public void Create_WithMixedCurrencies_ShouldReturnFailure()
    {
        // Arrange
        var customerId = CustomerId.New();
        var priceUsd = Money.Create(100m, Currency.Usd).Value;
        var priceEur = Money.Create(50m, Currency.Eur).Value;

        // Act
        var result = Order.Create(customerId,
        [
            (ProductId.New(), 1, priceUsd),
            (ProductId.New(), 1, priceEur)
        ]);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MixedCurrencies");
    }

    [Fact]
    public void MarkAsPaid_WhenPending_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var order = Order.Create(CustomerId.New(),
            [(ProductId.New(), 1, Money.Create(50m, Currency.Usd).Value)]).Value;
        var paidAt = DateTime.UtcNow;

        // Act
        var result = order.MarkAsPaid(paidAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAtUtc.Should().Be(paidAt);
        order.DomainEvents.Should().Contain(e => e is OrderPaidDomainEvent);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldReturnConflict()
    {
        // Arrange
        var order = Order.Create(CustomerId.New(),
            [(ProductId.New(), 1, Money.Create(50m, Currency.Usd).Value)]).Value;
        order.MarkAsPaid(DateTime.UtcNow);

        // Act
        var secondPay = order.MarkAsPaid(DateTime.UtcNow);

        // Assert
        secondPay.IsFailure.Should().BeTrue();
        secondPay.Error.Code.Should().Be("Order.AlreadyPaid");
    }
}
