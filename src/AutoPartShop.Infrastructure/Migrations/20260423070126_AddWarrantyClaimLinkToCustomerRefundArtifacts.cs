using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyClaimLinkToCustomerRefundArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyClaimId",
                table: "CustomerPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyClaimId",
                table: "CustomerCreditNotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_WarrantyClaimId",
                table: "CustomerPayments",
                column: "WarrantyClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_WarrantyClaimId",
                table: "CustomerCreditNotes",
                column: "WarrantyClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_WarrantyClaimId",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCreditNotes_WarrantyClaimId",
                table: "CustomerCreditNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyClaimId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "WarrantyClaimId",
                table: "CustomerCreditNotes");
        }
    }
}
