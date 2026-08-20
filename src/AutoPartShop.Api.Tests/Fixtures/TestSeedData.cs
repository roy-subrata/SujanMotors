using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Tests.Fixtures;

/// <summary>
/// Seeds deterministic test data for report calculation tests.
/// Uses raw SQL to bypass EF auditing and set exact date values.
/// All dates relative to DateTime.UtcNow so aging/expiry reports stay valid.
/// </summary>
public static class TestSeedData
{
    // Fixed IDs for cross-table FK references
    public static readonly Guid WarehouseId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CategoryBrakesId = new("22222222-2222-2222-2222-222222222221");
    public static readonly Guid CategoryFiltersId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BrandBoschId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PaymentProviderCashId = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid SupplierAId = new("55555555-5555-5555-5555-555555555551");
    public static readonly Guid SupplierBId = new("55555555-5555-5555-5555-555555555552");
    public static readonly Guid CustomerRetailId = new("66666666-6666-6666-6666-666666666661");
    public static readonly Guid CustomerWholesaleId = new("66666666-6666-6666-6666-666666666662");
    public static readonly Guid TechnicianId = new("77777777-7777-7777-7777-777777777777");
    public static readonly Guid CashierUserId = new("88888888-8888-8888-8888-888888888888");
    public static readonly Guid PartAId = new("99999999-9999-9999-9999-999999999991");
    public static readonly Guid PartBId = new("99999999-9999-9999-9999-999999999992");
    public static readonly Guid PartCId = new("99999999-9999-9999-9999-999999999993");

    // Sales Orders
    public static readonly Guid SO1Id = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    public static readonly Guid SO2Id = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    public static readonly Guid SO3Id = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
    public static readonly Guid SOL1_1Id = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01");
    public static readonly Guid SOL1_2Id = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02");
    public static readonly Guid SOL2_1Id = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb03");
    public static readonly Guid SOL3_1Id = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb04");

    // Invoices
    public static readonly Guid Invoice1Id = new("cccccccc-cccc-cccc-cccc-cccccccccc01");
    public static readonly Guid Invoice2Id = new("cccccccc-cccc-cccc-cccc-cccccccccc02");
    public static readonly Guid Invoice3Id = new("cccccccc-cccc-cccc-cccc-cccccccccc03");

    // Customer Payments
    public static readonly Guid CPayment1Id = new("dddddddd-dddd-dddd-dddd-aaaaaaaaaa01");
    public static readonly Guid CPayment2Id = new("dddddddd-dddd-dddd-dddd-aaaaaaaaaa02");

    // Sales Return
    public static readonly Guid SalesReturnId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01");
    public static readonly Guid SalesReturnLineId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee02");

    // Customer Credit Note
    public static readonly Guid CreditNoteId = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // Purchase Orders
    public static readonly Guid PO1Id = new("11111111-aaaa-aaaa-aaaa-111111111111");
    public static readonly Guid PO2Id = new("11111111-aaaa-aaaa-aaaa-111111111112");
    public static readonly Guid POL1_1Id = new("22222222-bbbb-bbbb-bbbb-222222222221");
    public static readonly Guid POL1_2Id = new("22222222-bbbb-bbbb-bbbb-222222222222");
    public static readonly Guid POL2_1Id = new("22222222-bbbb-bbbb-bbbb-222222222223");

    // Goods Receipts
    public static readonly Guid GRN1Id = new("33333333-cccc-cccc-cccc-333333333331");
    public static readonly Guid GRNL1_1Id = new("44444444-dddd-dddd-dddd-444444444441");
    public static readonly Guid GRNL1_2Id = new("44444444-dddd-dddd-dddd-444444444442");

    // Supplier Payment
    public static readonly Guid SPayment1Id = new("55555555-eeee-eeee-eeee-555555555551");

    // Purchase Return
    public static readonly Guid PurchaseReturnId = new("66666666-ffff-ffff-ffff-666666666661");
    public static readonly Guid PurchaseReturnLineId = new("66666666-ffff-ffff-ffff-666666666662");

    // Stock Levels
    public static readonly Guid StockLevel_AId = new("77777777-0000-0000-0000-777777777771");
    public static readonly Guid StockLevel_BId = new("77777777-0000-0000-0000-777777777772");
    public static readonly Guid StockLevel_CId = new("77777777-0000-0000-0000-777777777773");

