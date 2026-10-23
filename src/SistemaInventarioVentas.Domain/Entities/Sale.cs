using System.Collections.ObjectModel;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class Sale
{
    private readonly List<SaleDetail> _details = [];
    private readonly ReadOnlyCollection<SaleDetail> _detailsView;

    private Sale()
    {
        CustomerReference = string.Empty;
        _detailsView = _details.AsReadOnly();
    }

    public Sale(
        string customerReference,
        Guid userId,
        DateTimeOffset? createdAtUtc = null,
        Guid? customerId = null)
        : this(Guid.NewGuid(), customerReference, userId, createdAtUtc, customerId)
    {
    }

    public Sale(
        Guid id,
        string customerReference,
        Guid userId,
        DateTimeOffset? createdAtUtc = null,
        Guid? customerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (customerId.HasValue && customerId.Value == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty when provided.", nameof(customerId));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CustomerId = customerId;
        CustomerReference = customerReference.Trim();
        UserId = userId;
        Status = SaleStatus.Draft;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        _detailsView = _details.AsReadOnly();
        RecalculateTotals();
    }

    public Guid Id { get; }

    public Guid? CustomerId { get; private set; }

    public string CustomerReference { get; private set; }

    public Guid UserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public SaleStatus Status { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal Total { get; private set; }

    public IReadOnlyCollection<SaleDetail> Details => _detailsView;

    public void AssignCustomer(Guid customerId, string customerName)
    {
        EnsureIsDraft();

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        CustomerId = customerId;
        CustomerReference = customerName.Trim();
    }

    public void ClearCustomer(string customerReference)
    {
        EnsureIsDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);

        CustomerId = null;
        CustomerReference = customerReference.Trim();
    }

    public void UpdateCustomerReference(string customerReference)
    {
        EnsureIsDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);
        CustomerReference = customerReference.Trim();
    }

    public SaleDetail AddDetail(
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        EnsureIsDraft();
        ArgumentNullException.ThrowIfNull(productIdentifier);

        var existingDetail = _details.FirstOrDefault(d => d.ProductIdentifier == productIdentifier);
        if (existingDetail is not null)
        {
            existingDetail.IncreaseQuantity(quantity);
            RecalculateTotals();
            return existingDetail;
        }

        var detail = new SaleDetail(Id, productIdentifier, quantity, unitPrice, taxRate);
        _details.Add(detail);
        RecalculateTotals();
        return detail;
    }

    public void UpdateDetail(
        ProductIdentifier productIdentifier,
        int newQuantity,
        decimal? newUnitPrice = null,
        decimal? newTaxRate = null)
    {
        EnsureIsDraft();
        ArgumentNullException.ThrowIfNull(productIdentifier);

        var detail = _details.FirstOrDefault(d => d.ProductIdentifier == productIdentifier)
            ?? throw new InvalidOperationException(
                $"Product '{productIdentifier.Value}' was not found in sale details.");

        detail.Update(newQuantity, newUnitPrice, newTaxRate);
        RecalculateTotals();
    }

    public void UpdateDetail(
        Guid detailId,
        int newQuantity,
        decimal? newUnitPrice = null,
        decimal? newTaxRate = null)
    {
        EnsureIsDraft();

        var detail = _details.FirstOrDefault(d => d.Id == detailId)
            ?? throw new InvalidOperationException(
                $"Sale detail with ID '{detailId}' was not found.");

        detail.Update(newQuantity, newUnitPrice, newTaxRate);
        RecalculateTotals();
    }

    public void UpdateDetailQuantity(ProductIdentifier productIdentifier, int newQuantity)
    {
        UpdateDetail(productIdentifier, newQuantity);
    }

    public bool RemoveDetail(ProductIdentifier productIdentifier)
    {
        EnsureIsDraft();
        ArgumentNullException.ThrowIfNull(productIdentifier);

        var detail = _details.FirstOrDefault(d => d.ProductIdentifier == productIdentifier);
        if (detail is null)
        {
            return false;
        }

        _details.Remove(detail);
        RecalculateTotals();
        return true;
    }

    public bool RemoveDetail(Guid detailId)
    {
        EnsureIsDraft();

        var detail = _details.FirstOrDefault(d => d.Id == detailId);
        if (detail is null)
        {
            return false;
        }

        _details.Remove(detail);
        RecalculateTotals();
        return true;
    }

    public void Confirm(DateTimeOffset? confirmedAtUtc = null)
    {
        if (Status != SaleStatus.Draft)
        {
            throw new InvalidOperationException(
                $"A sale in '{Status}' status cannot be confirmed.");
        }

        if (_details.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale must contain at least one detail line before it can be confirmed.");
        }

        Status = SaleStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
        {
            throw new InvalidOperationException("Sale is already cancelled.");
        }

        Status = SaleStatus.Cancelled;
    }

    private void EnsureIsDraft()
    {
        if (Status != SaleStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot modify details of a sale in '{Status}' status.");
        }
    }

    private void RecalculateTotals()
    {
        decimal subtotal = 0m;
        decimal taxAmount = 0m;

        foreach (var detail in _details)
        {
            subtotal += detail.Subtotal;
            taxAmount += detail.TaxAmount;
        }

        Subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        Total = decimal.Round(Subtotal + TaxAmount, 2, MidpointRounding.AwayFromZero);
    }
}
