namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record ProductSalesReportQuery
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public string? ProductIdentifier { get; init; }

    public Guid? CategoryId { get; init; }
}
