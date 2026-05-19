# Multi-Unit Support - Testing Guide

## ✅ Migration Applied Successfully!

The database has been updated with all the new multi-unit fields.

## How to Test

### 1. Start the API

```bash
cd "/media/roy/New Volume3/AI/SujanMotors/src/AutoPartShop.Api"
dotnet run
```

The API will start on the configured port (check appsettings.json for the URL).

### 2. Create Base Units

First, create some units via API or directly in the database:

```sql
-- Example: Create Pieces (base unit)
INSERT INTO Units (Id, Name, Code, Symbol, Description, IsActive, CreatedDate)
VALUES (NEWID(), 'Pieces', 'PCS', 'pcs', 'Individual pieces', 1, GETUTCDATE());

-- Example: Create Box (1 Box = 10 Pieces)
INSERT INTO Units (Id, Name, Code, Symbol, Description, IsActive, CreatedDate)
VALUES (NEWID(), 'Box', 'BOX', 'box', 'Box of 10 pieces', 1, GETUTCDATE());
```

### 3. Create Unit Conversion

```sql
-- Get the IDs from the units you just created
DECLARE @PiecesId UNIQUEIDENTIFIER = (SELECT Id FROM Units WHERE Code = 'PCS');
DECLARE @BoxId UNIQUEIDENTIFIER = (SELECT Id FROM Units WHERE Code = 'BOX');

-- Create conversion: 1 Box = 10 Pieces
INSERT INTO UnitConversions (Id, FromUnitId, ToUnitId, ConversionFactor, IsActive, CreatedDate)
VALUES (NEWID(), @BoxId, @PiecesId, 10.0, 1, GETUTCDATE());
```

### 4. Create a Part with BaseUnit

```json
POST /api/parts
{
  "name": "Oil Filter",
  "description": "Engine oil filter",
  "partNumber": "OF-001",
  "sku": "OIL-FILTER-001",
  "categoryId": "<your-category-id>",
  "baseUnitId": "<Pieces-Id>",  // Stock in pieces
  "unitId": "<Box-Id>",         // Sell in boxes
  "costPrice": 50.00,
  "sellingPrice": 75.00,
  "minimumStock": 10
}
```

### 5. Create Purchase Order in Boxes

```json
POST /api/purchaseorders
{
  "supplierId": "<supplier-id>",
  "warehouseId": "<warehouse-id>",
  "poDate": "2026-04-07",
  "lineItems": [
    {
      "partId": "<oil-filter-id>",
      "quantity": 5,  // 5 boxes
      "unitPrice": 50.00,
      "unitId": "<Box-Id>",
      "quantityInBaseUnit": 50,  // 50 pieces
      "description": "5 boxes of oil filters"
    }
  ]
}
```

### 6. Receive Goods Receipt

```json
POST /api/purchaseorders/{poId}/goods-receipts
{
  "grnNumber": "GRN-001",
  "warehouseId": "<warehouse-id>",
  "receiptDate": "2026-04-07",
  "lineItems": [
    {
      "purchaseOrderLineId": "<po-line-id>",
      "partId": "<oil-filter-id>",
      "orderedQuantity": 5,
      "receivedQuantity": 5,
      "unitId": "<Box-Id>",
      "orderedQuantityInBaseUnit": 50,
      "receivedQuantityInBaseUnit": 50,
      "unitCost": 50.00,
      "unitCostInBaseUnit": 5.00,
      "condition": "GOOD"
    }
  ]
}
```

### 7. Verify Stock Levels

```sql
-- Check stock level shows both units
SELECT 
    p.Name,
    sl.QuantityOnHand,
    sl.QuantityOnHandInBaseUnit,
    sl.QuantityReserved,
    sl.QuantityReservedInBaseUnit,
    sl.UnitId,
    u.Code as UnitCode,
    u2.Code as BaseUnitCode
FROM StockLevels sl
JOIN Parts p ON sl.PartId = p.Id
LEFT JOIN Units u ON sl.UnitId = u.Id
LEFT JOIN Units u2 ON p.BaseUnitId = u2.Id
WHERE p.Name = 'Oil Filter';
```

**Expected Result:**
- QuantityOnHand: 5 (boxes)
- QuantityOnHandInBaseUnit: 50 (pieces)

### 8. Check Stock Movements

