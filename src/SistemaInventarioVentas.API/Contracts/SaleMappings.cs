using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class SaleMappings
{
    public static SaleDetailResponse ToResponse(this SaleDetail detail) =>
        new(
            detail.Id,
            detail.SaleId,
            detail.ProductIdentifier.Value,
            detail.Quantity,
            detail.UnitPrice,
            detail.TaxRate,
            detail.Subtotal,
            detail.TaxAmount,
            detail.Total);

    public static SaleResponse ToResponse(this Sale sale) =>
        new(
            sale.Id,
            sale.CustomerReference,
            sale.UserId,
            sale.Status.ToString(),
            sale.Subtotal,
            sale.TaxAmount,
            sale.Total,
            sale.CreatedAtUtc,
            sale.ConfirmedAtUtc,
            sale.Details.Select(ToResponse).ToList(),
            sale.CustomerId);
}
