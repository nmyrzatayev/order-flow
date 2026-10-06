using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using OrderFlow.Application.Orders.Queries.GetOrderById;

namespace OrderFlow.Infrastructure.Queries;

public sealed class OrderQueries : IOrderQueries
{
    private readonly string _connectionString;
    private readonly HybridCache _cache;

    public OrderQueries(string connectionString, HybridCache cache)
    {
        _connectionString = connectionString;
        _cache = cache;
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"orders:{orderId}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry => await FetchOrderFromDatabaseAsync(orderId, entry),
            cancellationToken: cancellationToken);
    }

    private async Task<OrderResponse?> FetchOrderFromDatabaseAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT 
                o."Id" AS "Id",
                o."CustomerId" AS "CustomerId",
                o."Status" AS "Status",
                o."CreatedAtUtc" AS "CreatedAtUtc",
                o."PaidAtUtc" AS "PaidAtUtc",
                i."ProductId" AS "ProductId",
                i."Quantity" AS "Quantity",
                i.unit_price_amount AS "UnitPrice",
                i.unit_price_currency AS "Currency"
            FROM orders o
            LEFT JOIN order_items i ON o."Id" = i."OrderId"
            WHERE o."Id" = @OrderId
            """;

        var orderDictionary = new Dictionary<Guid, OrderResponse>();

        var rows = await connection.QueryAsync<dynamic>(sql, new { OrderId = orderId });
        var rowsList = rows.ToList();

        if (rowsList.Count == 0)
        {
            return null;
        }

        var firstRow = rowsList[0];
        var items = new List<OrderItemResponse>();
        decimal totalAmount = 0;
        string currency = "USD";

        foreach (var row in rowsList)
        {
            if (row.ProductId is not null)
            {
                var unitPrice = (decimal)row.UnitPrice;
                var quantity = (int)row.Quantity;
                var itemTotalPrice = unitPrice * quantity;
                currency = (string)row.Currency;
                totalAmount += itemTotalPrice;

                items.Add(new OrderItemResponse(
                    (Guid)row.ProductId,
                    quantity,
                    unitPrice,
                    currency,
                    itemTotalPrice));
            }
        }

        return new OrderResponse(
            (Guid)firstRow.Id,
            (Guid)firstRow.CustomerId,
            (string)firstRow.Status,
            totalAmount,
            currency,
            (DateTime)firstRow.CreatedAtUtc,
            (DateTime?)firstRow.PaidAtUtc,
            items);
    }
}
