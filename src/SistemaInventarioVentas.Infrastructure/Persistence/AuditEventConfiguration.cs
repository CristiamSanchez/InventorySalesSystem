using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id)
            .HasField("<Id>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ValueGeneratedNever();

        builder.Property(audit => audit.EventType)
            .HasField("<EventType>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(audit => audit.EntityName)
            .HasField("<EntityName>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(audit => audit.EntityId)
            .HasField("<EntityId>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(audit => audit.UserId)
            .HasField("<UserId>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired(false);

        builder.Property(audit => audit.OccurredAtUtc)
            .HasField("<OccurredAtUtc>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(audit => audit.MetadataJson)
            .HasField("<MetadataJson>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Ignore(audit => audit.HasUser);

        builder.HasIndex(audit => audit.OccurredAtUtc)
            .HasDatabaseName("ix_audit_events_occurred_at_utc");

        builder.HasIndex(audit => audit.EventType)
            .HasDatabaseName("ix_audit_events_event_type");

        builder.HasIndex(audit => new { audit.EntityName, audit.EntityId })
            .HasDatabaseName("ix_audit_events_entity_name_entity_id");

        builder.HasIndex(audit => audit.UserId)
            .HasDatabaseName("ix_audit_events_user_id");
    }
}
