using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Request to create a category.</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>The category name.</summary>
    [Required]
    public string? Name { get; init; }
}

/// <summary>Request to update a category.</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>The category name.</summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>Whether the category is available for new products.</summary>
    [Required]
    public bool? IsActive { get; init; }
}

/// <summary>Request to create a product.</summary>
public sealed record CreateProductRequest
{
    /// <summary>The unique product identifier.</summary>
    [Required]
    public string? Identifier { get; init; }

    /// <summary>The product name.</summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>The category to which the product belongs.</summary>
    [Required]
    public Guid? CategoryId { get; init; }
}

/// <summary>Request to update a product.</summary>
public sealed record UpdateProductRequest
{
    /// <summary>The product name.</summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>The product's category.</summary>
    [Required]
    public Guid? CategoryId { get; init; }

    /// <summary>Whether the product is active.</summary>
    [Required]
    public bool? IsActive { get; init; }
}

/// <summary>A category representation returned by the API.</summary>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<ProductSummaryResponse> Products);

/// <summary>A product summary nested in a category response.</summary>
public sealed record ProductSummaryResponse(
    string Identifier,
    string Name,
    bool IsActive);

/// <summary>A product representation returned by the API.</summary>
public sealed record ProductResponse(
    string Identifier,
    string Name,
    Guid CategoryId,
    string CategoryName,
    bool IsActive);
