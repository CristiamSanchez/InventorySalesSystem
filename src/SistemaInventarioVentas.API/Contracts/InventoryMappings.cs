using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class InventoryMappings
{
    public static InventoryResponse ToResponse(this Inventory inventory) =>
        new(inventory.ProductIdentifier.Value, inventory.Quantity);

    public static InventoryMovementResponse ToResponse(this InventoryMovement movement) =>
        new(
            movement.Id,
            movement.ProductIdentifier.Value,
            movement.MovementType.ToString(),
            movement.Quantity,
            movement.Reference,
            movement.OccurredAtUtc,
            movement.SupplierId,
            movement.SupplierName);
}
