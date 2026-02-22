using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPaymentAccountToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierPaymentAccountId",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierPaymentAccountId",
                table: "SupplierPayments",
                column: "SupplierPaymentAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_SupplierPaymentAccounts_SupplierPaymentAccountId",
                table: "SupplierPayments",
                column: "SupplierPaymentAccountId",
                principalTable: "SupplierPaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_SupplierPaymentAccounts_SupplierPaymentAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_SupplierPaymentAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "SupplierPaymentAccountId",
                table: "SupplierPayments");
        }
    }
}
