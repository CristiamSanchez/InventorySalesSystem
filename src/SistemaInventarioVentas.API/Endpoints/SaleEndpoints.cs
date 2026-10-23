using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class SaleEndpoints
{
    public static RouteGroupBuilder MapSales(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sales")
            .WithTags("Sales")
            .RequireAuthorization();

        // Reads: Authenticated (User or Admin)
        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetSale")
            .WithSummary("Get a sale by ID")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Mutations: Admin only
        group.MapPost("/", CreateDraftAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateSale")
            .WithSummary("Create a draft sale")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/customer", UpdateCustomerAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update or clear the customer on a draft sale")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/details", AddDetailAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Add a detail line to a draft sale")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/details/{detailIdOrProductIdentifier}", UpdateDetailAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update a detail line on a draft sale by detail ID or product identifier")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/details/{detailIdOrProductIdentifier}", RemoveDetailAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Remove a detail line from a draft sale by detail ID or product identifier")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/confirm", ConfirmAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Confirm a draft sale and deduct inventory stock")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<SaleResponse>, ValidationProblem, ProblemHttpResult>>
        CreateDraftAsync(
            CreateSaleRequest request,
            SaleUseCases useCases,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var userId = request.UserId ?? currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "A valid User ID is required to create a sale.",
                instance: httpContext.Request.Path);
        }

        var result = await useCases.CreateDraftAsync(
            request.CustomerReference,
            userId.Value,
            customerId: request.CustomerId,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        var sale = result.Value;
        return TypedResults.CreatedAtRoute(
            sale.ToResponse(),
            "GetSale",
            new { id = sale.Id });
    }

    private static async Task<Results<Ok<SaleResponse>, ValidationProblem, ProblemHttpResult>>
        UpdateCustomerAsync(
            Guid id,
            UpdateSaleCustomerRequest request,
            SaleUseCases useCases,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.UpdateCustomerAsync(
            id,
            request.CustomerId,
            request.CustomerReference,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SaleResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        SaleUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SaleResponse>, ValidationProblem, ProblemHttpResult>> AddDetailAsync(
        Guid id,
        AddSaleDetailRequest request,
        SaleUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.AddDetailAsync(
            id,
            request.ProductIdentifier!,
            request.Quantity!.Value,
            request.UnitPrice!.Value,
            request.TaxRate!.Value,
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SaleResponse>, ValidationProblem, ProblemHttpResult>> UpdateDetailAsync(
        Guid id,
        string detailIdOrProductIdentifier,
        UpdateSaleDetailRequest request,
        SaleUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = Guid.TryParse(detailIdOrProductIdentifier, out var detailId)
            ? await useCases.UpdateDetailAsync(id, detailId, request.Quantity!.Value, request.UnitPrice, request.TaxRate, cancellationToken)
            : await useCases.UpdateDetailAsync(id, detailIdOrProductIdentifier, request.Quantity!.Value, request.UnitPrice, request.TaxRate, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SaleResponse>, ProblemHttpResult>> RemoveDetailAsync(
        Guid id,
        string detailIdOrProductIdentifier,
        SaleUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = Guid.TryParse(detailIdOrProductIdentifier, out var detailId)
            ? await useCases.RemoveDetailAsync(id, detailId, cancellationToken)
            : await useCases.RemoveDetailAsync(id, detailIdOrProductIdentifier, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SaleResponse>, ProblemHttpResult>> ConfirmAsync(
        Guid id,
        SaleUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ConfirmSaleAsync(id, cancellationToken: cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
