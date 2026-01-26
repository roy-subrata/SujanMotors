using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptIdToSupplierPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column if it doesn't exist (may have been partially created by previous migration)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[SupplierPayments]') AND name = 'GoodsReceiptId')
                BEGIN
                    ALTER TABLE [SupplierPayments] ADD [GoodsReceiptId] uniqueidentifier NULL;
                END
            ");

            // Add index if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[SupplierPayments]') AND name = 'IX_SupplierPayments_GoodsReceiptId')
                BEGIN
                    CREATE INDEX [IX_SupplierPayments_GoodsReceiptId] ON [SupplierPayments] ([GoodsReceiptId]);
                END
            ");

            // Drop existing foreign key if it exists (should not, but just in case)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_SupplierPayments_GoodsReceipts_GoodsReceiptId]') AND parent_object_id = OBJECT_ID(N'[SupplierPayments]'))
                BEGIN
                    ALTER TABLE [SupplierPayments] DROP CONSTRAINT [FK_SupplierPayments_GoodsReceipts_GoodsReceiptId];
                END
            ");

            // Add foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_GoodsReceipts_GoodsReceiptId",
                table: "SupplierPayments",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_GoodsReceipts_GoodsReceiptId",
                table: "SupplierPayments");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_GoodsReceipts_GoodsReceiptId",
                table: "SupplierPayments",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
