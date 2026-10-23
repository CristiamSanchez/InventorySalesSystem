using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomers(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        // Reads: Authenticated (User or Admin)
        group.MapGet("/", ListAsync)
            .WithSummary("List and filter customers")
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetCustomer")
            .WithSummary("Get a customer by ID")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Mutations: Admin only
        group.MapPost("/", CreateAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateCustomer")
            .WithSummary("Create a customer")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Update customer details")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", ActivateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Activate a customer")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/deactivate", DeactivateAsync)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Deactivate a customer")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<CustomerResponse>, ValidationProblem, ProblemHttpResult>>
        CreateAsync(
            CreateCustomerRequest request,
            CustomerUseCases useCases,
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
            identificationNumber: request.IdentificationNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        var customer = result.Value;
        var response = customer.ToResponse();
        return TypedResults.CreatedAtRoute(
            response,
            "GetCustomer",
            new { id = customer.Id });
    }

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        CustomerUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Ok<IReadOnlyList<CustomerResponse>>> ListAsync(
        bool? isActive,
        string? searchTerm,
        CustomerUseCases useCases,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(
            new CustomerQuery(isActive, searchTerm),
            cancellationToken);

        return TypedResults.Ok<IReadOnlyList<CustomerResponse>>(
            result.Value.Select(customer => customer.ToResponse()).ToArray());
    }

    private static async Task<Results<Ok<CustomerResponse>, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CustomerUseCases useCases,
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
            identificationNumber: request.IdentificationNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> ActivateAsync(
        Guid id,
        CustomerUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ActivateAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> DeactivateAsync(
        Guid id,
        CustomerUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.DeactivateAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }
}