```sql
-- View movement audit trail
SELECT 
    sm.MovementType,
    sm.Quantity,
    sm.QuantityInBaseUnit,
    sm.Reason,
    sm.ReferenceNumber,
    u.Code as UnitCode
FROM StockMovements sm
LEFT JOIN Units u ON sm.UnitId = u.Id
WHERE sm.StockLevelId IN (
    SELECT Id FROM StockLevels 
    WHERE PartId = (SELECT Id FROM Parts WHERE Name = 'Oil Filter')
);
```

**Expected Result:**
- MovementType: IN
- Quantity: 5
- QuantityInBaseUnit: 50
- Reason: GRN

### 9. Check Stock Lots

```sql
-- View lot information
SELECT 
    LotNumber,
    QuantityReceived,
    QuantityReceivedInBaseUnit,
    QuantityAvailable,
    QuantityAvailableInBaseUnit,
    CostPrice,
    CostPriceInBaseUnit,
    u.Code as UnitCode
FROM StockLots
WHERE PartId = (SELECT Id FROM Parts WHERE Name = 'Oil Filter');
```

**Expected Result:**
- QuantityReceived: 5 (boxes)
- QuantityReceivedInBaseUnit: 50 (pieces)
- CostPrice: 50.00 (per box)
- CostPriceInBaseUnit: 5.00 (per piece)

## API Endpoints to Test

### Units
- `GET /api/units` - List all units
- `POST /api/units` - Create unit

### Unit Conversions
- `GET /api/unitconversions` - List all conversions
- `POST /api/unitconversions` - Create conversion

### Parts
- `POST /api/parts` - Create part with BaseUnitId and UnitId
- `GET /api/parts/{id}` - Get part details

### Purchase Orders
- `POST /api/purchaseorders` - Create PO with unit support
- `POST /api/purchaseorders/{id}/goods-receipts` - Create GRN

### Stock Levels
- `GET /api/stock/levels` - View stock levels (should show both units)

### Stock Movements
- `GET /api/stock/movements` - View movement history

### Sales Orders
- `POST /api/salesorders` - Create sales order
- `POST /api/salesorders/{id}/dispatch` - Dispatch (should deduct stock in both units)

### Sales Returns
- `POST /api/salesreturns` - Create return with unit fields

## Verification Checklist

- [ ] Units created successfully
- [ ] UnitConversion created (1 Box = 10 Pieces)
- [ ] Part created with BaseUnitId and UnitId
- [ ] Purchase Order created with InBaseUnit quantities
- [ ] Goods Receipt processed
- [ ] StockLevel shows both QuantityOnHand and QuantityOnHandInBaseUnit
- [ ] StockMovement shows both Quantity and QuantityInBaseUnit
- [ ] StockLot shows both quantities and costs in both units
- [ ] Sales Order dispatches correctly
- [ ] Sales Return processes correctly

## Common Issues & Solutions

### Issue: "UnitConversionService not found"
**Solution**: Ensure Dependency.cs has the service registered (it should be uncommented now)

### Issue: "Conversion factor not found"
**Solution**: Make sure UnitConversions exist between the units you're using

### Issue: Stock not updating
**Solution**: Check that BaseUnitId is set on the Part, and UnitConversion exists

## SQL Queries for Quick Verification

```sql
-- Summary of multi-unit implementation
SELECT 
    'Units' as TableName, COUNT(*) as RecordCount FROM Units
UNION ALL
SELECT 'UnitConversions', COUNT(*) FROM UnitConversions
UNION ALL
SELECT 'PartsWithUnits', COUNT(*) FROM Parts WHERE BaseUnitId IS NOT NULL
UNION ALL
SELECT 'StockLevels_WithUnits', COUNT(*) FROM StockLevels WHERE UnitId IS NOT NULL
UNION ALL
SELECT 'StockMovements_WithBaseUnit', COUNT(*) FROM StockMovements WHERE QuantityInBaseUnit > 0
UNION ALL
SELECT 'StockLots_WithBaseUnit', COUNT(*) FROM StockLots WHERE QuantityReceivedInBaseUnit > 0;
```

---

**Status**: ✅ Migration Applied  
**Database**: Updated with multi-unit schema  
**Next**: Test with actual API calls using Postman or your frontend!
