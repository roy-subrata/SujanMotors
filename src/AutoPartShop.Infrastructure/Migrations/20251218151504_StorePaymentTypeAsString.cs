using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StorePaymentTypeAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "SupplierPayments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "REGULAR",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "REGULAR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "REGULAR",
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldDefaultValue: "REGULAR");
        }
    }
}
