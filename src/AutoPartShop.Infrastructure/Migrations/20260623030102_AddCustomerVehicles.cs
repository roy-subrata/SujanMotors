using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerVehicleId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleLabel",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CustomerVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Make = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: true),
                    EngineType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mileage = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CatalogVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_Vehicles_CatalogVehicleId",
                        column: x => x.CatalogVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerVehicleId",
                table: "SalesOrders",
                column: "CustomerVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_CatalogVehicleId",
                table: "CustomerVehicles",
                column: "CatalogVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_CustomerId",
                table: "CustomerVehicles",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_RegistrationNo",
                table: "CustomerVehicles",
                column: "RegistrationNo");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_CustomerVehicles_CustomerVehicleId",
                table: "SalesOrders",
                column: "CustomerVehicleId",
                principalTable: "CustomerVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_CustomerVehicles_CustomerVehicleId",
                table: "SalesOrders");

            migrationBuilder.DropTable(
                name: "CustomerVehicles");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CustomerVehicleId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CustomerVehicleId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "VehicleLabel",
                table: "SalesOrders");
        }
    }
}
