using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureSupplierCurrentBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "CustomerPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "CustomerPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAdvancePaymentId",
                table: "CustomerPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_PaymentType",
                table: "CustomerPayments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_SourceAdvancePaymentId",
                table: "CustomerPayments",
                column: "SourceAdvancePaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_CustomerPayments_SourceAdvancePaymentId",
                table: "CustomerPayments",
                column: "SourceAdvancePaymentId",
                principalTable: "CustomerPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_CustomerPayments_SourceAdvancePaymentId",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_PaymentType",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_SourceAdvancePaymentId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "SourceAdvancePaymentId",
                table: "CustomerPayments");
        }
    }
}
