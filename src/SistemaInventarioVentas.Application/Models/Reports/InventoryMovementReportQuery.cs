using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record InventoryMovementReportQuery
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public string? ProductIdentifier { get; init; }

    public MovementType? MovementType { get; init; }

    public Guid? SupplierId { get; init; }
}
