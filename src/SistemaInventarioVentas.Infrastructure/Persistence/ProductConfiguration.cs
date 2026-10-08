using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.Property<Guid>("PersistenceId").ValueGeneratedOnAdd();
        builder.HasKey("PersistenceId");
        builder.Property(product => product.Name).IsRequired();
        builder.Property(product => product.IsActive).IsRequired();

        builder.OwnsOne(product => product.Identifier, identifier =>
        {
            identifier.Property(value => value.Value)
                .HasColumnName("Identifier")
                .HasMaxLength(100)
                .IsRequired();
            identifier.HasIndex(value => value.Value)
                .IsUnique()
                .HasDatabaseName("ux_products_identifier");
        });
        builder.Navigation(product => product.Identifier).IsRequired();

    }
}
