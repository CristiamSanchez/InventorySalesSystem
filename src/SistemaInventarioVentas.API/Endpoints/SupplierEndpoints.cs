using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class SupplierEndpoints
{
    public static RouteGroupBuilder MapSuppliers(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/suppliers")
            .WithTags("Suppliers")
            .RequireAuthorization();

        // Reads: Authenticated (User or Admin)
        group.MapGet("/", ListAsync)
            .WithSummary("List and filter suppliers")
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetSupplier")
            .WithSummary("Get a supplier by ID")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Mutations: Admin only
        group.MapPost("/", CreateAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateSupplier")
            .WithSummary("Create a supplier")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update supplier details")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", ActivateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Activate a supplier")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/deactivate", DeactivateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Deactivate a supplier")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<SupplierResponse>, ValidationProblem, ProblemHttpResult>>
        CreateAsync(
            CreateSupplierRequest request,
            SupplierUseCases useCases,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.CreateAsync(
            name: request.Name!,
            contactName: request.ContactName,
            identificationNumber: request.IdentificationNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        var supplier = result.Value;
        var response = supplier.ToResponse();
        return TypedResults.CreatedAtRoute(
            response,
            "GetSupplier",
            new { id = supplier.Id });
    }

    private static async Task<Results<Ok<SupplierResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        SupplierUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Ok<IReadOnlyList<SupplierResponse>>> ListAsync(
        bool? isActive,
        string? searchTerm,
        SupplierUseCases useCases,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(
            new SupplierQuery(isActive, searchTerm),
            cancellationToken);

        return TypedResults.Ok<IReadOnlyList<SupplierResponse>>(
            result.Value.Select(supplier => supplier.ToResponse()).ToArray());
    }

    private static async Task<Results<Ok<SupplierResponse>, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateSupplierRequest request,
        SupplierUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.UpdateDetailsAsync(
            id: id,
            name: request.Name!,
            contactName: request.ContactName,
            identificationNumber: request.IdentificationNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SupplierResponse>, ProblemHttpResult>> ActivateAsync(
        Guid id,
        SupplierUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ActivateAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<SupplierResponse>, ProblemHttpResult>> DeactivateAsync(
        Guid id,
        SupplierUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.DeactivateAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
