using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MaterialsAndSuppliesRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_variants",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    ean = table.Column<string>(type: "text", nullable: true),
                    manufacturer_code = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "text", nullable: true),
                    total_used_amount = table.Column<double>(type: "double precision", nullable: false),
                    company_amount = table.Column<double>(type: "double precision", nullable: false),
                    private_amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_variants_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "business_tracker",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplies",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    invoice_no = table.Column<string>(type: "text", nullable: true),
                    shipping_net_price = table.Column<double>(type: "double precision", nullable: false),
                    shipping_gross_price = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplies", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplies_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "business_tracker",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supply_items",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_supply_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packing_material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sets_amount = table.Column<int>(type: "integer", nullable: false),
                    units_in_set = table.Column<double>(type: "double precision", nullable: false),
                    set_net_price = table.Column<decimal>(type: "numeric", nullable: false),
                    set_gross_price = table.Column<decimal>(type: "numeric", nullable: false),
                    private_supply = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supply_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_supply_items_material_variants_material_variant_id",
                        column: x => x.material_variant_id,
                        principalSchema: "business_tracker",
                        principalTable: "material_variants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_supply_items_supplies_material_supply_id",
                        column: x => x.material_supply_id,
                        principalSchema: "business_tracker",
                        principalTable: "supplies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Data Migration
            migrationBuilder.Sql(@"
                INSERT INTO business_tracker.material_variants (id, material_id, name, ean, description, unit, total_used_amount, company_amount, private_amount)
                SELECT id, id, name, ean, description, unit, 0, amount, 0
                FROM business_tracker.materials;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO business_tracker.supplies (id, supplier_id, order_date, description, status, invoice_no, shipping_net_price, shipping_gross_price)
                SELECT id, supplier_id, order_date, description, status, invoice_no, 0, 0
                FROM business_tracker.material_supplies;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO business_tracker.supply_items (id, material_supply_id, material_variant_id, sets_amount, units_in_set, set_net_price, set_gross_price, private_supply)
                SELECT msi.id, msi.material_supply_id, mv.id, msi.sets_amount, msi.units_in_set, msi.set_net_price, msi.set_gross_price, false
                FROM business_tracker.material_supply_items msi
                JOIN business_tracker.material_variants mv ON mv.material_id = msi.material_id;
            ");

            migrationBuilder.DropForeignKey(
                name: "fk_production_materials_materials_material_id",
                schema: "business_tracker",
                table: "production_materials");

            migrationBuilder.DropTable(
                name: "material_supply_items",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "material_supplies",
                schema: "business_tracker");

            migrationBuilder.DropIndex(
                name: "ix_materials_ean",
                schema: "business_tracker",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "business_tracker",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "business_tracker",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "business_tracker",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "ean",
                schema: "business_tracker",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "unit",
                schema: "business_tracker",
                table: "materials");

            migrationBuilder.RenameColumn(
                name: "material_id",
                schema: "business_tracker",
                table: "production_materials",
                newName: "material_variant_id");

            migrationBuilder.RenameIndex(
                name: "ix_production_materials_material_id",
                schema: "business_tracker",
                table: "production_materials",
                newName: "ix_production_materials_material_variant_id");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "business_tracker",
                table: "materials",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "packing_materials",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    ean = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "text", nullable: true),
                    manufacturer_code = table.Column<string>(type: "text", nullable: true),
                    total_used_amount = table.Column<double>(type: "double precision", nullable: false),
                    company_amount = table.Column<double>(type: "double precision", nullable: false),
                    private_amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packing_materials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_packing_materials",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packing_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_packing_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_packing_materials_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "business_tracker",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_packing_materials_packing_materials_packing_material_",
                        column: x => x.packing_material_id,
                        principalSchema: "business_tracker",
                        principalTable: "packing_materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_variants_ean",
                schema: "business_tracker",
                table: "material_variants",
                column: "ean",
                unique: true,
                filter: "\"ean\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_material_variants_material_id",
                schema: "business_tracker",
                table: "material_variants",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_packing_materials_order_id",
                schema: "business_tracker",
                table: "order_packing_materials",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_packing_materials_packing_material_id",
                schema: "business_tracker",
                table: "order_packing_materials",
                column: "packing_material_id");

            migrationBuilder.CreateIndex(
                name: "ix_packing_materials_ean",
                schema: "business_tracker",
                table: "packing_materials",
                column: "ean",
                unique: true,
                filter: "\"ean\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_supplies_supplier_id",
                schema: "business_tracker",
                table: "supplies",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supply_items_material_supply_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "material_supply_id");

            migrationBuilder.CreateIndex(
                name: "ix_supply_items_material_variant_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "material_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_supply_items_packing_material_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "packing_material_id");

            migrationBuilder.AddForeignKey(
                name: "fk_supply_items_packing_materials_packing_material_id",
                schema: "business_tracker",
                table: "supply_items",
                column: "packing_material_id",
                principalSchema: "business_tracker",
                principalTable: "packing_materials",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_production_materials_material_variants_material_variant_id",
                schema: "business_tracker",
                table: "production_materials",
                column: "material_variant_id",
                principalSchema: "business_tracker",
                principalTable: "material_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_production_materials_material_variants_material_variant_id",
                schema: "business_tracker",
                table: "production_materials");

            migrationBuilder.DropTable(
                name: "order_packing_materials",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "supply_items",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "material_variants",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "packing_materials",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "supplies",
                schema: "business_tracker");

            migrationBuilder.RenameColumn(
                name: "material_variant_id",
                schema: "business_tracker",
                table: "production_materials",
                newName: "material_id");

            migrationBuilder.RenameIndex(
                name: "ix_production_materials_material_variant_id",
                schema: "business_tracker",
                table: "production_materials",
                newName: "ix_production_materials_material_id");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "business_tracker",
                table: "suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "business_tracker",
                table: "materials",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<double>(
                name: "amount",
                schema: "business_tracker",
                table: "materials",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "business_tracker",
                table: "materials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ean",
                schema: "business_tracker",
                table: "materials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                schema: "business_tracker",
                table: "materials",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_supplies",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    invoice_no = table.Column<string>(type: "text", nullable: true),
                    order_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_supplies", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_supplies_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "business_tracker",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_supply_items",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_supply_id = table.Column<Guid>(type: "uuid", nullable: false),
                    set_gross_price = table.Column<decimal>(type: "numeric", nullable: false),
                    set_net_price = table.Column<decimal>(type: "numeric", nullable: false),
                    sets_amount = table.Column<int>(type: "integer", nullable: false),
                    units_in_set = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_supply_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_supply_items_material_supplies_material_supply_id",
                        column: x => x.material_supply_id,
                        principalSchema: "business_tracker",
                        principalTable: "material_supplies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_material_supply_items_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "business_tracker",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_materials_ean",
                schema: "business_tracker",
                table: "materials",
                column: "ean",
                unique: true,
                filter: "\"ean\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_material_supplies_supplier_id",
                schema: "business_tracker",
                table: "material_supplies",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_supply_items_material_id",
                schema: "business_tracker",
                table: "material_supply_items",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_supply_items_material_supply_id",
                schema: "business_tracker",
                table: "material_supply_items",
                column: "material_supply_id");

            migrationBuilder.AddForeignKey(
                name: "fk_production_materials_materials_material_id",
                schema: "business_tracker",
                table: "production_materials",
                column: "material_id",
                principalSchema: "business_tracker",
                principalTable: "materials",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
