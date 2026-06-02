using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OemNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Embedding = table.Column<SqlVector<float>>(type: "vector(1536)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEmbeddings_Parts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ProductId",
                table: "ProductEmbeddings",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductEmbeddings");
        }
    }
}
