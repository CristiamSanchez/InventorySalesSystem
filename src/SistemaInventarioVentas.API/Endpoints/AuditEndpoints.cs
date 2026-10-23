using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class AuditEndpoints
{
    public static RouteGroupBuilder MapAudit(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", ListAsync)
            .WithName("ListAuditEvents")
            .WithSummary("List and filter audit events")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetAuditEvent")
            .WithSummary("Get an audit event by ID")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<AuditEventResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        AuditUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
    }

    private static async Task<Results<Ok<IReadOnlyList<AuditEventResponse>>, ProblemHttpResult>> ListAsync(
        string? eventType,
        string? entityName,
        string? entityId,
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        AuditUseCases useCases,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(
            new AuditQuery(eventType, entityName, entityId, userId, fromUtc, toUtc),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        return TypedResults.Ok<IReadOnlyList<AuditEventResponse>>(
            result.Value.Select(audit => audit.ToResponse()).ToArray());
    }
}
