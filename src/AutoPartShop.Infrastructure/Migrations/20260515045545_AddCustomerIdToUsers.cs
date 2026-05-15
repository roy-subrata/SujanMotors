using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDiscountPercentOverride",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "MinMarginPercentOverride",
                table: "Parts");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPercentOverride",
                table: "Parts",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinMarginPercentOverride",
                table: "Parts",
                type: "decimal(5,2)",
                nullable: true);
        }
    }
}
