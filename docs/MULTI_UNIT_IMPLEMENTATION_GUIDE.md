# Multi-Unit Support Implementation Guide

## Overview
This document describes the implementation of multi-unit conversion and base unit support across GoodsReceipts, PurchaseOrders, SalesOrders, SalesReturns, StockLevels, StockLots, and StockMovements.

## Architecture

### Core Concept
- **BaseUnitId**: The primary unit of measurement for stock/inventory tracking (e.g., "Pieces")
- **UnitId**: Display/sales unit which may differ from base unit (e.g., "Box" of 10 pieces)
- **QuantityInBaseUnit**: All quantities are stored in both the transaction unit AND the base unit for accurate stock tracking

### Unit Conversion Flow
```
Transaction Unit (e.g., Box) 
    → Convert using UnitConversion table 
    → Store in Base Unit (e.g., Pieces)
    → Stock Level tracks both units
```

## Database Changes

### New/Modified Columns

#### Parts Table
- ✅ `BaseUnitId` (nullable GUID) - Base unit for stock tracking
- ✅ `UnitId` (nullable GUID) - Display/sales unit (defaults to BaseUnitId)

#### GoodsReceiptLines Table
- ✅ `UnitId` (nullable GUID) - Unit of received items
- ✅ `OrderedQuantityInBaseUnit` (int)
- ✅ `ReceivedQuantityInBaseUnit` (int)
- ✅ `RejectedQuantityInBaseUnit` (int)
- ✅ `UnitCostInBaseUnit` (decimal)

#### SalesReturnLines Table
- ✅ `UnitId` (nullable GUID) - Unit of returned items
- ✅ `QuantityInBaseUnit` (int)
- ✅ `UnitPriceInBaseUnit` (decimal)

#### StockLevels Table
- ✅ `UnitId` (nullable GUID) - Unit for stock quantities
- ✅ `QuantityOnHandInBaseUnit` (int)
- ✅ `QuantityReservedInBaseUnit` (int)

#### StockLots Table
- ✅ `UnitId` (nullable GUID) - Unit for lot quantities
- ✅ `QuantityReceivedInBaseUnit` (int)
- ✅ `QuantityAvailableInBaseUnit` (int)
- ✅ `CostPriceInBaseUnit` (decimal)

#### StockMovements Table
- ✅ `UnitId` (nullable GUID) - Unit of movement
- ✅ `QuantityInBaseUnit` (int)

#### StockLotMovements Table
- ✅ `UnitId` (nullable GUID) - Unit of movement
- ✅ `QuantityInBaseUnit` (int)
- ✅ `CostAtMovementInBaseUnit` (decimal)

#### PurchaseOrderLines Table (Already Existed)
- ✅ `UnitId` (nullable GUID) - Already exists
- ✅ `QuantityInBaseUnit` (int) - Already exists
- ✅ `ReceivedQuantityInBaseUnit` (int) - Already exists

#### SalesOrderLines Table (Already Existed)
- ✅ `UnitId` (nullable GUID) - Already exists
- ✅ `QuantityInBaseUnit` (int) - Already exists
- ✅ `ShippedQuantityInBaseUnit` (int) - Already exists

## Services

### IUnitConversionService
Located: `/src/AutoPartShop.Application/Services/IUnitConversionService.cs`

Implementation: `/src/AutoPartShop.Infrastructure/Services/UnitConversionService.cs`

**Key Methods:**
- `ConvertQuantityAsync(quantity, fromUnitId, toUnitId)` - Convert between any two units
- `ConvertToBaseUnitAsync(quantity, fromUnitId, baseUnitId)` - Convert to base unit
- `ConvertFromBaseUnitAsync(quantity, fromBaseUnitId, toUnitId)` - Convert from base unit
- `GetConversionFactorAsync(fromUnitId, toUnitId)` - Get conversion factor
- `ConversionExistsAsync(fromUnitId, toUnitId)` - Check if conversion exists
- `ValidateConversionAsync(fromUnitId, toUnitId, out errorMessage)` - Validate conversion

## Entity Changes Summary

### 1. Part (Product.cs)
**Changes:**
- Added `BaseUnitId` property
- Added `BaseUnit` navigation property
- Updated `Create()` factory method to accept `baseUnitId` parameter
- Updated `Update()` method to accept `baseUnitId` parameter
- Default behavior: `UnitId` defaults to `BaseUnitId` if not specified

