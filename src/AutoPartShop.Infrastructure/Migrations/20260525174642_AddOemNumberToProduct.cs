using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOemNumberToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OemNumber",
                table: "Parts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Challans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Challans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransportCompany",
                table: "Challans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "Challans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OemNumber",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Challans");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Challans");

            migrationBuilder.DropColumn(
                name: "TransportCompany",
                table: "Challans");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "Challans");
        }
    }
}
