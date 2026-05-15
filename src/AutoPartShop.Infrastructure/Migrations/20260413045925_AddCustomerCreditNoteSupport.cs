using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCreditNoteSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerCreditNoteId",
                table: "SalesReturns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerCreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IssuedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_SalesReturns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CustomerCreditNoteId",
                table: "SalesReturns",
                column: "CustomerCreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_CreditNoteNumber",
                table: "CustomerCreditNotes",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_CustomerId",
                table: "CustomerCreditNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_InvoiceId",
                table: "CustomerCreditNotes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_SalesOrderId",
                table: "CustomerCreditNotes",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_SalesReturnId",
                table: "CustomerCreditNotes",
                column: "SalesReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_Status",
                table: "CustomerCreditNotes",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturns_CustomerCreditNotes_CustomerCreditNoteId",
                table: "SalesReturns",
                column: "CustomerCreditNoteId",
                principalTable: "CustomerCreditNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturns_CustomerCreditNotes_CustomerCreditNoteId",
                table: "SalesReturns");

            migrationBuilder.DropTable(
                name: "CustomerCreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_CustomerCreditNoteId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "CustomerCreditNoteId",
                table: "SalesReturns");
        }
    }
}
