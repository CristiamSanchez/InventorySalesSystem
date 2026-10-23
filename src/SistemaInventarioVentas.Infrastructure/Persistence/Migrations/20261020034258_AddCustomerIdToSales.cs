using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInventarioVentas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_customer_id",
                table: "sales",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_customers_CustomerId",
                table: "sales",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sales_customers_CustomerId",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_customer_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "sales");
        }
    }
}