    // Stock Lots
    public static readonly Guid Lot1Id = new("88888888-0000-0000-0000-888888888881");
    public static readonly Guid Lot2Id = new("88888888-0000-0000-0000-888888888882");
    public static readonly Guid Lot3Id = new("88888888-0000-0000-0000-888888888883");

    // Stock Lot Movements
    public static readonly Guid SLM1Id = new("99999999-0000-0000-0000-999999999991");
    public static readonly Guid SLM2Id = new("99999999-0000-0000-0000-999999999992");
    public static readonly Guid SLM3Id = new("99999999-0000-0000-0000-999999999993");
    public static readonly Guid SLM4Id = new("99999999-0000-0000-0000-999999999994");

    // Daily Expenses
    public static readonly Guid Expense1Id = new("aaaaaaaa-0000-0000-0000-aaaaaaaaaa01");
    public static readonly Guid Expense2Id = new("aaaaaaaa-0000-0000-0000-aaaaaaaaaa02");
    public static readonly Guid Expense3Id = new("aaaaaaaa-0000-0000-0000-aaaaaaaaaa03");

    public static async Task SeedAsync(AutoPartDbContext db)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        await using var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        // Warehouse
        await Exec(conn, $"INSERT INTO Warehouses (Id,Name,Code,Location,City,State,Country,PostalCode,Manager,IsActive,CapacityUnit,StorageCapacity,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{WarehouseId}','Main Warehouse','WH01','Dhaka','Dhaka','Dhaka','Bangladesh','1000','Manager',1,'SQM',1000,'{now:O}','{now:O}','system','system',0)");

        // Categories
        await Exec(conn, $"INSERT INTO Categories (Id,Name,Description,DisplayOrder,BreadcrumbPath,DepthLevel,ChildCount,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CategoryBrakesId}','Brakes','Brake parts',1,'Brakes',0,0,1,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Categories (Id,Name,Description,DisplayOrder,BreadcrumbPath,DepthLevel,ChildCount,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CategoryFiltersId}','Filters','Filter parts',2,'Filters',0,0,1,'{now:O}','{now:O}','system','system',0)");

        // Brand (added LogoUrl, Website, ContactEmail, ContactPhone)
        await Exec(conn, $"INSERT INTO Brands (Id,Name,Description,Country,DisplayOrder,IsActive,LogoUrl,Website,ContactEmail,ContactPhone,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{BrandBoschId}','Bosch','German brand','Germany',1,1,'','','','','{now:O}','{now:O}','system','system',0)");

        // Payment Provider
        await Exec(conn, $"INSERT INTO PaymentProviders (Id,ProviderName,ProviderType,Status,ApiKey,MerchantId,BankName,BankAccountNumber,BankRoutingNumber,BankIBAN,BankSWIFT,BeneficiaryName,MobileNumber,AccountHolderName,AgentNumber,TransactionFeeType,TransactionFeeAmount,MinimumAmount,MaximumAmount,SettlementDays,SupportedCurrencies,WebhookUrl,Notes,IsDefault,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PaymentProviderCashId}','Cash','CASH',0,'','','','','','','','','','','','FIXED',0,0,0,1,'','', '',1,'{now:O}','{now:O}','system','system',0)");

        // Suppliers (added CurrentBalance)
        await Exec(conn, $"INSERT INTO Suppliers (Id,Name,Code,ContactPerson,Email,Phone,Address,City,State,Country,PostalCode,PaymentTerms,CreditLimit,IsActive,Rating,CurrentBalance,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SupplierAId}','Supplier Alpha','SUP-A','Ali','a@test.com','011','Addr','Dhaka','Dhaka','Bangladesh','1000','NET30',0,1,5,0,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Suppliers (Id,Name,Code,ContactPerson,Email,Phone,Address,City,State,Country,PostalCode,PaymentTerms,CreditLimit,IsActive,Rating,CurrentBalance,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SupplierBId}','Supplier Beta','SUP-B','Bari','b@test.com','022','Addr2','Chittagong','Chittagong','Bangladesh','4000','NET30',0,1,5,0,'{now:O}','{now:O}','system','system',0)");

