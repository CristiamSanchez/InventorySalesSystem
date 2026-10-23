using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Request to create a customer.</summary>
public sealed record CreateCustomerRequest
{
    /// <summary>The customer's legal or commercial name.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Customer name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer name must be between 1 and 200 characters.")]
    public string? Name { get; init; }

    /// <summary>Optional fiscal or national identification number.</summary>
    [StringLength(50, ErrorMessage = "Identification number cannot exceed 50 characters.")]
    public string? IdentificationNumber { get; init; }

    /// <summary>Optional contact email address.</summary>
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string? Email { get; init; }

    /// <summary>Optional contact phone number.</summary>
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; init; }

    /// <summary>Optional physical address.</summary>
    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; init; }
}

/// <summary>Request to update an existing customer's details.</summary>
public sealed record UpdateCustomerRequest
{
    /// <summary>The customer's legal or commercial name.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Customer name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer name must be between 1 and 200 characters.")]
    public string? Name { get; init; }

    /// <summary>Optional fiscal or national identification number.</summary>
    [StringLength(50, ErrorMessage = "Identification number cannot exceed 50 characters.")]
    public string? IdentificationNumber { get; init; }

    /// <summary>Optional contact email address.</summary>
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string? Email { get; init; }

    /// <summary>Optional contact phone number.</summary>
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; init; }

    /// <summary>Optional physical address.</summary>
    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; init; }
}

/// <summary>A customer representation returned by the API.</summary>
public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string? IdentificationNumber,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
