using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.Application.Common;

namespace SistemaInventarioVentas.API.ErrorHandling;

internal static class ApplicationProblemMapping
{
    public static ProblemHttpResult ToProblem(ApplicationError error, string? instance = null)
    {
        var (statusCode, title) = error.Code switch
        {
            ApplicationErrorCode.InvalidInput => (StatusCodes.Status400BadRequest, "Invalid request"),
            ApplicationErrorCode.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ApplicationErrorCode.CategoryNameConflict => (StatusCodes.Status409Conflict, "Category conflict"),
            ApplicationErrorCode.ProductIdentifierConflict => (StatusCodes.Status409Conflict, "Product conflict"),
            ApplicationErrorCode.InactiveCategory => (StatusCodes.Status409Conflict, "Category state conflict"),
            ApplicationErrorCode.CategoryInUse => (StatusCodes.Status409Conflict, "Category in use"),
            ApplicationErrorCode.UserEmailConflict => (StatusCodes.Status409Conflict, "User conflict"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error.Code,
                "An application error code has no HTTP mapping.")
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Message,
            instance: instance);
    }
}
