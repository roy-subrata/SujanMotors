using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderFixedDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountFixedAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "PurchaseOrders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "TOTAL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountFixedAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseOrders");
        }
    }
}
