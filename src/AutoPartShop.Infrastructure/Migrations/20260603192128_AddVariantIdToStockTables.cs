using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantIdToStockTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "StockLots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "StockLevels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "GoodsReceiptLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_PartId_VariantId_WarehouseId",
                table: "StockLots",
                columns: new[] { "PartId", "VariantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_VariantId",
                table: "StockLots",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_PartId_VariantId_WarehouseId",
                table: "StockLevels",
                columns: new[] { "PartId", "VariantId", "WarehouseId" },
                unique: true,
                filter: "[VariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels",
                columns: new[] { "PartId", "WarehouseId" },
                unique: true,
                filter: "[VariantId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_VariantId",
                table: "StockLevels",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_VariantId",
                table: "GoodsReceiptLines",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLines_ProductVariants_VariantId",
                table: "GoodsReceiptLines",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_ProductVariants_VariantId",
                table: "StockLevels",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLots_ProductVariants_VariantId",
                table: "StockLots",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLines_ProductVariants_VariantId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_ProductVariants_VariantId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLots_ProductVariants_VariantId",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_PartId_VariantId_WarehouseId",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_VariantId",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_PartId_VariantId_WarehouseId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_VariantId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptLines_VariantId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "GoodsReceiptLines");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels",
                columns: new[] { "PartId", "WarehouseId" },
                unique: true);
        }
    }
}
