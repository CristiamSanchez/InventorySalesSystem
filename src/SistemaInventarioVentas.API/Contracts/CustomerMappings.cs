using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.IdentificationNumber,
            customer.Email?.Value,
            customer.Phone,
            customer.Address,
            customer.IsActive,
            customer.CreatedAtUtc);
}
