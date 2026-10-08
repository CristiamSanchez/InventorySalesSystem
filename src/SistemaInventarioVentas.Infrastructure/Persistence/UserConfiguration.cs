using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly ValueConverter<EmailAddress, string> EmailConverter = new(
        email => email.Value,
        value => new EmailAddress(value));

    private static readonly ValueConverter<PasswordHash, string> PasswordHashConverter = new(
        passwordHash => passwordHash.EncodedValue,
        value => new PasswordHash(value));

    private static readonly ValueConverter<HashSet<UserRole>, string> RolesConverter = new(
        roles => JsonSerializer.Serialize(roles.Select(role => (int)role).ToArray()),
        value => JsonSerializer.Deserialize<int[]>(value)!
            .Select(role => (UserRole)role)
            .ToHashSet());

    private static readonly ValueComparer<HashSet<UserRole>> RolesComparer = new(
        (left, right) => left!.SetEquals(right!),
        roles => HashCode.Combine(
            roles.Count,
            roles.Aggregate(0, (hash, role) => hash ^ role.GetHashCode())),
        roles => roles.ToHashSet());

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Name).IsRequired();
        builder.Property(user => user.Email)
            .HasConversion(EmailConverter)
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.PasswordHash)
            .HasField("<PasswordHash>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(PasswordHashConverter)
            .HasColumnName("PasswordHash")
            .HasMaxLength(512)
            .IsRequired();

        var rolesProperty = builder.Property<HashSet<UserRole>>("_roles")
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(RolesConverter)
            .HasColumnName("Roles")
            .HasColumnType("jsonb")
            .IsRequired();
        rolesProperty.Metadata.SetValueComparer(RolesComparer);
    }
}
