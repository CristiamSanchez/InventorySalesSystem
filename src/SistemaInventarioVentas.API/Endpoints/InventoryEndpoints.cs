using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class InventoryEndpoints
{
    public static RouteGroupBuilder MapInventory(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization();

        // Reads: Authenticated (User or Admin)
        group.MapGet("/{productIdentifier}", GetByProductIdentifierAsync)
            .WithName("GetInventory")
            .WithSummary("Get current inventory for a product")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{productIdentifier}/movements", GetMovementsAsync)
            .WithName("GetInventoryMovements")
            .WithSummary("Get inventory movement history for a product")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Mutations: Admin only
        group.MapPost("/{productIdentifier}/entries", RecordEntryFromRouteAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Record a stock entry for a product in path")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/entries", RecordEntryFromBodyAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Record a stock entry for a product in body")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{productIdentifier}/exits", RecordExitFromRouteAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Record a stock exit for a product in path")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/exits", RecordExitFromBodyAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Record a stock exit for a product in body")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{productIdentifier}/adjustments", RecordAdjustmentFromRouteAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Adjust stock for a product in path")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{productIdentifier}/adjust", RecordAdjustmentFromRouteAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Adjust stock for a product in path (alias)")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/adjustments", RecordAdjustmentFromBodyAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Adjust stock for a product in body")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<Ok<InventoryResponse>, ProblemHttpResult>> GetByProductIdentifierAsync(
        string productIdentifier,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<IReadOnlyList<InventoryMovementResponse>>, ProblemHttpResult>> GetMovementsAsync(
        string productIdentifier,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetMovementsAsync(productIdentifier, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok<IReadOnlyList<InventoryMovementResponse>>(
                result.Value.Select(movement => movement.ToResponse()).ToArray())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordEntryFromRouteAsync(
        string productIdentifier,
        RecordStockEntryRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.RecordEntryAsync(
            productIdentifier,
            request.Quantity!.Value,
            request.Reference!,
            request.OccurredAtUtc,
            request.SupplierId,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordEntryFromBodyAsync(
        RecordStockEntryRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        if (string.IsNullOrWhiteSpace(request.ProductIdentifier))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ProductIdentifier)] = ["Product identifier is required."]
            });
        }

        var result = await useCases.RecordEntryAsync(
            request.ProductIdentifier,
            request.Quantity!.Value,
            request.Reference!,
            request.OccurredAtUtc,
            request.SupplierId,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordExitFromRouteAsync(
        string productIdentifier,
        RecordStockExitRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.RecordExitAsync(
            productIdentifier,
            request.Quantity!.Value,
            request.Reference!,
            request.OccurredAtUtc,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordExitFromBodyAsync(
        RecordStockExitRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        if (string.IsNullOrWhiteSpace(request.ProductIdentifier))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ProductIdentifier)] = ["Product identifier is required."]
            });
        }

        var result = await useCases.RecordExitAsync(
            request.ProductIdentifier,
            request.Quantity!.Value,
            request.Reference!,
            request.OccurredAtUtc,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordAdjustmentFromRouteAsync(
        string productIdentifier,
        AdjustStockRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        if (request.QuantityDelta == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.QuantityDelta)] = ["Adjustment quantity cannot be zero."]
            });
        }

        var result = await useCases.AdjustAsync(
            productIdentifier,
            request.QuantityDelta!.Value,
            request.Reason!,
            request.OccurredAtUtc,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryResponse>, ValidationProblem, ProblemHttpResult>> RecordAdjustmentFromBodyAsync(
        AdjustStockRequest request,
        InventoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        if (string.IsNullOrWhiteSpace(request.ProductIdentifier))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ProductIdentifier)] = ["Product identifier is required."]
            });
        }

        if (request.QuantityDelta == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.QuantityDelta)] = ["Adjustment quantity cannot be zero."]
            });
        }

        var result = await useCases.AdjustAsync(
            request.ProductIdentifier,
            request.QuantityDelta!.Value,
            request.Reason!,
            request.OccurredAtUtc,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
