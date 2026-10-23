using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Request to create a draft sale.</summary>
public sealed record CreateSaleRequest : IValidatableObject
{
    /// <summary>Optional registered customer ID. When supplied, customer reference is snapshotted from the registered customer.</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>The customer reference or name. Required when CustomerId is not supplied.</summary>
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer reference must be between 1 and 200 characters.")]
    public string? CustomerReference { get; init; }

    /// <summary>Optional user ID. If not supplied, the authenticated user's ID is used.</summary>
    public Guid? UserId { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId.HasValue && CustomerId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "Customer ID cannot be empty when provided.",
                [nameof(CustomerId)]);
        }

        if (!CustomerId.HasValue && string.IsNullOrWhiteSpace(CustomerReference))
        {
            yield return new ValidationResult(
                "Customer reference is required.",
                [nameof(CustomerReference)]);
        }
    }
}

/// <summary>Request to update or clear the customer association on a draft sale.</summary>
public sealed record UpdateSaleCustomerRequest : IValidatableObject
{
    /// <summary>Optional registered customer ID. If null, the sale becomes a walk-in sale using CustomerReference.</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>Customer reference/name to use when CustomerId is not provided.</summary>
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer reference must be between 1 and 200 characters.")]
    public string? CustomerReference { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId.HasValue && CustomerId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "Customer ID cannot be empty when provided.",
                [nameof(CustomerId)]);
        }

        if (!CustomerId.HasValue && string.IsNullOrWhiteSpace(CustomerReference))
        {
            yield return new ValidationResult(
                "Customer reference is required when removing customer association.",
                [nameof(CustomerReference)]);
        }
    }
}

/// <summary>Request to add a detail line to a draft sale.</summary>
public sealed record AddSaleDetailRequest
{
    /// <summary>The product identifier.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Product identifier is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Product identifier must be between 1 and 100 characters.")]
    public string? ProductIdentifier { get; init; }

    /// <summary>The line item quantity.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int? Quantity { get; init; }

    /// <summary>
    /// Explicit unit price snapshot.
    /// TEMPORARY API LIMITATION: Accepted explicitly for this phase only because catalog pricing is not yet established.
    /// This must not be treated as trusted production pricing.
    /// </summary>
    [Required]
    [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "Unit price cannot be negative.")]
    public decimal? UnitPrice { get; init; }

    /// <summary>
    /// Explicit line tax rate.
    /// TEMPORARY API LIMITATION: Accepted explicitly for this phase only because tax policy engine is not yet established.
    /// </summary>
    [Required]
    [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "Tax rate cannot be negative.")]
    public decimal? TaxRate { get; init; }
}

/// <summary>Request to update an existing detail line in a draft sale.</summary>
public sealed record UpdateSaleDetailRequest
{
    /// <summary>The updated line item quantity.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int? Quantity { get; init; }

    /// <summary>
    /// Optional updated unit price snapshot.
    /// TEMPORARY API LIMITATION: Accepted explicitly for this phase only because catalog pricing is not yet established.
    /// </summary>
    [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "Unit price cannot be negative.")]
    public decimal? UnitPrice { get; init; }

    /// <summary>
    /// Optional updated line tax rate.
    /// TEMPORARY API LIMITATION: Accepted explicitly for this phase only because tax policy engine is not yet established.
    /// </summary>
    [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "Tax rate cannot be negative.")]
    public decimal? TaxRate { get; init; }
}

/// <summary>A sale detail line representation returned by the API.</summary>
public sealed record SaleDetailResponse(
    Guid Id,
    Guid SaleId,
    string ProductIdentifier,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total);

/// <summary>A sale representation returned by the API.</summary>
public sealed record SaleResponse(
    Guid Id,
    string CustomerReference,
    Guid UserId,
    string Status,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    IReadOnlyList<SaleDetailResponse> Details,
    Guid? CustomerId = null);
