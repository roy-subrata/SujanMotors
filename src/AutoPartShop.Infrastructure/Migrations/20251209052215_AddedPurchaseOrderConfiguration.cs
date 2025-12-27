using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedPurchaseOrderConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrder_PurchaseOrderId",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Suppliers_SupplierId",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Warehouses_WarehouseId",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLine_Parts_PartId",
                table: "PurchaseOrderLine");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLine_PurchaseOrder_PurchaseOrderId",
                table: "PurchaseOrderLine");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrderLine",
                table: "PurchaseOrderLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrder",
                table: "PurchaseOrder");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderLine",
                newName: "PurchaseOrderLines");

            migrationBuilder.RenameTable(
                name: "PurchaseOrder",
                newName: "PurchaseOrders");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderLine_PurchaseOrderId",
                table: "PurchaseOrderLines",
                newName: "IX_PurchaseOrderLines_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderLine_PartId",
                table: "PurchaseOrderLines",
                newName: "IX_PurchaseOrderLines_PartId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrder_WarehouseId",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrder_SupplierId",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_SupplierId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PurchaseOrderLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "PurchaseOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PONumber",
                table: "PurchaseOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrderLines",
                table: "PurchaseOrderLines",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_PurchaseOrderLineId",
                table: "GoodsReceiptLine",
                column: "PurchaseOrderLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipt",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLine_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLine",
                column: "PurchaseOrderLineId",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Parts_PartId",
                table: "PurchaseOrderLines",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PurchaseOrders_PurchaseOrderId",
                table: "SupplierPayments",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLine_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Parts_PartId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PurchaseOrders_PurchaseOrderId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptLine_PurchaseOrderLineId",
                table: "GoodsReceiptLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrderLines",
                table: "PurchaseOrderLines");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                newName: "PurchaseOrder");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderLines",
                newName: "PurchaseOrderLine");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrder",
                newName: "IX_PurchaseOrder_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrder",
                newName: "IX_PurchaseOrder_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLine",
                newName: "IX_PurchaseOrderLine_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderLines_PartId",
                table: "PurchaseOrderLine",
                newName: "IX_PurchaseOrderLine_PartId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrder",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "PurchaseOrder",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PONumber",
                table: "PurchaseOrder",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PurchaseOrderLine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrder",
                table: "PurchaseOrder",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrderLine",
                table: "PurchaseOrderLine",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrder_PurchaseOrderId",
                table: "GoodsReceipt",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_Suppliers_SupplierId",
                table: "PurchaseOrder",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_Warehouses_WarehouseId",
                table: "PurchaseOrder",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLine_Parts_PartId",
                table: "PurchaseOrderLine",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLine_PurchaseOrder_PurchaseOrderId",
                table: "PurchaseOrderLine",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayments",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
