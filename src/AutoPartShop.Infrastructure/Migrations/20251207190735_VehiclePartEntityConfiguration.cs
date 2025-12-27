using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VehiclePartEntityConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibilities_Parts_VehicleId",
                table: "PartVehicleCompatibilities");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Part",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Part",
                table: "PartVehicleCompatibilities");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibilities_Parts_VehicleId",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
