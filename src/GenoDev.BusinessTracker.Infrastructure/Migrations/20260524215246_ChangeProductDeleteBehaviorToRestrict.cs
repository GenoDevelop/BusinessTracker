using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProductDeleteBehaviorToRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_quantity_adjustments_products_product_id",
                schema: "storage",
                table: "product_quantity_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_sales_products_product_id",
                schema: "sales",
                table: "product_sales");

            migrationBuilder.DropForeignKey(
                name: "fk_product_supplies_products_product_id",
                schema: "storage",
                table: "product_supplies");

            migrationBuilder.AddForeignKey(
                name: "fk_product_quantity_adjustments_products_product_id",
                schema: "storage",
                table: "product_quantity_adjustments",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_sales_products_product_id",
                schema: "sales",
                table: "product_sales",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_supplies_products_product_id",
                schema: "storage",
                table: "product_supplies",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_quantity_adjustments_products_product_id",
                schema: "storage",
                table: "product_quantity_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_sales_products_product_id",
                schema: "sales",
                table: "product_sales");

            migrationBuilder.DropForeignKey(
                name: "fk_product_supplies_products_product_id",
                schema: "storage",
                table: "product_supplies");

            migrationBuilder.AddForeignKey(
                name: "fk_product_quantity_adjustments_products_product_id",
                schema: "storage",
                table: "product_quantity_adjustments",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_sales_products_product_id",
                schema: "sales",
                table: "product_sales",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_supplies_products_product_id",
                schema: "storage",
                table: "product_supplies",
                column: "product_id",
                principalSchema: "storage",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