        // Customers (added AlternatePhone,CurrentBalance,PrimaryContactPerson,TotalPurchaseAmount)
        await Exec(conn, $"INSERT INTO Customers (Id,CustomerCode,FirstName,LastName,Email,Phone,CompanyName,BillingAddress,ShippingAddress,City,State,PostalCode,Country,CustomerType,Status,Notes,AlternatePhone,CurrentBalance,PrimaryContactPerson,TotalPurchaseAmount,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CustomerRetailId}','CUST-001','Rahim','Khan','rahim@test.com','0171','','','','Dhaka','Dhaka','1000','Bangladesh','RETAIL','ACTIVE','','',0,'',0,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Customers (Id,CustomerCode,FirstName,LastName,Email,Phone,CompanyName,BillingAddress,ShippingAddress,City,State,PostalCode,Country,CustomerType,Status,Notes,AlternatePhone,CurrentBalance,PrimaryContactPerson,TotalPurchaseAmount,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CustomerWholesaleId}','CUST-002','Karim','Ahmed','karim@test.com','0172','Karim Corp','','','Dhaka','Dhaka','1000','Bangladesh','WHOLESALE','ACTIVE','','',0,'',0,'{now:O}','{now:O}','system','system',0)");

        // Technician
        await Exec(conn, $"INSERT INTO Technicians (Id,TechnicianCode,Name,Phone,Email,ShopName,Address,City,Notes,Status,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{TechnicianId}','TECH-01','Rafiq Uddin','0181','','','','','',0,'{now:O}','{now:O}','system','system',0)");

        // User (cashier) - Identity table (fixed column names: CreatedAt/ModifiedAt, added CreatedBy/IsActive, removed Isdeleted)
        await Exec(conn, $"INSERT INTO Users (Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount,FirstName,LastName,CreatedAt,ModifiedAt,CreatedBy,IsActive) VALUES ('{CashierUserId}','cashier1','CASHIER1','cashier@test.com','CASHIER@TEST.COM',1,'AQAAAAIAAYagAAAAEplaceholder','SN','CN',NULL,0,0,NULL,0,0,'Cashier','One','{now:O}','{now:O}','system',1)");

        // Parts (renamed PartNumber -> OemNumber)
        await Exec(conn, $"INSERT INTO Parts (Id,Name,Description,SKU,OemNumber,CategoryId,BrandId,CostPrice,SellingPrice,MinimumStock,IsActive,TaxCode,ProductType,IsPerishable,HasWarranty,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PartAId}','Brake Pad Set','Front brake pads','BRK-001','BP-A100','{CategoryBrakesId}','{BrandBoschId}',50,150,50,1,'','PHYSICAL',0,0,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Parts (Id,Name,Description,SKU,OemNumber,CategoryId,BrandId,CostPrice,SellingPrice,MinimumStock,IsActive,TaxCode,ProductType,IsPerishable,HasWarranty,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PartBId}','Brake Disc','Front brake disc','BRK-002','BD-B200','{CategoryBrakesId}','{BrandBoschId}',80,200,30,1,'','PHYSICAL',0,0,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Parts (Id,Name,Description,SKU,OemNumber,CategoryId,BrandId,CostPrice,SellingPrice,MinimumStock,IsActive,TaxCode,ProductType,IsPerishable,HasWarranty,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PartCId}','Oil Filter','Standard oil filter','FLT-001','OF-C300','{CategoryFiltersId}','{BrandBoschId}',20,60,0,1,'','PHYSICAL',0,0,'{now:O}','{now:O}','system','system',0)");

