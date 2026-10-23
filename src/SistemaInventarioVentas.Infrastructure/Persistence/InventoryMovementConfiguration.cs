using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    private static readonly ValueConverter<ProductIdentifier, string> ProductIdentifierConverter = new(
        identifier => identifier.Value,
        value => new ProductIdentifier(value));

    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever();

        builder.Property(movement => movement.ProductIdentifier)
            .HasField("<ProductIdentifier>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(ProductIdentifierConverter)
            .HasColumnName("ProductIdentifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(movement => movement.MovementType)
            .HasField("<MovementType>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(movement => movement.Quantity)
            .HasField("<Quantity>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(movement => movement.Reference)
            .HasField("<Reference>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(movement => movement.OccurredAtUtc)
            .HasField("<OccurredAtUtc>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(movement => movement.SupplierId)
            .HasField("<SupplierId>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(movement => movement.SupplierName)
            .HasField("<SupplierName>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(200);

        builder.HasIndex("InventoryId")
            .HasDatabaseName("ix_inventory_movements_inventory_id");

        builder.HasIndex(movement => movement.ProductIdentifier)
            .HasDatabaseName("ix_inventory_movements_product_identifier");

        builder.HasIndex(movement => movement.OccurredAtUtc)
            .HasDatabaseName("ix_inventory_movements_occurred_at_utc");

        builder.HasIndex(movement => movement.SupplierId)
            .HasDatabaseName("ix_inventory_movements_supplier_id");

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(movement => movement.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
