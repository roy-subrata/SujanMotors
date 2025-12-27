using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedGoodReciptConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_Warehouses_WarehouseId",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLine_GoodsReceipt_GoodsReceiptId",
                table: "GoodsReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLine_Parts_PartId",
                table: "GoodsReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLine_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodsReceiptLine",
                table: "GoodsReceiptLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodsReceipt",
                table: "GoodsReceipt");

            migrationBuilder.RenameTable(
                name: "GoodsReceiptLine",
                newName: "GoodsReceiptLines");

            migrationBuilder.RenameTable(
                name: "GoodsReceipt",
                newName: "GoodsReceipts");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLine_PurchaseOrderLineId",
                table: "GoodsReceiptLines",
                newName: "IX_GoodsReceiptLines_PurchaseOrderLineId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLine_PartId",
                table: "GoodsReceiptLines",
                newName: "IX_GoodsReceiptLines_PartId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLine_GoodsReceiptId",
                table: "GoodsReceiptLines",
                newName: "IX_GoodsReceiptLines_GoodsReceiptId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceipt_WarehouseId",
                table: "GoodsReceipts",
                newName: "IX_GoodsReceipts_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceipt_PurchaseOrderId",
                table: "GoodsReceipts",
                newName: "IX_GoodsReceipts_PurchaseOrderId");

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumbers",
                table: "GoodsReceiptLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GoodsReceiptLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "GoodsReceiptLines",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "GoodsReceiptLines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GoodsReceipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GoodsReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GRNNumber",
                table: "GoodsReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DriverName",
                table: "GoodsReceipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryReference",
                table: "GoodsReceipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryNotes",
                table: "GoodsReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CarrierName",
                table: "GoodsReceipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodsReceiptLines",
                table: "GoodsReceiptLines",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodsReceipts",
                table: "GoodsReceipts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLines_Parts_PartId",
                table: "GoodsReceiptLines",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderLineId",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                table: "GoodsReceipts",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLines_Parts_PartId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                table: "GoodsReceipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodsReceipts",
                table: "GoodsReceipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodsReceiptLines",
                table: "GoodsReceiptLines");

            migrationBuilder.RenameTable(
                name: "GoodsReceipts",
                newName: "GoodsReceipt");

            migrationBuilder.RenameTable(
                name: "GoodsReceiptLines",
                newName: "GoodsReceiptLine");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceipts_WarehouseId",
                table: "GoodsReceipt",
                newName: "IX_GoodsReceipt_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipt",
                newName: "IX_GoodsReceipt_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderLineId",
                table: "GoodsReceiptLine",
                newName: "IX_GoodsReceiptLine_PurchaseOrderLineId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLines_PartId",
                table: "GoodsReceiptLine",
                newName: "IX_GoodsReceiptLine_PartId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                table: "GoodsReceiptLine",
                newName: "IX_GoodsReceiptLine_GoodsReceiptId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "GRNNumber",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DriverName",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryReference",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryNotes",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CarrierName",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumbers",
                table: "GoodsReceiptLine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GoodsReceiptLine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "GoodsReceiptLine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "GoodsReceiptLine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodsReceipt",
                table: "GoodsReceipt",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodsReceiptLine",
                table: "GoodsReceiptLine",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipt",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_Warehouses_WarehouseId",
                table: "GoodsReceipt",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLine_GoodsReceipt_GoodsReceiptId",
                table: "GoodsReceiptLine",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLine_Parts_PartId",
                table: "GoodsReceiptLine",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLine_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptLine",
                column: "PurchaseOrderLineId",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
