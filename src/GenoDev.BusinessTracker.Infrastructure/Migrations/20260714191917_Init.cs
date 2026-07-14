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
                name: "business_tracker");

            migrationBuilder.CreateTable(
                name: "materials",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    ean = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order_identifier = table.Column<string>(type: "text", nullable: true),
                    payment_identifier = table.Column<string>(type: "text", nullable: true),
                    tracking_number = table.Column<string>(type: "text", nullable: true),
                    carrier = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    nip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_products",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_amount = table.Column<int>(type: "integer", nullable: false),
                    assigned_amount = table.Column<int>(type: "integer", nullable: false),
                    unit_net_price = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_gross_price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_products_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "business_tracker",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_products_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "business_tracker",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_recipes",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_recipes", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_recipes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "business_tracker",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "business_tracker",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_supplies",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                name: "product_recipe_materials",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_recipe_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_recipe_materials_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "business_tracker",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_recipe_materials_product_recipes_product_recipe_id",
                        column: x => x.product_recipe_id,
                        principalSchema: "business_tracker",
                        principalTable: "product_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_materials",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    used_amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_materials_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "business_tracker",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_materials_productions_production_id",
                        column: x => x.production_id,
                        principalSchema: "business_tracker",
                        principalTable: "production",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_supply_items",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_supply_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sets_amount = table.Column<int>(type: "integer", nullable: false),
                    units_in_set = table.Column<double>(type: "double precision", nullable: false),
                    set_net_price = table.Column<decimal>(type: "numeric", nullable: false),
                    set_gross_price = table.Column<decimal>(type: "numeric", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_materials_ean",
                schema: "business_tracker",
                table: "materials",
                column: "ean",
                unique: true,
                filter: "\"ean\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_products_order_id",
                schema: "business_tracker",
                table: "order_products",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_products_product_id",
                schema: "business_tracker",
                table: "order_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_recipe_materials_material_id",
                schema: "business_tracker",
                table: "product_recipe_materials",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_recipe_materials_product_recipe_id",
                schema: "business_tracker",
                table: "product_recipe_materials",
                column: "product_recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_recipes_product_id",
                schema: "business_tracker",
                table: "product_recipes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_product_id",
                schema: "business_tracker",
                table: "production",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_materials_material_id",
                schema: "business_tracker",
                table: "production_materials",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_materials_production_id",
                schema: "business_tracker",
                table: "production_materials",
                column: "production_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_identifier",
                schema: "business_tracker",
                table: "products",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_nip",
                schema: "business_tracker",
                table: "suppliers",
                column: "nip",
                unique: true,
                filter: "\"nip\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_supply_items",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "order_products",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "product_recipe_materials",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "production_materials",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "material_supplies",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "product_recipes",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "materials",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "production",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "business_tracker");

            migrationBuilder.DropTable(
                name: "products",
                schema: "business_tracker");
        }
    }
}
