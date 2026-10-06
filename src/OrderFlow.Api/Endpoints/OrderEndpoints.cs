using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Orders.Commands.CancelOrder;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Application.Orders.Queries.GetOrderById;
using OrderFlow.Domain.Common;

namespace OrderFlow.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", async (
            [FromBody] CreateOrderCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return MapErrorToResponse(result.Error);
            }

            return Results.CreatedAtRoute("GetOrderById", new { id = result.Value }, new { OrderId = result.Value });
        })
        .WithName("CreateOrder")
        .Produces<object>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrderByIdQuery(id);
            var result = await sender.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return MapErrorToResponse(result.Error);
            }

            return Results.Ok(result.Value);
        })
        .WithName("GetOrderById")
        .Produces<OrderResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelOrderRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CancelOrderCommand(id, request.Reason);
            var result = await sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return MapErrorToResponse(result.Error);
            }

            return Results.NoContent();
        })
        .WithName("CancelOrder")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static IResult MapErrorToResponse(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description);
    }

    public sealed record CancelOrderRequest(string Reason);
}
