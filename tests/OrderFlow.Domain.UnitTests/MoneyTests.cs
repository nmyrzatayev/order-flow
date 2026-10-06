using FluentAssertions;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Domain.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Add_WithSameCurrency_ShouldSucceed()
    {
        // Arrange
        var m1 = Money.Create(100.50m, Currency.Usd).Value;
        var m2 = Money.Create(50.25m, Currency.Usd).Value;

        // Act
        var result = Money.Add(m1, m2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(150.75m);
        result.Value.Currency.Should().Be(Currency.Usd);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldFail()
    {
        // Arrange
        var m1 = Money.Create(100m, Currency.Usd).Value;
        var m2 = Money.Create(100m, Currency.Eur).Value;

        // Act
        var result = Money.Add(m1, m2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.CurrencyMismatch");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        // Act
        var result = Money.Create(-10m, Currency.Usd);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.Negative");
    }
}
