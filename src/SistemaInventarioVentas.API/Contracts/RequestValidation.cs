using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

internal static class RequestValidation
{
    public static IDictionary<string, string[]>? Validate<T>(T request)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request!);
        if (Validator.TryValidateObject(request!, validationContext, validationResults, validateAllProperties: true))
        {
            return null;
        }

        return validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Error: result.ErrorMessage ?? "The request is invalid.")))
            .GroupBy(item => item.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Error).Distinct().ToArray());
    }
}
