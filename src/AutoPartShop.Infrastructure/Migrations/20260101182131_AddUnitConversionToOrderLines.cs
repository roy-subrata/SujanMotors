using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitConversionToOrderLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityInBaseUnit",
                table: "SalesOrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippedQuantityInBaseUnit",
                table: "SalesOrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "SalesOrderLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityInBaseUnit",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantityInBaseUnit",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "PurchaseOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLine_UnitId",
                table: "SalesOrderLine",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_UnitId",
                table: "PurchaseOrderLines",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Units_UnitId",
                table: "PurchaseOrderLines",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLine_Units_UnitId",
                table: "SalesOrderLine",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Units_UnitId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLine_Units_UnitId",
                table: "SalesOrderLine");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderLine_UnitId",
                table: "SalesOrderLine");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLines_UnitId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "QuantityInBaseUnit",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "ShippedQuantityInBaseUnit",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "QuantityInBaseUnit",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantityInBaseUnit",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "PurchaseOrderLines");
        }
    }
}
