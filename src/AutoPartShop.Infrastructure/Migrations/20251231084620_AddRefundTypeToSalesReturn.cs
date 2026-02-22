using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundTypeToSalesReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundType",
                table: "SalesReturns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CASH_REFUND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundType",
                table: "SalesReturns");
        }
    }
}
