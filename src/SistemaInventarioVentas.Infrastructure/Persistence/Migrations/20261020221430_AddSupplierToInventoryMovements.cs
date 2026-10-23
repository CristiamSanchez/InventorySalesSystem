using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInventarioVentas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierToInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "inventory_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "inventory_movements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_supplier_id",
                table: "inventory_movements",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_movements_suppliers_SupplierId",
                table: "inventory_movements",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_movements_suppliers_SupplierId",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_supplier_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "inventory_movements");
        }
    }
}
