using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInventarioVentas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.Id);
                    table.CheckConstraint("ck_sales_subtotal_non_negative", "\"Subtotal\" >= 0");
                    table.CheckConstraint("ck_sales_tax_amount_non_negative", "\"TaxAmount\" >= 0");
                    table.CheckConstraint("ck_sales_total_non_negative", "\"Total\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "sale_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_details", x => x.Id);
                    table.CheckConstraint("ck_sale_details_quantity_positive", "\"Quantity\" > 0");
                    table.CheckConstraint("ck_sale_details_subtotal_non_negative", "\"Subtotal\" >= 0");
                    table.CheckConstraint("ck_sale_details_tax_amount_non_negative", "\"TaxAmount\" >= 0");
                    table.CheckConstraint("ck_sale_details_tax_rate_non_negative", "\"TaxRate\" >= 0");
                    table.CheckConstraint("ck_sale_details_total_non_negative", "\"Total\" >= 0");
                    table.CheckConstraint("ck_sale_details_unit_price_non_negative", "\"UnitPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_sale_details_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_details_product_identifier",
                table: "sale_details",
                column: "ProductIdentifier");

            migrationBuilder.CreateIndex(
                name: "ix_sale_details_sale_id",
                table: "sale_details",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "ux_sale_details_sale_id_product_identifier",
                table: "sale_details",
                columns: new[] { "SaleId", "ProductIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_created_at_utc",
                table: "sales",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_sales_customer_reference",
                table: "sales",
                column: "CustomerReference");

            migrationBuilder.CreateIndex(
                name: "ix_sales_status",
                table: "sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_sales_user_id",
                table: "sales",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_details");

            migrationBuilder.DropTable(
                name: "sales");
        }
    }
}
