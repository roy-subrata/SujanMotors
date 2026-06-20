using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyPerLineUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarrantyRegistrations_SalesOrderLineId",
                table: "WarrantyRegistrations");

            migrationBuilder.CreateIndex(
                name: "UX_WarrantyRegistrations_SalesOrderLineId_NotVoid",
                table: "WarrantyRegistrations",
                column: "SalesOrderLineId",
                unique: true,
                filter: "[Status] <> 'VOID'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WarrantyRegistrations_SalesOrderLineId_NotVoid",
                table: "WarrantyRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_SalesOrderLineId",
                table: "WarrantyRegistrations",
                column: "SalesOrderLineId");
        }
    }
}
