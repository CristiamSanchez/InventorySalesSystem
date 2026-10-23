using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Models.Reports;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class ReportEndpoints
{
    public static RouteGroupBuilder MapReports(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reports")
            .WithTags("Reports");

        // Financial reports (Admin only)
        group.MapGet("/sales-summary", GetSalesSummaryAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("GetSalesSummaryReport")
            .WithSummary("Get sales summary and transaction history report")
            .Produces<SalesSummaryReportResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/product-sales", GetProductSalesAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("GetProductSalesReport")
            .WithSummary("Get product sales performance report")
            .Produces<ProductSalesReportResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Operational inventory reports (Authenticated User or Admin)
        group.MapGet("/inventory-status", GetInventoryStatusAsync)
            .RequireAuthorization()
            .WithName("GetInventoryStatusReport")
            .WithSummary("Get inventory stock status report across catalog products")
            .Produces<InventoryStatusReportResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/inventory-movements", GetInventoryMovementsAsync)
            .RequireAuthorization()
            .WithName("GetInventoryMovementReport")
            .WithSummary("Get inventory movement traceability report")
            .Produces<InventoryMovementReportResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<Results<Ok<SalesSummaryReportResponse>, ProblemHttpResult>> GetSalesSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        Guid? customerId,
        Guid? userId,
        SaleStatus? status,
        ReportingUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!fromUtc.HasValue || !toUtc.HasValue)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "FromUtc and ToUtc are required query parameters.",
                instance: httpContext.Request.Path);
        }

        var result = await useCases.GetSalesSummaryReportAsync(
            new SalesSummaryReportQuery
            {
                FromUtc = fromUtc.Value,
                ToUtc = toUtc.Value,
                CustomerId = customerId,
                UserId = userId,
                Status = status
            },
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<ProductSalesReportResponse>, ProblemHttpResult>> GetProductSalesAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? productIdentifier,
        Guid? categoryId,
        ReportingUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!fromUtc.HasValue || !toUtc.HasValue)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "FromUtc and ToUtc are required query parameters.",
                instance: httpContext.Request.Path);
        }

        var result = await useCases.GetProductSalesReportAsync(
            new ProductSalesReportQuery
            {
                FromUtc = fromUtc.Value,
                ToUtc = toUtc.Value,
                ProductIdentifier = productIdentifier,
                CategoryId = categoryId
            },
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryStatusReportResponse>, ProblemHttpResult>> GetInventoryStatusAsync(
        Guid? categoryId,
        bool? inStockOnly,
        ReportingUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetInventoryStatusReportAsync(
            new InventoryStatusReportQuery
            {
                CategoryId = categoryId,
                InStockOnly = inStockOnly
            },
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<InventoryMovementReportResponse>, ProblemHttpResult>> GetInventoryMovementsAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? productIdentifier,
        MovementType? movementType,
        Guid? supplierId,
        ReportingUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!fromUtc.HasValue || !toUtc.HasValue)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "FromUtc and ToUtc are required query parameters.",
                instance: httpContext.Request.Path);
        }

        var result = await useCases.GetInventoryMovementReportAsync(
            new InventoryMovementReportQuery
            {
                FromUtc = fromUtc.Value,
                ToUtc = toUtc.Value,
                ProductIdentifier = productIdentifier,
                MovementType = movementType,
                SupplierId = supplierId
            },
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
