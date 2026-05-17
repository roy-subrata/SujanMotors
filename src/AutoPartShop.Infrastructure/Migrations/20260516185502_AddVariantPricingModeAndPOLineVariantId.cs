using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantPricingModeAndPOLineVariantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "PurchaseOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellingPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingMode",
                table: "ProductVariants",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "OVERRIDE");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_VariantId",
                table: "PurchaseOrderLines",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_ProductVariants_VariantId",
                table: "PurchaseOrderLines",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_ProductVariants_VariantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLines_VariantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "PricingMode",
                table: "ProductVariants");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellingPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
