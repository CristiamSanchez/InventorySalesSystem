using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    private static readonly ValueConverter<ProductIdentifier, string> ProductIdentifierConverter = new(
        identifier => identifier.Value,
        value => new ProductIdentifier(value));

    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_inventories_quantity_non_negative",
                "\"Quantity\" >= 0");
        });

        builder.HasKey(inventory => inventory.Id);
        builder.Property(inventory => inventory.Id).ValueGeneratedNever();

        builder.Property<uint>("Version").IsRowVersion();

        builder.Property(inventory => inventory.ProductIdentifier)
            .HasField("<ProductIdentifier>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(ProductIdentifierConverter)
            .HasColumnName("ProductIdentifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(inventory => inventory.ProductIdentifier)
            .IsUnique()
            .HasDatabaseName("ux_inventories_product_identifier");

        builder.Property(inventory => inventory.Quantity)
            .IsRequired();

        builder.HasMany(inventory => inventory.Movements)
            .WithOne()
            .HasForeignKey("InventoryId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(inventory => inventory.Movements)
            .HasField("_movements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
