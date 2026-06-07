using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DamagedGoodsHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptId",
                table: "PurchaseReturns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "GoodsReceiptLines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_GoodsReceiptId",
                table: "PurchaseReturns",
                column: "GoodsReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_GoodsReceipts_GoodsReceiptId",
                table: "PurchaseReturns",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_GoodsReceipts_GoodsReceiptId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_GoodsReceiptId",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "GoodsReceiptLines");
        }
    }
}
