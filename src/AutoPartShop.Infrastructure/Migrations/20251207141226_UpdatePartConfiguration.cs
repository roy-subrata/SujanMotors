using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Part_Categories_CategoryId",
                table: "Part");

            migrationBuilder.DropForeignKey(
                name: "FK_Part_Units_UnitId",
                table: "Part");

            migrationBuilder.DropForeignKey(
                name: "FK_PartVehicleCompatibility_Part_VehicleId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversion_Units_FromUnitId",
                table: "UnitConversion");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversion_Units_ToUnitId",
                table: "UnitConversion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitConversion",
                table: "UnitConversion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Part",
                table: "Part");

            migrationBuilder.RenameTable(
                name: "UnitConversion",
                newName: "UnitConversions");

            migrationBuilder.RenameTable(
                name: "Part",
                newName: "Parts");

            migrationBuilder.RenameIndex(
                name: "IX_UnitConversion_ToUnitId",
                table: "UnitConversions",
                newName: "IX_UnitConversions_ToUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_UnitConversion_FromUnitId",
                table: "UnitConversions",
                newName: "IX_UnitConversions_FromUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Part_UnitId",
                table: "Parts",
                newName: "IX_Parts_UnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Part_SKU",
                table: "Parts",
                newName: "IX_Parts_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Part_CategoryId",
                table: "Parts",
                newName: "IX_Parts_CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitConversions",
                table: "UnitConversions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parts",
                table: "Parts",
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
                name: "FK_PartVehicleCompatibility_Parts_VehicleId",
                table: "PartVehicleCompatibility",
                column: "VehicleId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_PartVehicleCompatibility_Parts_VehicleId",
                table: "PartVehicleCompatibility");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_FromUnitId",
                table: "UnitConversions");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_Units_ToUnitId",
                table: "UnitConversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitConversions",
                table: "UnitConversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parts",
                table: "Parts");

            migrationBuilder.RenameTable(
                name: "UnitConversions",
                newName: "UnitConversion");

            migrationBuilder.RenameTable(
                name: "Parts",
                newName: "Part");

            migrationBuilder.RenameIndex(
                name: "IX_UnitConversions_ToUnitId",
                table: "UnitConversion",
                newName: "IX_UnitConversion_ToUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_UnitConversions_FromUnitId",
                table: "UnitConversion",
                newName: "IX_UnitConversion_FromUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Parts_UnitId",
                table: "Part",
                newName: "IX_Part_UnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Parts_SKU",
                table: "Part",
                newName: "IX_Part_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Parts_CategoryId",
                table: "Part",
                newName: "IX_Part_CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitConversion",
                table: "UnitConversion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Part",
                table: "Part",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Part_Categories_CategoryId",
                table: "Part",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Part_Units_UnitId",
                table: "Part",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartVehicleCompatibility_Part_VehicleId",
                table: "PartVehicleCompatibility",
                column: "VehicleId",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversion_Units_FromUnitId",
                table: "UnitConversion",
                column: "FromUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversion_Units_ToUnitId",
                table: "UnitConversion",
                column: "ToUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
