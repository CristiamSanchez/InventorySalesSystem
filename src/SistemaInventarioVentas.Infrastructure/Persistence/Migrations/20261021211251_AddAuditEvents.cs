using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInventarioVentas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity_name_entity_id",
                table: "audit_events",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type",
                table: "audit_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_occurred_at_utc",
                table: "audit_events",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_user_id",
                table: "audit_events",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");
        }
    }
}
