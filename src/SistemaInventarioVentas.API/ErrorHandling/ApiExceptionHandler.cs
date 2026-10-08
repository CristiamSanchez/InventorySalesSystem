using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SistemaInventarioVentas.API.ErrorHandling;

internal sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var databaseException = FindPostgresException(exception);
        if (databaseException?.SqlState is PostgresErrorCodes.UniqueViolation
            or PostgresErrorCodes.ForeignKeyViolation)
        {
            logger.LogWarning(
                "A database constraint rejected request {RequestPath} with SQL state {SqlState}.",
                httpContext.Request.Path,
                databaseException.SqlState);

            await Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Data conflict",
                    detail: "The operation conflicts with existing data.",
                    instance: httpContext.Request.Path)
                .ExecuteAsync(httpContext);
            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception while processing request {RequestPath}.",
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
