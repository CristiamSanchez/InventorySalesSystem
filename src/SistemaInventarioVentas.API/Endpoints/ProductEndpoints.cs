using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class ProductEndpoints
{
    public static RouteGroupBuilder MapProducts(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization();
        group.MapPost("/", CreateAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateProduct")
            .WithSummary("Create a product")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/", ListAsync)
            .WithSummary("List and filter products")
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapGet("/{identifier}", GetAsync)
            .WithName("GetProduct")
            .WithSummary("Get a product by identifier")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{identifier}", UpdateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update a product")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<ProductResponse>, ValidationProblem, ProblemHttpResult>>
        CreateAsync(
            CreateProductRequest request,
            ProductUseCases useCases,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.CreateAsync(
            request.Identifier!,
            request.Name!,
            request.CategoryId!.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        var product = result.Value;
        var response = product.ToResponse();
        return TypedResults.CreatedAtRoute(
            response,
            "GetProduct",
            new { identifier = product.Identifier.Value });
    }

    private static async Task<Ok<IReadOnlyList<ProductResponse>>> ListAsync(
        Guid? categoryId,
        bool? isActive,
        string? searchTerm,
        ProductUseCases useCases,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(
            new ProductQuery(categoryId, isActive, searchTerm),
            cancellationToken);
        return TypedResults.Ok<IReadOnlyList<ProductResponse>>(
            result.Value.Select(product => product.ToResponse()).ToArray());
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetAsync(
        string identifier,
        ProductUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetAsync(identifier, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<ProductResponse>, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        string identifier,
        UpdateProductRequest request,
        ProductUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.UpdateAsync(
            identifier,
            request.Name!,
            request.CategoryId!.Value,
            request.IsActive!.Value,
            cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
