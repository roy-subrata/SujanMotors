using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VehicleEntityConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Parts_VehicleId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Vehicle_VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle",
                table: "Vehicle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartVehicleCompatibility",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropIndex(
                name: "IX_PartVehicleCompatibility_VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropColumn(
                name: "VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.RenameTable(
                name: "Vehicle",
                newName: "Vehicles");

            migrationBuilder.RenameTable(
                name: "PartVehicleCompatibility",
                newName: "PartVehicleCompatibilities");

            migrationBuilder.RenameIndex(
                name: "IX_PartVehicleCompatibility_VehicleId",
                table: "PartVehicleCompatibilities",
                newName: "IX_PartVehicleCompatibilities_VehicleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartVehicleCompatibilities",
                table: "PartVehicleCompatibilities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibilities_Parts_VehicleId",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_PartVehicleCompatibility",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibilities_Parts_VehicleId",
                table: "PartVehicleCompatibilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_PartVehicleCompatibility",
                table: "PartVehicleCompatibilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartVehicleCompatibilities",
                table: "PartVehicleCompatibilities");

            migrationBuilder.RenameTable(
                name: "Vehicles",
                newName: "Vehicle");

            migrationBuilder.RenameTable(
                name: "PartVehicleCompatibilities",
                newName: "PartVehicleCompatibility");

            migrationBuilder.RenameIndex(
                name: "IX_PartVehicleCompatibilities_VehicleId",
                table: "PartVehicleCompatibility",
                newName: "IX_PartVehicleCompatibility_VehicleId");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId1",
                table: "PartVehicleCompatibility",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle",
                table: "Vehicle",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartVehicleCompatibility",
                table: "PartVehicleCompatibility",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PartVehicleCompatibility_VehicleId1",
                table: "PartVehicleCompatibility",
                column: "VehicleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Parts_VehicleId",
                table: "PartVehicleCompatibility",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Vehicle_VehicleId1",
                table: "PartVehicleCompatibility",
                column: "VehicleId1",
                principalTable: "Vehicle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