        // Stock Levels
        await Exec(conn, $"INSERT INTO StockLevels (Id,PartId,WarehouseId,QuantityOnHand,QuantityOnHandInBaseUnit,QuantityReserved,QuantityReservedInBaseUnit,QuantityDamaged,QuantityDamagedInBaseUnit,ReorderLevel,ReorderQuantity,Location,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{StockLevel_AId}','{PartAId}','{WarehouseId}',30,30,5,5,0,0,20,100,'',1,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLevels (Id,PartId,WarehouseId,QuantityOnHand,QuantityOnHandInBaseUnit,QuantityReserved,QuantityReservedInBaseUnit,QuantityDamaged,QuantityDamagedInBaseUnit,ReorderLevel,ReorderQuantity,Location,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{StockLevel_BId}','{PartBId}','{WarehouseId}',50,50,0,0,0,0,10,50,'',1,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLevels (Id,PartId,WarehouseId,QuantityOnHand,QuantityOnHandInBaseUnit,QuantityReserved,QuantityReservedInBaseUnit,QuantityDamaged,QuantityDamagedInBaseUnit,ReorderLevel,ReorderQuantity,Location,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{StockLevel_CId}','{PartCId}','{WarehouseId}',100,100,0,0,0,0,0,200,'',1,'{now:O}','{now:O}','system','system',0)");

