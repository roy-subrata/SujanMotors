using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantIdToSalesOrderLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "SalesOrderLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLine_ProductVariantId",
                table: "SalesOrderLine",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLine_ProductVariants_ProductVariantId",
                table: "SalesOrderLine",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLine_ProductVariants_ProductVariantId",
                table: "SalesOrderLine");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderLine_ProductVariantId",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "SalesOrderLine");
        }
    }
}
