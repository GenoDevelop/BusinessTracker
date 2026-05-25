using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTaxRateDeleteBehaviorToRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_sales_tax_rates_tax_rate_id",
                schema: "sales",
                table: "product_sales");

            migrationBuilder.AddForeignKey(
                name: "fk_product_sales_tax_rates_tax_rate_id",
                schema: "sales",
                table: "product_sales",
                column: "tax_rate_id",
                principalSchema: "sales",
                principalTable: "tax_rates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_sales_tax_rates_tax_rate_id",
                schema: "sales",
                table: "product_sales");

            migrationBuilder.AddForeignKey(
                name: "fk_product_sales_tax_rates_tax_rate_id",
                schema: "sales",
                table: "product_sales",
                column: "tax_rate_id",
                principalSchema: "sales",
                principalTable: "tax_rates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
