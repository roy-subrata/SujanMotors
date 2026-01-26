using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEmptySupplierPaymentTransactionNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix existing SupplierPayments with empty TransactionNumber
            migrationBuilder.Sql(@"
                UPDATE SupplierPayments
                SET TransactionNumber = 'TXN-' + CONVERT(VARCHAR(20), GETDATE(), 112) + '-' +
                                        REPLACE(CAST(NEWID() AS VARCHAR(36)), '-', '')
                WHERE TransactionNumber = '' OR TransactionNumber IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration - we don't want to revert the fix
        }
    }
}
