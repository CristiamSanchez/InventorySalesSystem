using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    private static readonly ValueConverter<ProductIdentifier, string> ProductIdentifierConverter = new(
        identifier => identifier.Value,
        value => new ProductIdentifier(value));

    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.ToTable("sale_details", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_quantity_positive",
                "\"Quantity\" > 0");
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_unit_price_non_negative",
                "\"UnitPrice\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_tax_rate_non_negative",
                "\"TaxRate\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_subtotal_non_negative",
                "\"Subtotal\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_tax_amount_non_negative",
                "\"TaxAmount\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sale_details_total_non_negative",
                "\"Total\" >= 0");
        });

        builder.HasKey(detail => detail.Id);
        builder.Property(detail => detail.Id)
            .ValueGeneratedNever();

        builder.Property(detail => detail.SaleId)
            .IsRequired();

        builder.Property(detail => detail.ProductIdentifier)
            .HasField("<ProductIdentifier>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(ProductIdentifierConverter)
            .HasColumnName("ProductIdentifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(detail => detail.Quantity)
            .IsRequired();

        builder.Property(detail => detail.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detail => detail.TaxRate)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(detail => detail.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detail => detail.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detail => detail.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(detail => detail.SaleId)
            .HasDatabaseName("ix_sale_details_sale_id");

        builder.HasIndex(detail => detail.ProductIdentifier)
            .HasDatabaseName("ix_sale_details_product_identifier");

        builder.HasIndex(detail => new { detail.SaleId, detail.ProductIdentifier })
            .IsUnique()
            .HasDatabaseName("ux_sale_details_sale_id_product_identifier");
    }
}
