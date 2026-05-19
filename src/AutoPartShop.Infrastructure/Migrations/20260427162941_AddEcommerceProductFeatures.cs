using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEcommerceProductFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepthCm",
                table: "ProductVariants",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "ProductVariants",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "ProductVariants",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "ProductVariants",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "ProductMedias",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ProductMedias",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "ProductCatalogEntries",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "ProductCatalogEntries",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "ProductCatalogEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RichDescription",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "ProductMedias");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ProductMedias");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "ProductCatalogEntries");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "ProductCatalogEntries");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "ProductCatalogEntries");

            migrationBuilder.DropColumn(
                name: "RichDescription",
                table: "Parts");
        }
    }
}
