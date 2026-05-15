using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversalProductFields_GRNBatchExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Parts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepthCm",
                table: "Parts",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Parts",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPerishable",
                table: "Parts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Parts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PHYSICAL");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Parts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Parts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Parts",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Parts",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "GoodsReceiptLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "GoodsReceiptLines",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "IsPerishable",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "GoodsReceiptLines");
        }
    }
}
