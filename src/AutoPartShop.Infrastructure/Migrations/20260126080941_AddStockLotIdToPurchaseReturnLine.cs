using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLotIdToPurchaseReturnLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockLotId",
                table: "PurchaseReturnLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLine_StockLotId",
                table: "PurchaseReturnLine",
                column: "StockLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturnLine_StockLots_StockLotId",
                table: "PurchaseReturnLine",
                column: "StockLotId",
                principalTable: "StockLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturnLine_StockLots_StockLotId",
                table: "PurchaseReturnLine");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturnLine_StockLotId",
                table: "PurchaseReturnLine");

            migrationBuilder.DropColumn(
                name: "StockLotId",
                table: "PurchaseReturnLine");
        }
    }
}
