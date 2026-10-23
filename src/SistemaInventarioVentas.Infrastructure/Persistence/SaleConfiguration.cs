using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_sales_subtotal_non_negative",
                "\"Subtotal\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sales_tax_amount_non_negative",
                "\"TaxAmount\" >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_sales_total_non_negative",
                "\"Total\" >= 0");
        });

        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id)
            .ValueGeneratedNever();

        builder.Property(sale => sale.CustomerId);

        builder.Property(sale => sale.CustomerReference)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sale => sale.UserId)
            .HasField("<UserId>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(sale => sale.CreatedAtUtc)
            .HasField("<CreatedAtUtc>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(sale => sale.ConfirmedAtUtc);

        builder.Property(sale => sale.Status)
            .IsRequired();

        builder.Property(sale => sale.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sale => sale.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sale => sale.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(sale => sale.UserId)
            .HasDatabaseName("ix_sales_user_id");

        builder.HasIndex(sale => sale.CreatedAtUtc)
            .HasDatabaseName("ix_sales_created_at_utc");

        builder.HasIndex(sale => sale.Status)
            .HasDatabaseName("ix_sales_status");

        builder.HasIndex(sale => sale.CustomerReference)
            .HasDatabaseName("ix_sales_customer_reference");

        builder.HasIndex(sale => sale.CustomerId)
            .HasDatabaseName("ix_sales_customer_id");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(sale => sale.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sale => sale.Details)
            .WithOne()
            .HasForeignKey(detail => detail.SaleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sale => sale.Details)
            .HasField("_details")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
