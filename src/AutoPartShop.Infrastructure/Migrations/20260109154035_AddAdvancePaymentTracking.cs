using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAdvancePaymentId",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaymentType",
                table: "SupplierPayments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SourceAdvancePaymentId",
                table: "SupplierPayments",
                column: "SourceAdvancePaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_SupplierPayments_SourceAdvancePaymentId",
                table: "SupplierPayments",
                column: "SourceAdvancePaymentId",
                principalTable: "SupplierPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_SupplierPayments_SourceAdvancePaymentId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_PaymentType",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_SourceAdvancePaymentId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "SourceAdvancePaymentId",
                table: "SupplierPayments");
        }
    }
}
