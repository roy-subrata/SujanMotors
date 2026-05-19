using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiUnitSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturnLine_Parts_PartId",
                table: "SalesReturnLine");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturnLine_SalesReturns_SalesReturnId",
                table: "SalesReturnLine");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Parts_PartId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Warehouses_WarehouseId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockLevels_StockLevelId",
                table: "StockMovements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesReturnLine",
                table: "SalesReturnLine");

            migrationBuilder.RenameTable(
                name: "SalesReturnLine",
                newName: "SalesReturnLines");

            migrationBuilder.RenameIndex(
                name: "IX_SalesReturnLine_SalesReturnId",
                table: "SalesReturnLines",
                newName: "IX_SalesReturnLines_SalesReturnId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesReturnLine_PartId",
                table: "SalesReturnLines",
                newName: "IX_SalesReturnLines_PartId");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "StockMovements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "StockMovements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "StockMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MovementType",
                table: "StockMovements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "StockMovements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "QuantityInBaseUnit",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPriceInBaseUnit",
                table: "StockLots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuantityAvailableInBaseUnit",
                table: "StockLots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityReceivedInBaseUnit",
                table: "StockLots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "StockLots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostAtMovementInBaseUnit",
                table: "StockLotMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuantityInBaseUnit",
                table: "StockLotMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "StockLotMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityOnHandInBaseUnit",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityReservedInBaseUnit",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "StockLevels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUnitId",
                table: "Parts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RejectedQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostInBaseUnit",
                table: "GoodsReceiptLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuantityInBaseUnit",
                table: "SalesReturnLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "SalesReturnLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceInBaseUnit",
                table: "SalesReturnLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesReturnLines",
                table: "SalesReturnLines",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType",
                table: "StockMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseOrderLineId",
                table: "StockMovements",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SalesOrderLineId",
                table: "StockMovements",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_UnitId",
                table: "StockMovements",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_UnitId",
                table: "StockLots",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLotMovements_UnitId",
                table: "StockLotMovements",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels",
                columns: new[] { "PartId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_UnitId",
                table: "StockLevels",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_BaseUnitId",
                table: "Parts",
                column: "BaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesOrderLineId",
                table: "SalesReturnLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_UnitId",
                table: "SalesReturnLines",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Part_BaseUnit",
                table: "Parts",
                column: "BaseUnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Part_Unit",
                table: "Parts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturnLines_Parts_PartId",
                table: "SalesReturnLines",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturnLines_SalesReturns_SalesReturnId",
                table: "SalesReturnLines",
                column: "SalesReturnId",
                principalTable: "SalesReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturnLines_Units_UnitId",
                table: "SalesReturnLines",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Parts_PartId",
                table: "StockLevels",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Units_UnitId",
                table: "StockLevels",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Warehouses_WarehouseId",
                table: "StockLevels",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLotMovements_Units_UnitId",
                table: "StockLotMovements",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLots_Units_UnitId",
                table: "StockLots",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockLevels_StockLevelId",
                table: "StockMovements",
                column: "StockLevelId",
                principalTable: "StockLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Units_UnitId",
                table: "StockMovements",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Part_BaseUnit",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_Part_Unit",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturnLines_Parts_PartId",
                table: "SalesReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturnLines_SalesReturns_SalesReturnId",
                table: "SalesReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturnLines_Units_UnitId",
                table: "SalesReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Parts_PartId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Units_UnitId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Warehouses_WarehouseId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLotMovements_Units_UnitId",
                table: "StockLotMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLots_Units_UnitId",
                table: "StockLots");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockLevels_StockLevelId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Units_UnitId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementType",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PurchaseOrderLineId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SalesOrderLineId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_UnitId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockLots_UnitId",
                table: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_StockLotMovements_UnitId",
                table: "StockLotMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_PartId_WarehouseId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_UnitId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_Parts_BaseUnitId",
                table: "Parts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesReturnLines",
                table: "SalesReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturnLines_SalesOrderLineId",
                table: "SalesReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturnLines_UnitId",
                table: "SalesReturnLines");

            migrationBuilder.DropColumn(
                name: "QuantityInBaseUnit",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CostPriceInBaseUnit",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "QuantityAvailableInBaseUnit",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "QuantityReceivedInBaseUnit",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "CostAtMovementInBaseUnit",
                table: "StockLotMovements");

            migrationBuilder.DropColumn(
                name: "QuantityInBaseUnit",
                table: "StockLotMovements");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "StockLotMovements");

            migrationBuilder.DropColumn(
                name: "QuantityOnHandInBaseUnit",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "QuantityReservedInBaseUnit",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "BaseUnitId",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "OrderedQuantityInBaseUnit",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantityInBaseUnit",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "RejectedQuantityInBaseUnit",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "UnitCostInBaseUnit",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "QuantityInBaseUnit",
                table: "SalesReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "SalesReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitPriceInBaseUnit",
                table: "SalesReturnLines");

            migrationBuilder.RenameTable(
                name: "SalesReturnLines",
                newName: "SalesReturnLine");

            migrationBuilder.RenameIndex(
                name: "IX_SalesReturnLines_SalesReturnId",
                table: "SalesReturnLine",
                newName: "IX_SalesReturnLine_SalesReturnId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesReturnLines_PartId",
                table: "SalesReturnLine",
                newName: "IX_SalesReturnLine_PartId");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "MovementType",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesReturnLine",
                table: "SalesReturnLine",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturnLine_Parts_PartId",
                table: "SalesReturnLine",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturnLine_SalesReturns_SalesReturnId",
                table: "SalesReturnLine",
                column: "SalesReturnId",
                principalTable: "SalesReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Parts_PartId",
                table: "StockLevels",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Warehouses_WarehouseId",
                table: "StockLevels",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockLevels_StockLevelId",
                table: "StockMovements",
                column: "StockLevelId",
                principalTable: "StockLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
