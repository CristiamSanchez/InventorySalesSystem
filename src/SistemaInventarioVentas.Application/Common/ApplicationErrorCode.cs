namespace SistemaInventarioVentas.Application.Common;

public enum ApplicationErrorCode
{
    InvalidInput,
    NotFound,
    CategoryNameConflict,
    ProductIdentifierConflict,
    InactiveCategory,
    CategoryInUse,
    UserEmailConflict,
    InsufficientStock,
    ConcurrencyConflict,
    CustomerIdentificationConflict,
    SupplierIdentificationConflict
}
