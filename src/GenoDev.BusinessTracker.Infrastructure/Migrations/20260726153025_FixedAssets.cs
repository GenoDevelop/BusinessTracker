using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_amount",
                schema: "business_tracker",
                table: "product_recipe_materials");

            migrationBuilder.RenameColumn(
                name: "private_amount",
                schema: "business_tracker",
                table: "packing_materials",
                newName: "total_private_amount");

            migrationBuilder.RenameColumn(
                name: "company_amount",
                schema: "business_tracker",
                table: "packing_materials",
                newName: "total_company_amount");

            migrationBuilder.RenameColumn(
                name: "private_amount",
                schema: "business_tracker",
                table: "material_variants",
                newName: "total_private_amount");

            migrationBuilder.RenameColumn(
                name: "company_amount",
                schema: "business_tracker",
                table: "material_variants",
                newName: "total_company_amount");

            migrationBuilder.AddColumn<Guid>(
                name: "fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "item_type",
                schema: "business_tracker",
                table: "supply_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE business_tracker.supply_items SET item_type = 1;");

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_net_price",
                schema: "business_tracker",
                table: "supplies",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_gross_price",
                schema: "business_tracker",
                table: "supplies",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "business_tracker",
                table: "suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fixed_assets",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_company_amount = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    total_private_amount = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixed_assets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supply_items_fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "fixed_asset_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplyItem_XOR_Type",
                schema: "business_tracker",
                table: "supply_items",
                sql: "(\"item_type\" = 1 AND \"material_variant_id\" IS NOT NULL AND \"packing_material_id\" IS NULL AND \"fixed_asset_id\" IS NULL) OR \r\n              (\"item_type\" = 2 AND \"material_variant_id\" IS NULL AND \"packing_material_id\" IS NOT NULL AND \"fixed_asset_id\" IS NULL) OR \r\n              (\"item_type\" = 3 AND \"material_variant_id\" IS NULL AND \"packing_material_id\" IS NULL AND \"fixed_asset_id\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_supply_items_fixed_assets_fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "fixed_asset_id",
                principalSchema: "business_tracker",
                principalTable: "fixed_assets",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_supply_items_fixed_assets_fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items");

            migrationBuilder.DropTable(
                name: "fixed_assets",
                schema: "business_tracker");

            migrationBuilder.DropIndex(
                name: "ix_supply_items_fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplyItem_XOR_Type",
                schema: "business_tracker",
                table: "supply_items");

            migrationBuilder.DropColumn(
                name: "fixed_asset_id",
                schema: "business_tracker",
                table: "supply_items");

            migrationBuilder.DropColumn(
                name: "item_type",
                schema: "business_tracker",
                table: "supply_items");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "business_tracker",
                table: "suppliers");

            migrationBuilder.RenameColumn(
                name: "total_private_amount",
                schema: "business_tracker",
                table: "packing_materials",
                newName: "private_amount");

            migrationBuilder.RenameColumn(
                name: "total_company_amount",
                schema: "business_tracker",
                table: "packing_materials",
                newName: "company_amount");

            migrationBuilder.RenameColumn(
                name: "total_private_amount",
                schema: "business_tracker",
                table: "material_variants",
                newName: "private_amount");

            migrationBuilder.RenameColumn(
                name: "total_company_amount",
                schema: "business_tracker",
                table: "material_variants",
                newName: "company_amount");

            migrationBuilder.AlterColumn<double>(
                name: "shipping_net_price",
                schema: "business_tracker",
                table: "supplies",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<double>(
                name: "shipping_gross_price",
                schema: "business_tracker",
                table: "supplies",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<double>(
                name: "required_amount",
                schema: "business_tracker",
                table: "product_recipe_materials",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