### 2. GoodsReceiptLine
**Changes:**
- Moved `UnitId` to top of properties section
- Added `OrderedQuantityInBaseUnit`
- Added `ReceivedQuantityInBaseUnit`
- Added `RejectedQuantityInBaseUnit`
- Added `UnitCostInBaseUnit`
- Added computed properties: `AcceptedQuantityInBaseUnit`, `TotalCostInBaseUnit`, `AcceptedTotalCostInBaseUnit`
- Updated `Create()` method signature
- Updated `RejectQuantity()` method signature

### 3. SalesReturnLine
**Changes:**
- Added `UnitId` property
- Added `QuantityInBaseUnit`
- Added `UnitPriceInBaseUnit`
- Added `Unit` navigation property
- Added computed property: `RefundAmountInBaseUnit`
- Updated `Create()` method signature

### 4. StockLevel
**Changes:**
- Added `UnitId` property
- Added `QuantityOnHandInBaseUnit`
- Added `QuantityReservedInBaseUnit`
- Added `Unit` navigation property
- Added computed property: `QuantityAvailableInBaseUnit`
- Updated `Create()` method signature
- Updated `AddStock()`, `RemoveStock()`, `ReserveStock()`, `ReleaseReservedStock()` to accept `quantityInBaseUnit`

### 5. StockLot
**Changes:**
- Added `UnitId` property
- Added `QuantityReceivedInBaseUnit`
- Added `QuantityAvailableInBaseUnit`
- Added `CostPriceInBaseUnit`
- Added `Unit` navigation property
- Updated `Create()` method signature
- Updated `AddStock()`, `RemoveStock()` methods to accept `quantityInBaseUnit`
- Added `GetTotalCostInBaseUnit()`, `GetAvailableCostInBaseUnit()` methods

### 6. StockMovement
**Changes:**
- Added `UnitId` property
- Added `QuantityInBaseUnit`
- Added `Unit` navigation property
- Updated `Create()` method signature

### 7. StockLotMovement
**Changes:**
- Added `UnitId` property
- Added `QuantityInBaseUnit`
- Added `CostAtMovementInBaseUnit`
- Added `Unit` navigation property
- Updated `Create()` method signature
- Added `GetMovementCostInBaseUnit()` method

## Configuration Changes

All EF Core configurations have been updated in:
- `/src/AutoPartShop.Infrastructure/Data/Configurations/PartConfiguration.cs`
- `/src/AutoPartShop.Infrastructure/Data/Configurations/PurchaseOrderConfiguration.cs`
- `/src/AutoPartShop.Infrastructure/Data/Configurations/SalesReturnConfiguration.cs`
- `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLotConfiguration.cs`
- `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLotMovementConfiguration.cs`
- `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLevelConfiguration.cs` (NEW)

## Next Steps (Not Yet Implemented)

### Phase 4: Application Layer Updates
1. **Update DTOs** - Add unit fields to request/response DTOs
2. **Update Services** - Implement conversion logic in business services:
   - PurchaseOrder service
   - GoodsReceipt service
   - SalesOrder service
   - SalesReturn service
3. **Update Repositories** - Add unit filtering support in queries

### Phase 5: Frontend (Angular)
1. **Update Forms** - Add unit selection dropdowns
2. **Display Units** - Show unit symbols in lists and details
3. **Conversion Validation** - Client-side unit conversion checks

## Generating Database Migration

To generate the migration for these changes, run:

```bash
cd /media/roy/New Volume3/AI/SujanMotors/src/AutoPartShop.Infrastructure
dotnet ef migrations add AddMultiUnitSupport -s ../AutoPartShop.Api
```

This will create a migration that adds all the new columns to the database.

## Testing Strategy

1. **Unit Conversion Tests**
   - Test direct conversions between units
   - Test reverse conversions
   - Test missing conversions
   - Test same-unit scenarios

2. **Entity Tests**
   - Test entity creation with base units
   - Test quantity calculations
   - Test computed properties

3. **Integration Tests**
   - Test full purchase order flow with unit conversions
   - Test goods receipt with unit conversions
   - Test sales order with unit conversions
   - Test stock level updates

## Benefits

1. **Accurate Stock Tracking** - All stock is tracked in base units for accuracy
2. **Flexible Sales/Purchasing** - Can sell/purchase in different units (e.g., boxes vs pieces)
3. **Audit Trail** - All movements track both transaction unit and base unit
4. **Cost Accuracy** - Costs are tracked in both units for accurate reporting
5. **Backward Compatible** - Existing data continues to work (nullable fields default correctly)

