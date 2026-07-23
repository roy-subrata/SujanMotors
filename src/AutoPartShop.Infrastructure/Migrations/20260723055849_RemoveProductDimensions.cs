using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Parts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "WidthCm",
                table: "ProductVariants",
                type: "decimal(10,2)",
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

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Parts",
                type: "decimal(10,2)",
                nullable: true);
        }
    }
}
