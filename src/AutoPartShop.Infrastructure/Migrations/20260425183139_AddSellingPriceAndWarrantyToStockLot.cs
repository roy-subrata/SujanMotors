using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellingPriceAndWarrantyToStockLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasWarranty",
                table: "StockLots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "StockLots",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyPeriodMonths",
                table: "StockLots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "StockLots",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyType",
                table: "StockLots",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasWarranty",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriodMonths",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "WarrantyType",
                table: "StockLots");
        }
    }
}
