# Multi-Unit Support Migration Plan

## Status: ✅ **COMPLETED** (2026-04-07)

## Final Migration List (8 migrations, one per table)

| # | Migration | Size | Tables | Status |
|---|-----------|------|--------|--------|
| 1 | `20260407140000_AddBaseUnitAndUnitIdToParts` | 3.0K | Parts.BaseUnitId, Parts.UnitId + FKs | ✅ Applied |
| 2 | `20260407140100_AddMultiUnitToGoodsReceiptLines` | 3.7K | 5 columns + FK | ✅ Applied |
| 3 | `20260407140200_AddMultiUnitToSalesOrderLines` | 2.2K | 3 columns + FK | ✅ Applied |
| 4 | `20260407140300_AddMultiUnitToSalesReturnLine` | 2.2K | 3 columns + FK | ✅ Applied |
| 5 | `20260407140400_AddMultiUnitToStockLevels` | 2.2K | 3 columns + FK | ✅ Applied |
| 6 | `20260407140500_AddMultiUnitToStockLots` | 2.5K | 4 columns + FK | ✅ Applied |
| 7 | `20260407140600_AddMultiUnitToStockLotMovements` | 2.2K | 3 columns + FK | ✅ Applied |
| 8 | `20260407140700_AddMultiUnitToStockMovements` | 1.8K | 2 columns + FK | ✅ Applied |

**Total**: 28 columns added across 8 tables, all with proper indexes and foreign keys
