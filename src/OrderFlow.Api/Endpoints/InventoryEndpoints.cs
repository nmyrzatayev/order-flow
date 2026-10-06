using Microsoft.AspNetCore.Mvc;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory");

        group.MapPost("/seed", async (
            [FromBody] SeedInventoryRequest request,
            IInventoryRepository inventoryRepository,
            IUnitOfWork unitOfWork,
            CancellationToken cancellationToken) =>
        {
            var productId = new ProductId(request.ProductId);
            var existing = await inventoryRepository.GetByProductIdAsync(productId, cancellationToken);

            if (existing is not null)
            {
                existing.Restock(request.Quantity);
            }
            else
            {
                var inventoryResult = Inventory.Create(productId, request.Quantity);
                if (inventoryResult.IsFailure)
                {
                    return Results.BadRequest(inventoryResult.Error.Description);
                }

                await inventoryRepository.AddAsync(inventoryResult.Value, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { request.ProductId, request.Quantity, Status = "Seeded" });
        })
        .WithName("SeedInventory")
        .Produces<object>(StatusCodes.Status200OK);

        group.MapGet("/{productId:guid}", async (
            Guid productId,
            IInventoryRepository inventoryRepository,
            CancellationToken cancellationToken) =>
        {
            var inventory = await inventoryRepository.GetByProductIdAsync(new ProductId(productId), cancellationToken);
            if (inventory is null)
            {
                return Results.NotFound(new { Message = $"Inventory for product {productId} not found." });
            }

            return Results.Ok(new
            {
                InventoryId = inventory.Id.Value,
                ProductId = inventory.ProductId.Value,
                inventory.QuantityAvailable,
                inventory.QuantityReserved,
                inventory.Version
            });
        })
        .WithName("GetInventory")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    public sealed record SeedInventoryRequest(Guid ProductId, int Quantity);
}
