using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Application.Orders.Queries.GetOrderById;

namespace OrderFlow.IntegrationTests;

public class OrderFlowApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderFlowApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidInventory_ShouldReturnCreatedAndAllowQuery()
    {
        // 1. Seed inventory
        var productId = Guid.NewGuid();
        var seedResponse = await _client.PostAsJsonAsync("/api/inventory/seed", new
        {
            ProductId = productId,
            Quantity = 10
        });

        seedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Place an order
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            customerId,
            [
                new CreateOrderItemDto(productId, 2, 99.99m, "USD")
            ]);

        var response = await _client.PostAsJsonAsync("/api/orders", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        createResult.Should().NotBeNull();
        createResult!.OrderId.Should().NotBeEmpty();

        // 3. Query the order via CQRS (Dapper)
        var orderResponse = await _client.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{createResult.OrderId}", JsonOptions);

        orderResponse.Should().NotBeNull();
        orderResponse!.Id.Should().Be(createResult.OrderId);
        orderResponse.CustomerId.Should().Be(customerId);
        orderResponse.TotalAmount.Should().Be(199.98m);
        orderResponse.Items.Should().HaveCount(1);
        orderResponse.Items[0].ProductId.Should().Be(productId);
        orderResponse.Items[0].Quantity.Should().Be(2);

        // 4. Verify Inventory stock was reserved
        var inventoryResponse = await _client.GetFromJsonAsync<InventoryDto>(
            $"/api/inventory/{productId}", JsonOptions);

        inventoryResponse.Should().NotBeNull();
        inventoryResponse!.QuantityAvailable.Should().Be(8);
        inventoryResponse.QuantityReserved.Should().Be(2);
    }

    [Fact]
    public async Task CreateOrder_WithIdempotencyKey_ShouldReturnCachedResultOnDuplicate()
    {
        // 1. Seed inventory
        var productId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/inventory/seed", new
        {
            ProductId = productId,
            Quantity = 50
        });

        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(productId, 1, 20m, "USD")]);

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(command)
        };
        request1.Headers.Add("Idempotency-Key", idempotencyKey);

        var response1 = await _client.SendAsync(request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var result1 = await response1.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);

        // 2. Send duplicate request with SAME idempotency key
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(command)
        };
        request2.Headers.Add("Idempotency-Key", idempotencyKey);

        var response2 = await _client.SendAsync(request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        var result2 = await response2.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);

        // Assert: Same order ID returned from cache without creating a second order!
        result2!.OrderId.Should().Be(result1!.OrderId);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_ShouldReturnConflictProblemDetails()
    {
        // 1. Seed small inventory
        var productId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/inventory/seed", new
        {
            ProductId = productId,
            Quantity = 1
        });

        // 2. Request 5 items
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(productId, 5, 20m, "USD")]);

        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record CreateOrderResponse(Guid OrderId);
    private sealed record InventoryDto(int QuantityAvailable, int QuantityReserved);
}
