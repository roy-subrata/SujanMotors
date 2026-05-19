using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTechnicianFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicianOrderLine");

            migrationBuilder.DropTable(
                name: "TechnicianPayment");

            migrationBuilder.DropTable(
                name: "TechnicianOrders");

            migrationBuilder.DropIndex(
                name: "IX_Technicians_OutstandingAmount",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "LastOrderDate",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "TotalOrders",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "TotalSalesAmount",
                table: "Technicians");

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianName",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TechnicianId",
                table: "SalesOrders",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Technicians_TechnicianId",
                table: "SalesOrders",
                column: "TechnicianId",
                principalTable: "Technicians",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Technicians_TechnicianId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TechnicianId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TechnicianName",
                table: "SalesOrders");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Technicians",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOrderDate",
                table: "Technicians",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "Technicians",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Technicians",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalOrders",
                table: "Technicians",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSalesAmount",
                table: "Technicians",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TechnicianOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RepairDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VehicleMake = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianOrders_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianOrderLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianOrderLine_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianOrderLine_TechnicianOrders_TechnicianOrderId",
                        column: x => x.TechnicianOrderId,
                        principalTable: "TechnicianOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianPayment_TechnicianOrders_TechnicianOrderId",
                        column: x => x.TechnicianOrderId,
                        principalTable: "TechnicianOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_OutstandingAmount",
                table: "Technicians",
                column: "OutstandingAmount");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrderLine_PartId",
                table: "TechnicianOrderLine",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrderLine_TechnicianOrderId",
                table: "TechnicianOrderLine",
                column: "TechnicianOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_CustomerId",
                table: "TechnicianOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_OrderDate",
                table: "TechnicianOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_OrderNumber",
                table: "TechnicianOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_OutstandingAmount",
                table: "TechnicianOrders",
                column: "OutstandingAmount");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_PaymentStatus",
                table: "TechnicianOrders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_Status",
                table: "TechnicianOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianOrders_TechnicianId",
                table: "TechnicianOrders",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianPayment_PaymentDate",
                table: "TechnicianPayment",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianPayment_PaymentMethod",
                table: "TechnicianPayment",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianPayment_TechnicianOrderId",
                table: "TechnicianPayment",
                column: "TechnicianOrderId");
        }
    }
}
