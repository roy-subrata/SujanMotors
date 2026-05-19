using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedCleanUpConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PaymentProvider_PaymentProviderId",
                table: "SupplierPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentProvider",
                table: "PaymentProvider");

            migrationBuilder.RenameTable(
                name: "PaymentProvider",
                newName: "PaymentProviders");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentProviders",
                table: "PaymentProviders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PaymentProviders_PaymentProviderId",
                table: "SupplierPayments",
                column: "PaymentProviderId",
                principalTable: "PaymentProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PaymentProviders_PaymentProviderId",
                table: "SupplierPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentProviders",
                table: "PaymentProviders");

            migrationBuilder.RenameTable(
                name: "PaymentProviders",
                newName: "PaymentProvider");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentProvider",
                table: "PaymentProvider",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PaymentProvider_PaymentProviderId",
                table: "SupplierPayments",
                column: "PaymentProviderId",
                principalTable: "PaymentProvider",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
