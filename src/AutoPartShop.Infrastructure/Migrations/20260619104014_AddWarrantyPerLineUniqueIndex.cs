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
            // Pre-step: the new filtered UNIQUE index allows only one non-void warranty per sold line.
            // Existing data may already have duplicates (the app-level check was added later), which
            // would make CREATE UNIQUE INDEX fail and crash startup. Auto-void the older duplicates,
            // keeping the most recently created warranty for each SalesOrderLineId.
            migrationBuilder.Sql(@"
                ;WITH dups AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY SalesOrderLineId
                                              ORDER BY CreatedDate DESC, Id DESC) AS rn
                    FROM WarrantyRegistrations
                    WHERE Status <> 'VOID'
                )
                UPDATE wr
                   SET Status = 'VOID',
                       VoidReason = ISNULL(wr.VoidReason, 'Auto-voided: duplicate warranty for the same sold line (unique-index migration)'),
                       VoidedDate = SYSUTCDATETIME()
                  FROM WarrantyRegistrations wr
                  INNER JOIN dups ON dups.Id = wr.Id
                 WHERE dups.rn > 1;");

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