        // Stock Lots (added IsActive, QuantityAvailableInBaseUnit, QuantityReceivedInBaseUnit)
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);
        var plus120 = today.AddDays(120);
        var minus60 = today.AddDays(-60);
        var minus30 = today.AddDays(-30);
        var minus10 = today.AddDays(-10);

        await Exec(conn, $"INSERT INTO StockLots (Id,LotNumber,PartId,WarehouseId,SupplierId,GoodsReceiptLineId,QuantityReceived,QuantityReceivedInBaseUnit,QuantityAvailable,QuantityAvailableInBaseUnit,CostPrice,CostPriceInBaseUnit,ReceivingDate,ExpiryDate,ManufacturerLotNumber,Currency,Notes,Status,HasWarranty,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Lot1Id}','LOT-A1','{PartAId}','{WarehouseId}','{SupplierAId}','00000000-0000-0000-0000-000000000000',30,30,30,30,50,50,'{minus60:O}','{yesterday:O}','','BDT','','AVAILABLE',0,1,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLots (Id,LotNumber,PartId,WarehouseId,SupplierId,GoodsReceiptLineId,QuantityReceived,QuantityReceivedInBaseUnit,QuantityAvailable,QuantityAvailableInBaseUnit,CostPrice,CostPriceInBaseUnit,ReceivingDate,ExpiryDate,ManufacturerLotNumber,Currency,Notes,Status,HasWarranty,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Lot2Id}','LOT-B1','{PartBId}','{WarehouseId}','{SupplierBId}','00000000-0000-0000-0000-000000000000',50,50,50,50,80,80,'{minus30:O}','{tomorrow:O}','','BDT','','AVAILABLE',0,1,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLots (Id,LotNumber,PartId,WarehouseId,SupplierId,GoodsReceiptLineId,QuantityReceived,QuantityReceivedInBaseUnit,QuantityAvailable,QuantityAvailableInBaseUnit,CostPrice,CostPriceInBaseUnit,ReceivingDate,ExpiryDate,ManufacturerLotNumber,Currency,Notes,Status,HasWarranty,IsActive,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Lot3Id}','LOT-C1','{PartCId}','{WarehouseId}','{SupplierAId}','00000000-0000-0000-0000-000000000000',100,100,100,100,20,20,'{minus10:O}','{plus120:O}','','BDT','','AVAILABLE',0,1,'{now:O}','{now:O}','system','system',0)");

        // Stock Lot Movements (for Profit by Product COGS)
        await Exec(conn, $"INSERT INTO StockLotMovements (Id,StockLotId,Quantity,QuantityInBaseUnit,MovementType,ReferenceId,ReferenceType,MovementDate,CostAtMovement,CostAtMovementInBaseUnit,Reason,Notes,UnitId,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SLM1Id}','{Lot1Id}',2,2,'SALE','{SO1Id}','SalesOrder','{now:O}',50,50,'Sale','',NULL,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLotMovements (Id,StockLotId,Quantity,QuantityInBaseUnit,MovementType,ReferenceId,ReferenceType,MovementDate,CostAtMovement,CostAtMovementInBaseUnit,Reason,Notes,UnitId,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SLM2Id}','{Lot2Id}',1,1,'SALE','{SO2Id}','SalesOrder','{now:O}',80,80,'Sale','',NULL,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLotMovements (Id,StockLotId,Quantity,QuantityInBaseUnit,MovementType,ReferenceId,ReferenceType,MovementDate,CostAtMovement,CostAtMovementInBaseUnit,Reason,Notes,UnitId,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SLM3Id}','{Lot1Id}',1,1,'RETURN','{SalesReturnId}','SalesReturn','{now:O}',50,50,'Return','',NULL,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO StockLotMovements (Id,StockLotId,Quantity,QuantityInBaseUnit,MovementType,ReferenceId,ReferenceType,MovementDate,CostAtMovement,CostAtMovementInBaseUnit,Reason,Notes,UnitId,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SLM4Id}','{Lot3Id}',1,1,'SALE','{SO3Id}','SalesOrder','{now:O}',20,20,'Sale','',NULL,'{now:O}','{now:O}','system','system',0)");

        // === SALES ORDERS ===
        // SO1: 2x PartA@150 (discount=10/ea) + 1x PartB@200 = SubTotal 480, Tax 48, TotalAmount 432, PAID
        // Removed PaymentMethod (doesn't exist), added VehicleLabel
        await Exec(conn, $"INSERT INTO SalesOrders (Id,SONumber,CustomerId,CustomerName,CustomerEmail,CustomerPhone,WarehouseId,TechnicianId,TechnicianName,SODate,DeliveryAddress,Notes,Currency,Channel,Status,PaymentStatus,VehicleLabel,SubTotal,DiscountPercentage,DiscountAmount,TotalAmount,TaxAmount,PaidAmount,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted,CashierId) VALUES ('{SO1Id}','SO-001','{CustomerRetailId}','Rahim Khan','rahim@test.com','0171','{WarehouseId}','{TechnicianId}','Rafiq Uddin','{now:O}','','','BDT','POS','COMPLETED','PAID','',480,0,20,432,48,432,'{now:O}','{now:O}','system','system',0,'{CashierUserId}')");
        await Exec(conn, $"INSERT INTO SalesOrderLine (Id,SalesOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,Discount,Description,ShippedQuantity,ShippedQuantityInBaseUnit,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SOL1_1Id}','{SO1Id}','{PartAId}',2,2,150,1,10,'',2,2,'{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO SalesOrderLine (Id,SalesOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,Discount,Description,ShippedQuantity,ShippedQuantityInBaseUnit,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SOL1_2Id}','{SO1Id}','{PartBId}',1,1,200,2,0,'',1,1,'{now:O}','{now:O}','system','system',0)");

        // SO2: 5x PartB@200 (discount=15/ea) = SubTotal 925, Tax 92.50, TotalAmount 832.50, PENDING
        await Exec(conn, $"INSERT INTO SalesOrders (Id,SONumber,CustomerId,CustomerName,CustomerEmail,CustomerPhone,WarehouseId,TechnicianId,TechnicianName,SODate,DeliveryAddress,Notes,Currency,Channel,Status,PaymentStatus,VehicleLabel,SubTotal,DiscountPercentage,DiscountAmount,TotalAmount,TaxAmount,PaidAmount,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted,CashierId) VALUES ('{SO2Id}','SO-002','{CustomerWholesaleId}','Karim Ahmed','karim@test.com','0172','{WarehouseId}',NULL,NULL,'{now:O}','','','BDT','POS','COMPLETED','PENDING','',925,0,0,832.50,92.50,0,'{now:O}','{now:O}','system','system',0,NULL)");
        await Exec(conn, $"INSERT INTO SalesOrderLine (Id,SalesOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,Discount,Description,ShippedQuantity,ShippedQuantityInBaseUnit,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SOL2_1Id}','{SO2Id}','{PartBId}',5,5,200,1,15,'',5,5,'{now:O}','{now:O}','system','system',0)");

        // SO3: 10x PartC@60 = SubTotal 600, Tax 60, TotalAmount 540, PENDING
        await Exec(conn, $"INSERT INTO SalesOrders (Id,SONumber,CustomerId,CustomerName,CustomerEmail,CustomerPhone,WarehouseId,TechnicianId,TechnicianName,SODate,DeliveryAddress,Notes,Currency,Channel,Status,PaymentStatus,VehicleLabel,SubTotal,DiscountPercentage,DiscountAmount,TotalAmount,TaxAmount,PaidAmount,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted,CashierId) VALUES ('{SO3Id}','SO-003','{CustomerRetailId}','Rahim Khan','rahim@test.com','0171','{WarehouseId}',NULL,NULL,'{now:O}','','','BDT','MOBILE','COMPLETED','PENDING','',600,0,0,540,60,0,'{now:O}','{now:O}','system','system',0,NULL)");
        await Exec(conn, $"INSERT INTO SalesOrderLine (Id,SalesOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,Discount,Description,ShippedQuantity,ShippedQuantityInBaseUnit,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SOL3_1Id}','{SO3Id}','{PartCId}',10,10,60,1,0,'',10,10,'{now:O}','{now:O}','system','system',0)");

        // Invoices
        var inv1Due = today.AddDays(30);
        var inv2Due = today.AddDays(-15); // overdue
        var inv3Due = today.AddDays(45);
        await Exec(conn, $"INSERT INTO Invoices (Id,InvoiceNumber,SalesOrderId,InvoiceDate,DueDate,SubTotal,TaxAmount,DiscountAmount,ReturnedAmount,Status,Notes,Currency,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Invoice1Id}','INV-001','{SO1Id}','{now:O}','{inv1Due:O}',480,48,0,0,'ISSUED','','BDT','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Invoices (Id,InvoiceNumber,SalesOrderId,InvoiceDate,DueDate,SubTotal,TaxAmount,DiscountAmount,ReturnedAmount,Status,Notes,Currency,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Invoice2Id}','INV-002','{SO2Id}','{now:O}','{inv2Due:O}',925,92.50,0,0,'ISSUED','','BDT','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO Invoices (Id,InvoiceNumber,SalesOrderId,InvoiceDate,DueDate,SubTotal,TaxAmount,DiscountAmount,ReturnedAmount,Status,Notes,Currency,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Invoice3Id}','INV-003','{SO3Id}','{now:O}','{inv3Due:O}',600,60,0,0,'ISSUED','','BDT','{now:O}','{now:O}','system','system',0)");

        // Customer Payments (added AuthorizationCode,IsReconciled,PaymentFee,SettledBy)
        await Exec(conn, $"INSERT INTO CustomerPayments (Id,CustomerId,PaymentProviderId,Amount,NetAmount,PaymentMethod,TransactionNumber,ReferenceNumber,PaymentDate,Currency,Status,PaymentType,InvoiceId,SourceAdvancePaymentId,RemainingAmount,Notes,AuthorizationCode,IsReconciled,PaymentFee,SettledBy,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CPayment1Id}','{CustomerRetailId}','{PaymentProviderCashId}',432,432,'CASH','TXN-CP1','','{now:O}','BDT','COMPLETED',1,'{Invoice1Id}',NULL,0,'','',0,0,'system','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO CustomerPayments (Id,CustomerId,PaymentProviderId,Amount,NetAmount,PaymentMethod,TransactionNumber,ReferenceNumber,PaymentDate,Currency,Status,PaymentType,InvoiceId,SourceAdvancePaymentId,RemainingAmount,Notes,AuthorizationCode,IsReconciled,PaymentFee,SettledBy,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CPayment2Id}','{CustomerWholesaleId}','{PaymentProviderCashId}',280,280,'CREDIT_NOTE','TXN-CP2','','{now:O}','BDT','COMPLETED',0,'{Invoice2Id}',NULL,0,'','',0,0,'system','{now:O}','{now:O}','system','system',0)");

        // Sales Return (added ApprovedBy)
        await Exec(conn, $"INSERT INTO SalesReturns (Id,ReturnNumber,SalesOrderId,InvoiceId,WarehouseId,ReturnDate,Reason,Status,RefundType,RefundAmount,ApprovedBy,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SalesReturnId}','RET-001','{SO1Id}','{Invoice1Id}','{WarehouseId}','{now:O}','Customer returned 1 brake pad','PROCESSED','FULL',150,'system','','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO SalesReturnLines (Id,SalesReturnId,SalesOrderLineId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,UnitPriceInBaseUnit,Condition,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SalesReturnLineId}','{SalesReturnId}','{SOL1_1Id}','{PartAId}',1,1,150,150,'UNOPENED','','{now:O}','{now:O}','system','system',0)");

        // Customer Credit Note
        await Exec(conn, $"INSERT INTO CustomerCreditNotes (Id,CreditNoteNumber,CustomerId,SalesReturnId,TotalAmount,UsedAmount,Currency,IssueDate,ExpiryDate,Status,Notes,IssuedBy,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{CreditNoteId}','CN-001','{CustomerRetailId}',NULL,100,0,'BDT','{now:O}',NULL,'AVAILABLE','','system','{now:O}','{now:O}','system','system',0)");

        // === PURCHASE ORDERS ===
        // Added ApprovedBy, PaymentStatus
        await Exec(conn, $"INSERT INTO PurchaseOrders (Id,PONumber,SupplierId,WarehouseId,PODate,ExpectedDeliveryDate,ActualDeliveryDate,Status,Notes,Currency,SubTotal,TaxPercentage,TaxAmount,DiscountPercentage,DiscountFixedAmount,DiscountAmount,TotalAmount,PaidAmount,CreditAppliedAmount,ApprovedBy,PaymentStatus,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PO1Id}','PO-001','{SupplierAId}','{WarehouseId}','{now:O}','{today.AddDays(14):O}','{now:O}','DELIVERED','','BDT',500,10,50,0,0,0,550,100,0,'system','PARTIAL','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO PurchaseOrderLines (Id,PurchaseOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,ReceivedQuantity,ReceivedQuantityInBaseUnit,Description,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{POL1_1Id}','{PO1Id}','{PartAId}',2,2,100,1,2,2,'','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO PurchaseOrderLines (Id,PurchaseOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,ReceivedQuantity,ReceivedQuantityInBaseUnit,Description,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{POL1_2Id}','{PO1Id}','{PartCId}',10,10,30,2,10,10,'','{now:O}','{now:O}','system','system',0)");

        await Exec(conn, $"INSERT INTO PurchaseOrders (Id,PONumber,SupplierId,WarehouseId,PODate,ExpectedDeliveryDate,ActualDeliveryDate,Status,Notes,Currency,SubTotal,TaxPercentage,TaxAmount,DiscountPercentage,DiscountFixedAmount,DiscountAmount,TotalAmount,PaidAmount,CreditAppliedAmount,ApprovedBy,PaymentStatus,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PO2Id}','PO-002','{SupplierBId}','{WarehouseId}','{now:O}','{today.AddDays(14):O}','{now:O}','DELIVERED','','BDT',360,10,36,0,0,0,396,0,0,'system','UNPAID','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO PurchaseOrderLines (Id,PurchaseOrderId,PartId,Quantity,QuantityInBaseUnit,UnitPrice,LineNumber,ReceivedQuantity,ReceivedQuantityInBaseUnit,Description,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{POL2_1Id}','{PO2Id}','{PartBId}',3,3,120,1,3,3,'','{now:O}','{now:O}','system','system',0)");

        // Goods Receipt (added CarrierName,DeliveryNotes,DeliveryReference,DriverName,InvoiceNotProvided)
        await Exec(conn, $"INSERT INTO GoodsReceipts (Id,GRNNumber,PurchaseOrderId,WarehouseId,ReceiptDate,Status,VerifiedBy,VerificationDate,TotalItemsReceived,DiscrepancyCount,CarrierName,DeliveryNotes,DeliveryReference,DriverName,InvoiceNotProvided,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{GRN1Id}','GRN-001','{PO1Id}','{WarehouseId}','{now:O}','ACCEPTED','system','{now:O}',12,0,'','','','',0,'','{now:O}','{now:O}','system','system',0)");
        // Goods Receipt Lines (added DamagedQuantityInBaseUnit,RejectionReason,SerialNumbers,WrongQuantityInBaseUnit)
        await Exec(conn, $"INSERT INTO GoodsReceiptLines (Id,GoodsReceiptId,PurchaseOrderLineId,PartId,OrderedQuantity,OrderedQuantityInBaseUnit,ReceivedQuantity,ReceivedQuantityInBaseUnit,DamagedQuantity,DamagedQuantityInBaseUnit,WrongQuantity,WrongQuantityInBaseUnit,Condition,UnitCost,UnitCostInBaseUnit,Currency,RejectionReason,SerialNumbers,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{GRNL1_1Id}','{GRN1Id}','{POL1_1Id}','{PartAId}',2,2,2,2,0,0,0,0,'GOOD',100,100,'BDT','','','','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO GoodsReceiptLines (Id,GoodsReceiptId,PurchaseOrderLineId,PartId,OrderedQuantity,OrderedQuantityInBaseUnit,ReceivedQuantity,ReceivedQuantityInBaseUnit,DamagedQuantity,DamagedQuantityInBaseUnit,WrongQuantity,WrongQuantityInBaseUnit,Condition,UnitCost,UnitCostInBaseUnit,Currency,RejectionReason,SerialNumbers,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{GRNL1_2Id}','{GRN1Id}','{POL1_2Id}','{PartCId}',10,10,10,10,0,0,0,0,'GOOD',30,30,'BDT','','','','{now:O}','{now:O}','system','system',0)");

        // Supplier Payment (added AuthorizationCode,ConfirmedBy,InvoiceNumber,Notes,PaymentFee,ProcessedBy)
        await Exec(conn, $"INSERT INTO SupplierPayments (Id,SupplierId,PaymentProviderId,PurchaseOrderId,Amount,NetAmount,PaymentMethod,TransactionNumber,ReferenceNumber,PaymentDate,Currency,Status,PaymentType,SourceAdvancePaymentId,RemainingAmount,Description,AuthorizationCode,ConfirmedBy,InvoiceNumber,Notes,PaymentFee,ProcessedBy,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{SPayment1Id}','{SupplierAId}','{PaymentProviderCashId}','{PO1Id}',100,100,'CASH','TXN-SP1','','{now:O}','BDT','COMPLETED','ADVANCE',NULL,0,'','','system','',0,0,'system','{now:O}','{now:O}','system','system',0)");

        // Purchase Return (added ApprovedBy, ReceivedBy)
        await Exec(conn, $"INSERT INTO PurchaseReturns (Id,ReturnNumber,PurchaseOrderId,SupplierId,GoodsReceiptId,ReturnDate,Reason,Status,RefundAmount,SettlementStatus,SettledAmount,SettledDate,SettlementMethod,SettlementNotes,CreditNoteAmount,ApprovedBy,ReceivedBy,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PurchaseReturnId}','PR-001','{PO1Id}','{SupplierAId}','{GRN1Id}','{now:O}','Defective part','CREDITED',100,'SETTLED',100,'{now:O}','CREDIT','Settled',100,'system','system','','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO PurchaseReturnLine (Id,PurchaseReturnId,PurchaseOrderLineId,PartId,StockLotId,Quantity,RejectedQuantity,UnitPrice,Condition,Notes,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{PurchaseReturnLineId}','{PurchaseReturnId}','{POL1_1Id}','{PartAId}',NULL,1,0,100,'UNOPENED','','{now:O}','{now:O}','system','system',0)");

        // === DAILY EXPENSES (added Notes,RecurrencePattern,ReferenceNumber)
        await Exec(conn, $"INSERT INTO DailyExpenses (Id,ExpenseDate,Category,Amount,Description,PaymentMethod,VendorName,Currency,Notes,RecurrencePattern,ReferenceNumber,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Expense1Id}','{today:yyyy-MM-dd}','RENT',200,'Monthly rent','CASH','','BDT','','','','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO DailyExpenses (Id,ExpenseDate,Category,Amount,Description,PaymentMethod,VendorName,Currency,Notes,RecurrencePattern,ReferenceNumber,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Expense2Id}','{today:yyyy-MM-dd}','UTILITIES',150,'Electricity bill','CASH','','BDT','','','','{now:O}','{now:O}','system','system',0)");
        await Exec(conn, $"INSERT INTO DailyExpenses (Id,ExpenseDate,Category,Amount,Description,PaymentMethod,VendorName,Currency,Notes,RecurrencePattern,ReferenceNumber,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy,Isdeleted) VALUES ('{Expense3Id}','{today:yyyy-MM-dd}','UTILITIES',100,'Internet bill','CASH','','BDT','','','','{now:O}','{now:O}','system','system',0)");
    }

    private static async Task Exec(DbConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
