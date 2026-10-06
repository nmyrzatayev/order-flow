using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MoneyCalculationBenchmarks>();
    }
}

[MemoryDiagnoser]
public class MoneyCalculationBenchmarks
{
    private Currency _currency = null!;
    private Money _baseMoney = null!;
    private readonly List<Money> _items = [];

    [GlobalSetup]
    public void Setup()
    {
        _currency = Currency.Usd;
        _baseMoney = Money.Create(19.99m, _currency).Value;

        for (int i = 0; i < 100; i++)
        {
            _items.Add(Money.Create(10m + i, _currency).Value);
        }
    }

    [Benchmark(Baseline = true)]
    public Money SumWithAggregate()
    {
        return _items.Aggregate((a, b) => a + b);
    }

    [Benchmark]
    public decimal SumRawDecimals()
    {
        decimal sum = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            sum += _items[i].Amount;
        }
        return sum;
    }
}