## Example Usage

```csharp
// Part: Oil Filter
// BaseUnitId = Pieces
// UnitId = Box (where 1 Box = 10 Pieces)

// Purchase Order
var poLine = PurchaseOrderLine.Create(
    purchaseOrderId: po.Id,
    partId: part.Id,
    quantity: 5,  // 5 boxes
    unitPrice: 50m,  // $50 per box
    lineNumber: 1,
    unitId: boxUnit.Id,  // Box unit
    quantityInBaseUnit: 50  // 50 pieces (5 boxes * 10)
);

// When goods are received
var grLine = GoodsReceiptLine.Create(
    goodsReceiptId: gr.Id,
    purchaseOrderLineId: poLine.Id,
    partId: part.Id,
    orderedQuantity: 5,  // 5 boxes
    receivedQuantity: 5,  // 5 boxes
    unitId: boxUnit.Id,
    orderedQuantityInBaseUnit: 50,  // 50 pieces
    receivedQuantityInBaseUnit: 50,  // 50 pieces
    unitCost: 50m,
    unitCostInBaseUnit: 5m  // $5 per piece
);

// Stock level updated with both units
stockLevel.AddStock(
    quantity: 5,  // 5 boxes
    quantityInBaseUnit: 50  // 50 pieces
);
```

## Files Modified

### Domain Layer
1. `/src/AutoPartShop.Domain/Entities/Product.cs`
2. `/src/AutoPartShop.Domain/Entities/GoodsReceiptLine.cs`
3. `/src/AutoPartShop.Domain/Entities/SalesReturnLine.cs`
4. `/src/AutoPartShop.Domain/Entities/StockLevel.cs`
5. `/src/AutoPartShop.Domain/Entities/StockLot.cs`
6. `/src/AutoPartShop.Domain/Entities/StockMovement.cs`
7. `/src/AutoPartShop.Domain/Entities/StockLotMovement.cs`

### Application Layer
8. `/src/AutoPartShop.Application/Services/IUnitConversionService.cs` (NEW)

### Infrastructure Layer
9. `/src/AutoPartShop.Infrastructure/Services/UnitConversionService.cs` (NEW)
10. `/src/AutoPartShop.Infrastructure/Dependency.cs`
11. `/src/AutoPartShop.Infrastructure/Data/Configurations/PartConfiguration.cs`
12. `/src/AutoPartShop.Infrastructure/Data/Configurations/PurchaseOrderConfiguration.cs`
13. `/src/AutoPartShop.Infrastructure/Data/Configurations/SalesReturnConfiguration.cs`
14. `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLotConfiguration.cs`
15. `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLotMovementConfiguration.cs`
16. `/src/AutoPartShop.Infrastructure/Data/Configurations/StockLevelConfiguration.cs` (NEW)

---

**Status:** Phase 1-4 Complete (Domain Models, Services, EF Configurations, Business Logic)
**Next:** Generate Database Migration and Test

## Business Logic Updates

### StockManagementService ✅
**File:** `/src/AutoPartShop.Api/Services/StockManagementService.cs`

**Changes:**
- Updated `ProcessGoodsReceiptAsync()` to use `InBaseUnit` fields from GoodsReceiptLine
- Updated `ReverseGoodsReceiptAsync()` to use `InBaseUnit` fields
- Modified `GetOrCreateStockLevelAsync()` to accept and use `baseUnitId` parameter
- Stock operations now pass both display unit and base unit quantities:
  - `stockLevel.AddStock(quantity, quantityInBaseUnit, reason)`
  - `StockMovement.Create(..., unitId, quantityInBaseUnit)`
  - `StockLot.Create(..., unitId, quantityReceivedInBaseUnit, costPriceInBaseUnit)`

### SalesReturnController ✅
**File:** `/src/AutoPartShop.Api/Controllers/SalesReturnController.cs`

**Changes:**
- Updated `Create()` to pass unit fields to `SalesReturnLine.Create()`
- Updated `Process()` method to:
  - Use `QuantityInBaseUnit` from SalesReturnLine
  - Create StockLevel with UnitId
  - Call `stockLevel.AddStock(quantity, quantityInBaseUnit, reason)`
  - Create StockMovement with both units
  - Create StockLot with both unit quantities
  - Create StockLotMovement with both unit quantities
