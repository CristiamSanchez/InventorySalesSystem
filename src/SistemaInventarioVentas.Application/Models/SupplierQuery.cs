namespace SistemaInventarioVentas.Application.Models;

public sealed record SupplierQuery(
    bool? IsActive = null,
    string? SearchTerm = null);
