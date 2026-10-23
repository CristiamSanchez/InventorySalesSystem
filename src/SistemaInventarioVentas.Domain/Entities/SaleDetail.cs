using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class SaleDetail
{
    private SaleDetail()
    {
        ProductIdentifier = null!;
    }

    public SaleDetail(
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate)
        : this(Guid.NewGuid(), Guid.Empty, productIdentifier, quantity, unitPrice, taxRate)
    {
    }

    public SaleDetail(
        Guid saleId,
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate)
        : this(Guid.NewGuid(), saleId, productIdentifier, quantity, unitPrice, taxRate)
    {
    }

    public SaleDetail(
        Guid id,
        Guid saleId,
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (unitPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");
        }

        if (taxRate < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxRate),
                "Tax rate cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SaleId = saleId;
        ProductIdentifier = productIdentifier;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        TaxRate = taxRate;
        Quantity = quantity;

        CalculateAmounts();
    }

    public Guid Id { get; }

    public Guid SaleId { get; internal set; }

    public ProductIdentifier ProductIdentifier { get; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal Total { get; private set; }

    internal void IncreaseQuantity(int additionalQuantity)
    {
        if (additionalQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(additionalQuantity),
                "Quantity to add must be greater than zero.");
        }

        checked
        {
            Quantity += additionalQuantity;
        }

        CalculateAmounts();
    }

    internal void Update(int newQuantity, decimal? newUnitPrice = null, decimal? newTaxRate = null)
    {
        if (newQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newQuantity),
                "Quantity must be greater than zero.");
        }

        if (newUnitPrice.HasValue)
        {
            if (newUnitPrice.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newUnitPrice),
                    "Unit price cannot be negative.");
            }

            UnitPrice = decimal.Round(newUnitPrice.Value, 2, MidpointRounding.AwayFromZero);
        }

        if (newTaxRate.HasValue)
        {
            if (newTaxRate.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newTaxRate),
                    "Tax rate cannot be negative.");
            }

            TaxRate = newTaxRate.Value;
        }

        Quantity = newQuantity;
        CalculateAmounts();
    }

    private void CalculateAmounts()
    {
        Subtotal = decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        Total = decimal.Round(Subtotal + TaxAmount, 2, MidpointRounding.AwayFromZero);
    }
}
