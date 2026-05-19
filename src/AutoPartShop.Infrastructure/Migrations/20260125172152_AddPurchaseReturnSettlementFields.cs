using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseReturnSettlementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SettledAmount",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledDate",
                table: "PurchaseReturns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementMethod",
                table: "PurchaseReturns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SettlementNotes",
                table: "PurchaseReturns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SettlementStatus",
                table: "PurchaseReturns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            // Migrate existing data - mark completed returns as settled
            migrationBuilder.Sql(@"
                UPDATE PurchaseReturns
                SET SettlementStatus = 'SETTLED',
                    SettledAmount = RefundAmount,
                    SettledDate = ReceivedDate,
                    SettlementMethod = 'CREDIT'
                WHERE Status IN ('RETURNED', 'CREDITED', 'RECEIVED')
                AND RefundAmount > 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettledAmount",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "SettledDate",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "SettlementMethod",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "SettlementNotes",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "SettlementStatus",
                table: "PurchaseReturns");
        }
    }
}
