namespace SistemaInventarioVentas.Application.Models;

public sealed record ProductQuery(
    Guid? CategoryId = null,
    bool? IsActive = null,
    string? SearchTerm = null);
