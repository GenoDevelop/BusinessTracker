using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "products",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    ean_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sale_identifier = table.Column<string>(type: "text", nullable: true),
                    payment_identifier = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_rates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_rate_name = table.Column<string>(type: "text", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_rate_value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_quantity_adjustments",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_quantity_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_quantity_adjustments_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "storage",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_costs_adjustments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    adjustment_value_gross = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_costs_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_costs_adjustments_sales_sales_id",
                        column: x => x.sales_id,
                        principalSchema: "sales",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_supplies",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buy_price_net = table.Column<decimal>(type: "numeric", nullable: false),
                    buy_price_gross = table.Column<decimal>(type: "numeric", nullable: false),
                    buy_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quantity = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    supply_status = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_supplies", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_supplies_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "storage",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_supplies_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "storage",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_sales",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_rate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<double>(type: "double precision", nullable: false),
                    sale_price_gross = table.Column<decimal>(type: "numeric", nullable: false),
                    decription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_sales_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "storage",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_sales_sales_sales_id",
                        column: x => x.sales_id,
                        principalSchema: "sales",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_sales_tax_rates_tax_rate_id",
                        column: x => x.tax_rate_id,
                        principalSchema: "sales",
                        principalTable: "tax_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_quantity_adjustments_product_id",
                schema: "storage",
                table: "product_quantity_adjustments",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_sales_product_id",
                schema: "sales",
                table: "product_sales",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_sales_sales_id",
                schema: "sales",
                table: "product_sales",
                column: "sales_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_sales_tax_rate_id",
                schema: "sales",
                table: "product_sales",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_supplies_product_id",
                schema: "storage",
                table: "product_supplies",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_supplies_supplier_id",
                schema: "storage",
                table: "product_supplies",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_ean_code",
                schema: "storage",
                table: "products",
                column: "ean_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_costs_adjustments_sales_id",
                schema: "sales",
                table: "sales_costs_adjustments",
                column: "sales_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_quantity_adjustments",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "product_sales",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "product_supplies",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "sales_costs_adjustments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "tax_rates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "products",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "sales",
                schema: "sales");
        }
    }
}
