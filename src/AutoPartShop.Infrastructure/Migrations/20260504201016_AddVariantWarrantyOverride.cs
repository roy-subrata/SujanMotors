using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantWarrantyOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasWarrantyOverride",
                table: "ProductVariants",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyPeriodMonthsOverride",
                table: "ProductVariants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTypeOverride",
                table: "ProductVariants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasWarrantyOverride",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriodMonthsOverride",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WarrantyTypeOverride",
                table: "ProductVariants");
        }
    }
}
