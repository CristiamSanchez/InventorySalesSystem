using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class SupplierMappings
{
    public static SupplierResponse ToResponse(this Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.ContactName,
            supplier.IdentificationNumber,
            supplier.Email?.Value,
            supplier.Phone,
            supplier.Address,
            supplier.IsActive,
            supplier.CreatedAtUtc);
}
