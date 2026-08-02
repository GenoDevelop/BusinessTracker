using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrdersChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "company_order",
                schema: "business_tracker",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "order_source",
                schema: "business_tracker",
                table: "orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_gross_client_price",
                schema: "business_tracker",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_gross_cost",
                schema: "business_tracker",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_net_client_price",
                schema: "business_tracker",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_net_cost",
                schema: "business_tracker",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_net_price",
                schema: "business_tracker",
                table: "order_products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_gross_price",
                schema: "business_tracker",
                table: "order_products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "client_details",
                schema: "business_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_name = table.Column<string>(type: "text", nullable: true),
                    street = table.Column<string>(type: "text", nullable: true),
                    post_code = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_details_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "business_tracker",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_details_order_id",
                schema: "business_tracker",
                table: "client_details",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_details",
                schema: "business_tracker");

            migrationBuilder.DropColumn(
                name: "company_order",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_source",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_gross_client_price",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_gross_cost",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_net_client_price",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_net_cost",
                schema: "business_tracker",
                table: "orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_net_price",
                schema: "business_tracker",
                table: "order_products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_gross_price",
                schema: "business_tracker",
                table: "order_products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
