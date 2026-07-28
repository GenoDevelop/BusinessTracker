using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "business_tracker",
                table: "products",
                newName: "total_amount");

            migrationBuilder.AddColumn<int>(
                name: "total_sold_amount",
                schema: "business_tracker",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_sold_amount",
                schema: "business_tracker",
                table: "products");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                schema: "business_tracker",
                table: "products",
                newName: "amount");
        }
    }
}
