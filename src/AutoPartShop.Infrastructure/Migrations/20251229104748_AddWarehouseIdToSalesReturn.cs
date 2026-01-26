using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseIdToSalesReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "SalesReturns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Update existing sales returns with warehouse ID from their sales orders
            migrationBuilder.Sql(@"
                UPDATE sr
                SET sr.WarehouseId = so.WarehouseId
                FROM SalesReturns sr
                INNER JOIN SalesOrders so ON sr.SalesOrderId = so.Id
                WHERE sr.WarehouseId = '00000000-0000-0000-0000-000000000000'
                AND so.WarehouseId IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SalesReturns");
        }
    }
}
