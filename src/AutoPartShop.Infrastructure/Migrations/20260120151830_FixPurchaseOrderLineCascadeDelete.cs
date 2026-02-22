using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPurchaseOrderLineCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");
        }
    }
}
