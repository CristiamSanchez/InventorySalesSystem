using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private static readonly ValueConverter<EmailAddress?, string?> EmailConverter = new(
        email => email != null ? email.Value : null,
        value => value != null ? new EmailAddress(value) : null);

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();

        builder.Property(customer => customer.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(customer => customer.IdentificationNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property<string>("IdentificationNumberKey")
            .HasMaxLength(50)
            .HasComputedColumnSql("upper(\"IdentificationNumber\")", stored: true);

        builder.HasIndex("IdentificationNumberKey")
            .IsUnique()
            .HasDatabaseName("ux_customers_identification_number_key");

        builder.Property(customer => customer.Email)
            .HasConversion(EmailConverter)
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property<string>("EmailKey")
            .HasMaxLength(320)
            .HasComputedColumnSql("lower(\"Email\")", stored: true);

        builder.Property(customer => customer.Phone)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(customer => customer.Address)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .IsRequired();
    }
}
