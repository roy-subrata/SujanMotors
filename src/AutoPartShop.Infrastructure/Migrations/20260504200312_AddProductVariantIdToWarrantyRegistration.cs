using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantIdToWarrantyRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "WarrantyRegistrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyRegistrations_ProductVariantId",
                table: "WarrantyRegistrations",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyRegistrations_ProductVariants_ProductVariantId",
                table: "WarrantyRegistrations",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyRegistrations_ProductVariants_ProductVariantId",
                table: "WarrantyRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyRegistrations_ProductVariantId",
                table: "WarrantyRegistrations");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "WarrantyRegistrations");
        }
    }
}
