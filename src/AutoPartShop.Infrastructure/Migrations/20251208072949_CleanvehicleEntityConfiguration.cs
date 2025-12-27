using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanvehicleEntityConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicles_Parts_VehicleId",
                table: "PartVehicles");

            migrationBuilder.CreateIndex(
                name: "IX_PartVehicles_PartId",
                table: "PartVehicles",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicles_Parts_PartId",
                table: "PartVehicles",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicles_Parts_PartId",
                table: "PartVehicles");

            migrationBuilder.DropIndex(
                name: "IX_PartVehicles_PartId",
                table: "PartVehicles");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicles_Parts_VehicleId",
                table: "PartVehicles",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id");
        }
    }
}
