using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    private static readonly ValueConverter<EmailAddress?, string?> EmailConverter = new(
        email => email != null ? email.Value : null,
        value => value != null ? new EmailAddress(value) : null);

    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Id).ValueGeneratedNever();

        builder.Property(supplier => supplier.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(supplier => supplier.ContactName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(supplier => supplier.IdentificationNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property<string>("IdentificationNumberKey")
            .HasMaxLength(50)
            .HasComputedColumnSql("upper(\"IdentificationNumber\")", stored: true);

        builder.HasIndex("IdentificationNumberKey")
            .IsUnique()
            .HasDatabaseName("ux_suppliers_identification_number_key");

        builder.Property(supplier => supplier.Email)
            .HasConversion(EmailConverter)
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property<string>("EmailKey")
            .HasMaxLength(320)
            .HasComputedColumnSql("lower(\"Email\")", stored: true);

        builder.Property(supplier => supplier.Phone)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(supplier => supplier.Address)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(supplier => supplier.IsActive)
            .IsRequired();

        builder.Property(supplier => supplier.CreatedAtUtc)
            .IsRequired();
    }
}
