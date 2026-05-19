using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpEntityConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Part",
                table: "PartVehicleCompatibilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_PartVehicleCompatibility",
                table: "PartVehicleCompatibilities");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_FromUnitId",
                table: "UnitConversions");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_ToUnitId",
                table: "UnitConversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartVehicleCompatibilities",
                table: "PartVehicleCompatibilities");

            migrationBuilder.RenameTable(
                name: "PartVehicleCompatibilities",
                newName: "PartVehicles");

            migrationBuilder.RenameIndex(
                name: "IX_PartVehicleCompatibilities_VehicleId",
                table: "PartVehicles",
                newName: "IX_PartVehicles_VehicleId");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Make",
                table: "Vehicles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EngineType",
                table: "Vehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UnitConversions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PartVehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartVehicles",
                table: "PartVehicles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicles_Parts_VehicleId",
                table: "PartVehicles",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicles_Vehicles_VehicleId",
                table: "PartVehicles",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversions_Units_FromUnitId",
                table: "UnitConversions",
                column: "FromUnitId",
                principalTable: "Units",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversions_Units_ToUnitId",
                table: "UnitConversions",
                column: "ToUnitId",
                principalTable: "Units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicles_Parts_VehicleId",
                table: "PartVehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicles_Vehicles_VehicleId",
                table: "PartVehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_FromUnitId",
                table: "UnitConversions");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_ToUnitId",
                table: "UnitConversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartVehicles",
                table: "PartVehicles");

            migrationBuilder.RenameTable(
                name: "PartVehicles",
                newName: "PartVehicleCompatibilities");

            migrationBuilder.RenameIndex(
                name: "IX_PartVehicles_VehicleId",
                table: "PartVehicleCompatibilities",
                newName: "IX_PartVehicleCompatibilities_VehicleId");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Make",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "EngineType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UnitConversions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PartVehicleCompatibilities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartVehicleCompatibilities",
                table: "PartVehicleCompatibilities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Units_UnitId",
                table: "Parts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Part",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_PartVehicleCompatibility",
                table: "PartVehicleCompatibilities",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversions_Units_FromUnitId",
                table: "UnitConversions",
                column: "FromUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversions_Units_ToUnitId",
                table: "UnitConversions",
                column: "ToUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
