using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountRuleTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedPromoCode",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CartDiscountRuleId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountRuleId",
                table: "SalesOrderLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CartDiscountRuleId",
                table: "SalesOrders",
                column: "CartDiscountRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLine_DiscountRuleId",
                table: "SalesOrderLine",
                column: "DiscountRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLine_Discounts_DiscountRuleId",
                table: "SalesOrderLine",
                column: "DiscountRuleId",
                principalTable: "Discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Discounts_CartDiscountRuleId",
                table: "SalesOrders",
                column: "CartDiscountRuleId",
                principalTable: "Discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLine_Discounts_DiscountRuleId",
                table: "SalesOrderLine");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Discounts_CartDiscountRuleId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CartDiscountRuleId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderLine_DiscountRuleId",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "AppliedPromoCode",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CartDiscountRuleId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DiscountRuleId",
                table: "SalesOrderLine");
        }
    }
}
