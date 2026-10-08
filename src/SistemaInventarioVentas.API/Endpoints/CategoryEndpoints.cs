using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategories(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();
        group.MapPost("/", CreateAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateCategory")
            .WithSummary("Create a category")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/", ListAsync)
            .WithSummary("List categories")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetCategory")
            .WithSummary("Get a category by ID")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update a category")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", RemoveAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Remove a category")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<CategoryResponse>, ValidationProblem, ProblemHttpResult>>
        CreateAsync(
            CreateCategoryRequest request,
            CategoryUseCases useCases,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.CreateAsync(request.Name!, cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        var category = result.Value;
        var response = category.ToResponse();
        return TypedResults.CreatedAtRoute(response, "GetCategory", new { id = category.Id });
    }

    private static async Task<Ok<IReadOnlyList<CategoryResponse>>> ListAsync(
        CategoryUseCases useCases,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<CategoryResponse>>(
            result.Value.Select(category => category.ToResponse()).ToArray());
    }

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        CategoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<CategoryResponse>, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CategoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.UpdateAsync(
            id,
            request.Name!,
            request.IsActive!.Value,
            cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveAsync(
        Guid id,
        CategoryUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.RemoveAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
