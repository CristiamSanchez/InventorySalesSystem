using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();

        builder.Property<string>("NameKey")
            .HasMaxLength(200)
            .HasComputedColumnSql("lower(\"Name\")", stored: true);
        builder.HasIndex("NameKey")
            .IsUnique()
            .HasDatabaseName("ux_categories_name_key");

        builder.HasMany(category => category.Products)
            .WithOne(product => product.Category)
            .HasForeignKey("CategoryId")
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientNoAction);

        builder.Navigation(category => category.Products)
            .HasField("_products")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
