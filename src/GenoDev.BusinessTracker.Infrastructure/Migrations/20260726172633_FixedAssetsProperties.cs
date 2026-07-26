using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedAssetsProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "business_tracker",
                table: "fixed_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ean",
                schema: "business_tracker",
                table: "fixed_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer_code",
                schema: "business_tracker",
                table: "fixed_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "business_tracker",
                table: "fixed_assets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit",
                schema: "business_tracker",
                table: "fixed_assets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fixed_assets_ean",
                schema: "business_tracker",
                table: "fixed_assets",
                column: "ean",
                unique: true,
                filter: "\"ean\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fixed_assets_ean",
                schema: "business_tracker",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "business_tracker",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "ean",
                schema: "business_tracker",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "manufacturer_code",
                schema: "business_tracker",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "business_tracker",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "unit",
                schema: "business_tracker",
                table: "fixed_assets");
        }
    }
}
