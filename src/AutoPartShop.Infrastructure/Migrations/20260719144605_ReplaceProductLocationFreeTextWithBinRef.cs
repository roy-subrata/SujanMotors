using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProductLocationFreeTextWithBinRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductLocations_Warehouses_WarehouseId",
                table: "ProductLocations");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "ProductLocations");

            migrationBuilder.DropColumn(
                name: "Shelf",
                table: "ProductLocations");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "ProductLocations",
                newName: "WarehouseLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductLocations_WarehouseId",
                table: "ProductLocations",
                newName: "IX_ProductLocations_WarehouseLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLocations_WarehouseLocations_WarehouseLocationId",
                table: "ProductLocations",
                column: "WarehouseLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductLocations_WarehouseLocations_WarehouseLocationId",
                table: "ProductLocations");

            migrationBuilder.RenameColumn(
                name: "WarehouseLocationId",
                table: "ProductLocations",
                newName: "WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductLocations_WarehouseLocationId",
                table: "ProductLocations",
                newName: "IX_ProductLocations_WarehouseId");

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "ProductLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Shelf",
                table: "ProductLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLocations_Warehouses_WarehouseId",
                table: "ProductLocations",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
