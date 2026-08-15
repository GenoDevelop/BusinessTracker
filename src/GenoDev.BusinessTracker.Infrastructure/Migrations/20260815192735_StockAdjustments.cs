using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    material_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packing_material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fixed_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<double>(type: "double precision", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustments", x => x.id);
                    table.CheckConstraint("CK_StockAdjustment_NonZeroAmount", "\"amount\" <> 0");
                    table.CheckConstraint("CK_StockAdjustment_ProductRules", "\"item_type\" <> 4 OR (NOT \"is_private\" AND \"amount\" = trunc(\"amount\"))");
                    table.CheckConstraint("CK_StockAdjustment_XOR_Type", "\n(\"item_type\" = 1 AND \"material_variant_id\" IS NOT NULL AND \"packing_material_id\" IS NULL AND \"fixed_asset_id\" IS NULL AND \"product_id\" IS NULL) OR\n(\"item_type\" = 2 AND \"material_variant_id\" IS NULL AND \"packing_material_id\" IS NOT NULL AND \"fixed_asset_id\" IS NULL AND \"product_id\" IS NULL) OR\n(\"item_type\" = 3 AND \"material_variant_id\" IS NULL AND \"packing_material_id\" IS NULL AND \"fixed_asset_id\" IS NOT NULL AND \"product_id\" IS NULL) OR\n(\"item_type\" = 4 AND \"material_variant_id\" IS NULL AND \"packing_material_id\" IS NULL AND \"fixed_asset_id\" IS NULL AND \"product_id\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_stock_adjustments_fixed_assets_fixed_asset_id",
                        column: x => x.fixed_asset_id,
                        principalSchema: "business_tracker",
                        principalTable: "fixed_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustments_material_variants_material_variant_id",
                        column: x => x.material_variant_id,
                        principalSchema: "business_tracker",
                        principalTable: "material_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustments_packing_materials_packing_material_id",
                        column: x => x.packing_material_id,
                        principalSchema: "business_tracker",
                        principalTable: "packing_materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustments_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "business_tracker",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_date",
                schema: "storage",
                table: "stock_adjustments",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_fixed_asset_id",
                schema: "storage",
                table: "stock_adjustments",
                column: "fixed_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_material_variant_id",
                schema: "storage",
                table: "stock_adjustments",
                column: "material_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_packing_material_id",
                schema: "storage",
                table: "stock_adjustments",
                column: "packing_material_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_product_id",
                schema: "storage",
                table: "stock_adjustments",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_adjustments",
                schema: "storage");
        }
    }
}
