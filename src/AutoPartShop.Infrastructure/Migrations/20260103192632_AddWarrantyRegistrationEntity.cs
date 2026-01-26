using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyRegistrationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarrantyRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarrantyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarrantyStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarrantyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarrantyPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    WarrantyTerms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyRegistrations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarrantyRegistrations_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarrantyRegistrations_SalesOrderLine_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalTable: "SalesOrderLine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarrantyRegistrations_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_CustomerId",
                table: "WarrantyRegistrations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_PartId",
                table: "WarrantyRegistrations",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_SalesOrderId",
                table: "WarrantyRegistrations",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_SalesOrderLineId",
                table: "WarrantyRegistrations",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_Status",
                table: "WarrantyRegistrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_WarrantyExpiryDate",
                table: "WarrantyRegistrations",
                column: "WarrantyExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_WarrantyNumber",
                table: "WarrantyRegistrations",
                column: "WarrantyNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyRegistrations");
        }
    }
}
