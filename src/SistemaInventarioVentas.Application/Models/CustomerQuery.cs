namespace SistemaInventarioVentas.Application.Models;

public sealed record CustomerQuery(
    bool? IsActive = null,
    string? SearchTerm = null);
