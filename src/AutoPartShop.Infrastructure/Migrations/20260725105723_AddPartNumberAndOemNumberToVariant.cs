using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartNumberAndOemNumberToVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OemNumber",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartNumber",
                table: "ProductVariants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_PartNumber",
                table: "ProductVariants",
                column: "PartNumber",
                unique: true,
                filter: "[PartNumber] IS NOT NULL AND [Isdeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_PartNumber",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "OemNumber",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PartNumber",
                table: "ProductVariants");
        }
    }
}
