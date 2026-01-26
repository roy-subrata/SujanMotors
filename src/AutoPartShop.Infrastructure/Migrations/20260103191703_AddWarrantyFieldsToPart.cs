using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyFieldsToPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasWarranty",
                table: "Parts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyCertificateTemplate",
                table: "Parts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyPeriodMonths",
                table: "Parts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "Parts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyType",
                table: "Parts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasWarranty",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WarrantyCertificateTemplate",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriodMonths",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "WarrantyType",
                table: "Parts");
        }
    }
}
