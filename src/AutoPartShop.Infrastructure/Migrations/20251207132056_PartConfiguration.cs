using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Part_PartId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Vehicle_VehicleId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropIndex(
                name: "IX_PartVehicleCompatibility_PartId",
                table: "PartVehicleCompatibility");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId1",
                table: "PartVehicleCompatibility",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Part",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Part",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Part",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PartVehicleCompatibility_VehicleId1",
                table: "PartVehicleCompatibility",
                column: "VehicleId1");

            migrationBuilder.CreateIndex(
                name: "IX_Part_SKU",
                table: "Part",
                column: "SKU",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Part_VehicleId",
                table: "PartVehicleCompatibility",
                column: "VehicleId",
                principalTable: "Part",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Part_VehicleId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Vehicle_VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropIndex(
                name: "IX_PartVehicleCompatibility_VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropIndex(
                name: "IX_Part_SKU",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "VehicleId1",
                table: "PartVehicleCompatibility");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Part",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Part",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Part",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartVehicleCompatibility_PartId",
                table: "PartVehicleCompatibility",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Part_PartId",
                table: "PartVehicleCompatibility",
                column: "PartId",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Vehicle_VehicleId",
                table: "PartVehicleCompatibility",
                column: "VehicleId",
                principalTable: "Vehicle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
