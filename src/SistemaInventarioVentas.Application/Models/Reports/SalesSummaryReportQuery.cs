using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record SalesSummaryReportQuery
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? UserId { get; init; }

    public SaleStatus? Status { get; init; }
}
