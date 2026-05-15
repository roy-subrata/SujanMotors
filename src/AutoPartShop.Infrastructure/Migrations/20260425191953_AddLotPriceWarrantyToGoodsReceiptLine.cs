using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLotPriceWarrantyToGoodsReceiptLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasWarranty",
                table: "GoodsReceiptLines",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "GoodsReceiptLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyPeriodMonths",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "GoodsReceiptLines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyType",
                table: "GoodsReceiptLines",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasWarranty",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriodMonths",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "WarrantyType",
                table: "GoodsReceiptLines");
        }
    }
}
