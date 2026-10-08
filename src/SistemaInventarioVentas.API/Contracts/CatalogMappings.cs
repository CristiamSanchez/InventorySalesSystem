using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class CatalogMappings
{
    public static CategoryResponse ToResponse(this Category category) =>
        new(
            category.Id,
            category.Name,
            category.IsActive,
            category.Products
                .Select(product => new ProductSummaryResponse(
                    product.Identifier.Value,
                    product.Name,
                    product.IsActive))
                .ToArray());

    public static ProductResponse ToResponse(this Product product) =>
        new(
            product.Identifier.Value,
            product.Name,
            product.Category.Id,
            product.Category.Name,
            product.IsActive);
}
