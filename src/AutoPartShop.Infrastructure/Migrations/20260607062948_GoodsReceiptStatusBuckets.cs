using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GoodsReceiptStatusBuckets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing data: the old single "Rejected" bucket was treated as damaged
            // (it auto-raised DAMAGED returns), so rename it to DamagedQuantity and add Wrong* fresh.
            migrationBuilder.RenameColumn(
                name: "RejectedQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                newName: "DamagedQuantityInBaseUnit");

            migrationBuilder.RenameColumn(
                name: "RejectedQuantity",
                table: "GoodsReceiptLines",
                newName: "DamagedQuantity");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StockLots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "AVAILABLE");

            migrationBuilder.AddColumn<int>(
                name: "QuantityDamaged",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityDamagedInBaseUnit",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityQuarantine",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityQuarantineInBaseUnit",
                table: "StockLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WrongQuantity",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WrongQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_Status",
                table: "StockLots",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLots_Status",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "QuantityDamaged",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "QuantityDamagedInBaseUnit",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "QuantityQuarantine",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "QuantityQuarantineInBaseUnit",
                table: "StockLevels");

            migrationBuilder.DropColumn(
                name: "WrongQuantity",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "WrongQuantityInBaseUnit",
                table: "GoodsReceiptLines");

            migrationBuilder.RenameColumn(
                name: "DamagedQuantityInBaseUnit",
                table: "GoodsReceiptLines",
                newName: "RejectedQuantityInBaseUnit");

            migrationBuilder.RenameColumn(
                name: "DamagedQuantity",
                table: "GoodsReceiptLines",
                newName: "RejectedQuantity");
        }
    }
}
