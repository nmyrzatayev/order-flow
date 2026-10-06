using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Shared;

public sealed class Currency : ValueObject
{
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Eur = new("EUR");
    public static readonly Currency Kzt = new("KZT");
    public static readonly Currency Rub = new("RUB");

    public string Code { get; }

    private Currency(string code)
    {
        Code = code;
    }

    public static Result<Currency> FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Currency>(Error.Validation("Currency.Empty", "Currency code cannot be empty."));

        var normalized = code.Trim().ToUpperInvariant();
        return normalized switch
        {
            "USD" => Usd,
            "EUR" => Eur,
            "KZT" => Kzt,
            "RUB" => Rub,
            _ => Result.Failure<Currency>(Error.Validation("Currency.Unsupported", $"Currency '{code}' is not supported."))
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;
}

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Money Zero(Currency currency) => new(0, currency);

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(
                Error.Validation("Money.Negative", "Money amount cannot be negative."));
        }

        return new Money(amount, currency);
    }

    public static Result<Money> Add(Money first, Money second)
    {
        if (first.Currency != second.Currency)
        {
            return Result.Failure<Money>(
                Error.Validation("Money.CurrencyMismatch", "Cannot add money with different currencies."));
        }

        return new Money(first.Amount + second.Amount, first.Currency);
    }

    public static Money operator +(Money first, Money second)
    {
        var result = Add(first, second);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Description);

        return result.Value;
    }

    public static Money operator *(Money money, int factor)
    {
        if (factor < 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Multiplier cannot be negative.");

        return new Money(money.Amount * factor, money.Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency.Code}";
}
