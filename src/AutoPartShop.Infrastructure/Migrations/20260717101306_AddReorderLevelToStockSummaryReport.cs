using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Adds ReorderLevel to the stock summary report. The Stock Report document shows a LOW/OK
    /// status per line, which needs the stock level's reorder threshold — the proc already reads
    /// dbo.StockLevels, it just wasn't selecting the column.
    ///
    /// Purely additive to the result set: existing callers map by name and ignore the extra column.
    /// </summary>
    public partial class AddReorderLevelToStockSummaryReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StockSummaryProc(includeReorderLevel: true));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StockSummaryProc(includeReorderLevel: false));
        }

        /// <summary>
        /// Body of dbo.usp_Report_StockSummary. Kept in one place so Up and Down can't drift —
        /// the only difference between them is the ReorderLevel column.
        /// </summary>
        private static string StockSummaryProc(bool includeReorderLevel)
        {
            var reorderSelect = includeReorderLevel ? "        sl.ReorderLevel," : "";

            return $@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_StockSummary
    @WarehouseId      uniqueidentifier = NULL,
    @CategoryId       uniqueidentifier = NULL,
    @BrandId          uniqueidentifier = NULL,
    @Search           nvarchar(100) = NULL,
    @IncludeZeroStock bit = 0,
    @PageNumber       int = 1,
    @PageSize         int = 50
AS
BEGIN
    SET NOCOUNT ON;

    -- One row per StockLevel (part/variant/warehouse); valuation from AVAILABLE lots only.
    SELECT
        p.Id   AS PartId,
        p.PartNumber,
        p.Name AS PartName,
        p.SKU  AS Sku,
        v.Name AS VariantName,
        c.Name AS CategoryName,
        w.Name AS WarehouseName,
        sl.QuantityOnHand,
        sl.QuantityReserved,
        sl.QuantityDamaged,
        sl.QuantityOnHand - sl.QuantityReserved AS QuantityAvailable,
{reorderSelect}
        CASE WHEN ISNULL(lv.Qty, 0) = 0 THEN NULL
             ELSE CAST(lv.Value / lv.Qty AS decimal(18,2)) END AS AverageCost,
        ISNULL(lv.Value, 0) AS StockValue
    INTO #rows
    FROM dbo.StockLevels sl
    JOIN dbo.Parts p       ON p.Id = sl.PartId      AND p.Isdeleted = 0
    JOIN dbo.Warehouses w  ON w.Id = sl.WarehouseId AND w.Isdeleted = 0
    LEFT JOIN dbo.ProductVariants v ON v.Id = sl.VariantId AND v.Isdeleted = 0
    LEFT JOIN dbo.Categories c      ON c.Id = p.CategoryId AND c.Isdeleted = 0
    OUTER APPLY (
        SELECT SUM(lot.QuantityAvailable * lot.CostPrice) AS Value,
               SUM(lot.QuantityAvailable)                 AS Qty
        FROM dbo.StockLots lot
        WHERE lot.Isdeleted = 0
          AND lot.Status = 'AVAILABLE'
          AND lot.QuantityAvailable > 0
          AND lot.PartId = sl.PartId
          AND lot.WarehouseId = sl.WarehouseId
          AND ((sl.VariantId IS NULL AND lot.VariantId IS NULL) OR lot.VariantId = sl.VariantId)
    ) lv
    WHERE sl.Isdeleted = 0
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND (@BrandId IS NULL OR p.BrandId = @BrandId)
      AND (@IncludeZeroStock = 1 OR sl.QuantityOnHand <> 0)
      AND (@Search IS NULL
           OR p.Name LIKE N'%' + @Search + N'%'
           OR p.PartNumber LIKE N'%' + @Search + N'%'
           OR p.SKU LIKE N'%' + @Search + N'%');

    -- Result set 1: the requested page.
    SELECT *
    FROM #rows
    ORDER BY PartName, VariantName, WarehouseName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Result set 2: grand totals over the whole filtered set.
    SELECT
        COUNT(*)                        AS [RowCount],
        COUNT(DISTINCT PartId)          AS DistinctPartCount,
        ISNULL(SUM(QuantityOnHand), 0)  AS TotalQuantityOnHand,
        ISNULL(SUM(StockValue), 0)      AS TotalStockValue
    FROM #rows;
END
";
        }
    }
}
