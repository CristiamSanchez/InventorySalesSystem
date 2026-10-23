using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record SalesSummaryReport
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public int TotalTransactions { get; init; }

    public int ConfirmedTransactions { get; init; }

    public int DraftTransactions { get; init; }

    public int CancelledTransactions { get; init; }

    public decimal TotalSubtotal { get; init; }

    public decimal TotalTax { get; init; }

    public decimal TotalRevenue { get; init; }

    public IReadOnlyList<SalesSummarySaleItem> Sales { get; init; } = [];
}

public sealed record SalesSummarySaleItem
{
    public Guid Id { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ConfirmedAtUtc { get; init; }

    public SaleStatus Status { get; init; }

    public Guid? CustomerId { get; init; }

    public string CustomerReference { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public int TotalUnits { get; init; }

    public int LineCount { get; init; }

    public decimal Subtotal { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal Total { get; init; }
}
