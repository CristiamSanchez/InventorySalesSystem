using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInventarioVentas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_inventories_product_identifier",
                table: "inventories",
                column: "ProductIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_id",
                table: "inventory_movements",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_occurred_at_utc",
                table: "inventory_movements",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_identifier",
                table: "inventory_movements",
                column: "ProductIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "inventories");
        }
    }
}
