using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToProductQuantityAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "storage",
                table: "product_quantity_adjustments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "storage",
                table: "product_quantity_adjustments");
        }
    }
}
