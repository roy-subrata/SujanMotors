using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReleased = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartReservations_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartReservations_ExpiresAt",
                table: "CartReservations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CartReservations_PartId",
                table: "CartReservations",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartReservations_SessionId",
                table: "CartReservations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CartReservations_SessionId_PartId_ProductVariantId",
                table: "CartReservations",
                columns: new[] { "SessionId", "PartId", "ProductVariantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartReservations");
        }
    }
}
