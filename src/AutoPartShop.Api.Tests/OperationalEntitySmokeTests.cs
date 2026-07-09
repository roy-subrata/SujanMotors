using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class OperationalEntitySmokeTests
{
    // ─── Warehouse ───────────────────────────────────────────────

    [Fact]
    public void Warehouse_Create_Valid_ShouldSetProperties()
    {
        var w = Warehouse.Create("Main Warehouse", "WH01", "123 Industrial Area",
            "Dhaka", "Dhaka", "Bangladesh", "1205", "Mr. Manager");
        Assert.Equal("Main Warehouse", w.Name);
        Assert.Equal("WH01", w.Code);
        Assert.Equal("123 Industrial Area", w.Location);
        Assert.Equal("Dhaka", w.City);
        Assert.True(w.IsActive);
    }

    [Fact]
    public void Warehouse_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Warehouse.Create("", "WH01", "Loc", "C", "S", "Co", "Z"));

    [Fact]
    public void Warehouse_Create_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Warehouse.Create("Name", "", "Loc", "C", "S", "Co", "Z"));

    [Fact]
    public void Warehouse_Create_EmptyLocation_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Warehouse.Create("Name", "WH01", "", "C", "S", "Co", "Z"));

    [Fact]
    public void Warehouse_Create_NameTooLong_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Warehouse.Create(new string('X', 151), "WH01", "Loc", "C", "S", "Co", "Z"));

    [Fact]
    public void Warehouse_ActivateDeactivate_ShouldToggle()
    {
        var w = Warehouse.Create("W", "W01", "L", "C", "S", "Co", "Z");
        w.Deactivate();
        Assert.False(w.IsActive);
        w.Activate();
        Assert.True(w.IsActive);
    }

    [Fact]
    public void Warehouse_Update_ShouldModifyProperties()
    {
        var w = Warehouse.Create("Old", "W01", "Loc", "C", "S", "Co", "Z");
        w.Update("New", "NewLoc", "NewC", "NewS", "NewCo", "Z2", "Mgr", "m@t.com",
            "123", 5000, "CBM", "desc", false);
        Assert.Equal("New", w.Name);
        Assert.Equal("NewLoc", w.Location);
        Assert.Equal(5000, w.StorageCapacity);
        Assert.Equal("CBM", w.CapacityUnit);
        Assert.False(w.IsActive);
    }

    [Fact]
    public void Warehouse_Update_NegativeCapacity_Throws()
    {
        var w = Warehouse.Create("W", "W01", "L", "C", "S", "Co", "Z");
        Assert.Throws<ArgumentException>(() =>
            w.Update("W", "L", "C", "S", "Co", "Z", "", "", "", -1, "SQM", "", true));
    }

    // ─── ProductLocation ─────────────────────────────────────────

    [Fact]
    public void ProductLocation_Create_Valid_ShouldSetProperties()
    {
        var pl = ProductLocation.Create(Guid.NewGuid(), Guid.NewGuid(), "Aisle-A", "Shelf-1", true);
        Assert.Equal("Aisle-A", pl.Section);
        Assert.Equal("Shelf-1", pl.Shelf);
        Assert.True(pl.IsPrimary);
        Assert.Equal("Aisle-A / Shelf-1", pl.GetFullLocation());
    }

    [Fact]
    public void ProductLocation_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductLocation.Create(Guid.Empty, Guid.NewGuid(), "A", "S"));

    [Fact]
    public void ProductLocation_Create_EmptyWarehouseId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductLocation.Create(Guid.NewGuid(), Guid.Empty, "A", "S"));

    [Fact]
    public void ProductLocation_Create_EmptySection_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductLocation.Create(Guid.NewGuid(), Guid.NewGuid(), "", "S"));

    [Fact]
    public void ProductLocation_Create_EmptyShelf_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductLocation.Create(Guid.NewGuid(), Guid.NewGuid(), "A", ""));

    [Fact]
    public void ProductLocation_Update_ShouldModify()
    {
        var pl = ProductLocation.Create(Guid.NewGuid(), Guid.NewGuid(), "A", "S1");
        pl.Update("B", "S2", false);
        Assert.Equal("B", pl.Section);
        Assert.Equal("S2", pl.Shelf);
        Assert.False(pl.IsPrimary);
    }

    [Fact]
    public void ProductLocation_SetAsPrimary_ShouldWork()
    {
        var pl = ProductLocation.Create(Guid.NewGuid(), Guid.NewGuid(), "A", "S", false);
        pl.SetAsPrimary();
        Assert.True(pl.IsPrimary);
        pl.UnsetPrimary();
        Assert.False(pl.IsPrimary);
    }

    // ─── Unit ────────────────────────────────────────────────────

    [Fact]
    public void Unit_Create_Valid_ShouldSetProperties()
    {
        var u = Unit.Create("Pieces", "pcs", "Individual units");
        Assert.Equal("Pieces", u.Name);
        Assert.Equal("pcs", u.Symbol);
        Assert.Equal("Individual units", u.Description);
        Assert.True(u.IsActive);
        Assert.Equal(0, u.DisplayOrder);
    }

    [Fact]
    public void Unit_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => Unit.Create("", "pcs"));

    [Fact]
    public void Unit_Create_EmptySymbol_Throws() =>
        Assert.Throws<ArgumentException>(() => Unit.Create("Pieces", ""));

    [Fact]
    public void Unit_Create_NameTooLong_Throws() =>
        Assert.Throws<ArgumentException>(() => Unit.Create(new string('X', 101), "pcs"));

    [Fact]
    public void Unit_ActivateDeactivate_ShouldToggle()
    {
        var u = Unit.Create("Pieces", "pcs");
        u.Deactivate();
        Assert.False(u.IsActive);
        u.Activate();
        Assert.True(u.IsActive);
    }

    [Fact]
    public void Unit_Update_ShouldModifyProperties()
    {
        var u = Unit.Create("Pieces", "pcs");
        u.Update("Kilogram", "kg", "Weight unit", false, 1);
        Assert.Equal("Kilogram", u.Name);
        Assert.Equal("kg", u.Symbol);
        Assert.False(u.IsActive);
        Assert.Equal(1, u.DisplayOrder);
    }

    // ─── UnitConversion ───────────────────────────────────────────

    [Fact]
    public void UnitConversion_Create_Valid_ShouldSetProperties()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var uc = UnitConversion.Create(fromId, toId, 1000m, "g to kg");
        Assert.Equal(fromId, uc.FromUnitId);
        Assert.Equal(toId, uc.ToUnitId);
        Assert.Equal(1000m, uc.ConversionFactor);
        Assert.True(uc.IsActive);
    }

    [Fact]
    public void UnitConversion_Create_EmptyFromId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            UnitConversion.Create(Guid.Empty, Guid.NewGuid(), 1m));

    [Fact]
    public void UnitConversion_Create_EmptyToId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            UnitConversion.Create(Guid.NewGuid(), Guid.Empty, 1m));

    [Fact]
    public void UnitConversion_Create_SameIds_Throws()
    {
        var id = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            UnitConversion.Create(id, id, 1m));
    }

    [Fact]
    public void UnitConversion_Create_ZeroFactor_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 0));

    [Fact]
    public void UnitConversion_Create_NegativeFactor_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), -1m));

    [Fact]
    public void UnitConversion_Convert_ShouldMultiply()
    {
        var uc = UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 1000m);
        Assert.Equal(5000m, uc.Convert(5m));
    }

    [Fact]
    public void UnitConversion_ConvertReverse_ShouldDivide()
    {
        var uc = UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 1000m);
        Assert.Equal(5m, uc.ConvertReverse(5000m));
    }

    [Fact]
    public void UnitConversion_Convert_Inactive_Throws()
    {
        var uc = UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 2m);
        uc.Deactivate();
        Assert.Throws<InvalidOperationException>(() => uc.Convert(5m));
    }

    [Fact]
    public void UnitConversion_ConvertReverse_Inactive_Throws()
    {
        var uc = UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 2m);
        uc.Deactivate();
        Assert.Throws<InvalidOperationException>(() => uc.ConvertReverse(10m));
    }

    [Fact]
    public void UnitConversion_Update_ShouldModify()
    {
        var uc = UnitConversion.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        uc.Update(500m, "updated", false);
        Assert.Equal(500m, uc.ConversionFactor);
        Assert.False(uc.IsActive);
    }

    // ─── CartReservation ──────────────────────────────────────────

    [Fact]
    public void CartReservation_Create_Valid_ShouldSetProperties()
    {
        var cr = CartReservation.Create("session-1", Guid.NewGuid(), 3, 30);
        Assert.Equal("session-1", cr.SessionId);
        Assert.Equal(3, cr.Quantity);
        Assert.False(cr.IsReleased);
        Assert.False(cr.IsExpired);
    }

    [Fact]
    public void CartReservation_Create_EmptySession_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CartReservation.Create("", Guid.NewGuid(), 1));

    [Fact]
    public void CartReservation_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CartReservation.Create("s", Guid.Empty, 1));

    [Fact]
    public void CartReservation_Create_ZeroQuantity_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CartReservation.Create("s", Guid.NewGuid(), 0));

    [Fact]
    public void CartReservation_Release_ShouldSetFlag()
    {
        var cr = CartReservation.Create("s", Guid.NewGuid(), 1);
        cr.Release();
        Assert.True(cr.IsReleased);
    }

    [Fact]
    public void CartReservation_UpdateQuantity_ShouldChange()
    {
        var cr = CartReservation.Create("s", Guid.NewGuid(), 2);
        cr.UpdateQuantity(5);
        Assert.Equal(5, cr.Quantity);
    }

    [Fact]
    public void CartReservation_UpdateQuantity_NonPositive_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CartReservation.Create("s", Guid.NewGuid(), 2).UpdateQuantity(0));

    [Fact]
    public void CartReservation_ExtendTtl_ShouldUpdateExpiry()
    {
        var cr = CartReservation.Create("s", Guid.NewGuid(), 1, -1);
        Assert.True(cr.IsExpired);
        cr.ExtendTtl(60);
        Assert.False(cr.IsExpired);
    }

    // ─── Shipment ─────────────────────────────────────────────────

    [Fact]
    public void Shipment_Create_Valid_ShouldSetProperties()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid(), "CourierX", "TRACK-123", DateTime.Today.AddDays(3));
        Assert.Equal("SHP-001", s.ShipmentNumber);
        Assert.Equal("PENDING", s.Status);
        Assert.Equal("CourierX", s.CourierName);
        Assert.Equal("TRACK-123", s.TrackingNumber);
    }

    [Fact]
    public void Shipment_Create_EmptyNumber_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shipment.Create("", Guid.NewGuid()));

    [Fact]
    public void Shipment_Create_EmptySalesOrderId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shipment.Create("SHP-001", Guid.Empty));

    [Fact]
    public void Shipment_Dispatch_ShouldTransition()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 2);
        s.Lines.Add(line);
        s.Dispatch("TRK-999");
        Assert.Equal("DISPATCHED", s.Status);
        Assert.NotNull(s.DispatchedDate);
        Assert.Equal("TRK-999", s.TrackingNumber);
    }

    [Fact]
    public void Shipment_Dispatch_NoLines_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => s.Dispatch());
    }

    [Fact]
    public void Shipment_Dispatch_NonPending_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        Assert.Throws<InvalidOperationException>(() => s.Dispatch());
    }

    [Fact]
    public void Shipment_MarkInTransit_ShouldTransition()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        s.MarkInTransit();
        Assert.Equal("IN_TRANSIT", s.Status);
    }

    [Fact]
    public void Shipment_MarkInTransit_NonDispatched_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => s.MarkInTransit());
    }

    [Fact]
    public void Shipment_MarkDelivered_ShouldTransition()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        s.MarkDelivered();
        Assert.Equal("DELIVERED", s.Status);
        Assert.NotNull(s.DeliveredDate);
    }

    [Fact]
    public void Shipment_MarkDelivered_FromPending_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => s.MarkDelivered());
    }

    [Fact]
    public void Shipment_MarkFailed_ShouldTransition()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        s.MarkFailed("Address not found");
        Assert.Equal("FAILED", s.Status);
        Assert.Equal("Address not found", s.FailureReason);
    }

    [Fact]
    public void Shipment_MarkFailed_AlreadyDelivered_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        s.MarkDelivered();
        Assert.Throws<InvalidOperationException>(() => s.MarkFailed("reason"));
    }

    [Fact]
    public void Shipment_MarkFailed_EmptyReason_Throws()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        var line = ShipmentLine.Create(s.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        s.Lines.Add(line);
        s.Dispatch();
        Assert.Throws<ArgumentException>(() => s.MarkFailed(""));
    }

    [Fact]
    public void Shipment_UpdateTracking_ShouldSet()
    {
        var s = Shipment.Create("SHP-001", Guid.NewGuid());
        s.UpdateTracking("FedEx", "FDX-123", DateTime.Today.AddDays(5));
        Assert.Equal("FedEx", s.CourierName);
        Assert.Equal("FDX-123", s.TrackingNumber);
    }

    // ─── ShipmentLine ─────────────────────────────────────────────

    [Fact]
    public void ShipmentLine_Create_Valid_ShouldSetProperties()
    {
        var sl = ShipmentLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, 10);
        Assert.Equal(5, sl.Quantity);
        Assert.Equal(10, sl.QuantityInBaseUnit);
    }

    [Fact]
    public void ShipmentLine_Create_EmptyShipmentId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ShipmentLine.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 1));

    [Fact]
    public void ShipmentLine_Create_EmptySalesOrderLineId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ShipmentLine.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 1));

    [Fact]
    public void ShipmentLine_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ShipmentLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1));

    [Fact]
    public void ShipmentLine_Create_ZeroQuantity_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ShipmentLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0));

    // ─── ProductMedia ─────────────────────────────────────────────

    [Fact]
    public void ProductMedia_Create_Valid_ShouldSetProperties()
    {
        var pm = ProductMedia.Create(Guid.NewGuid(), "https://img.com/1.jpg", "image", 1, true);
        Assert.Equal("https://img.com/1.jpg", pm.Url);
        Assert.Equal("image", pm.MediaType);
        Assert.Equal(1, pm.SortOrder);
        Assert.True(pm.IsPrimary);
    }

    [Fact]
    public void ProductMedia_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductMedia.Create(Guid.Empty, "https://img.com/1.jpg"));

    [Fact]
    public void ProductMedia_Create_EmptyUrl_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductMedia.Create(Guid.NewGuid(), ""));

    [Fact]
    public void ProductMedia_Update_ShouldModify()
    {
        var pm = ProductMedia.Create(Guid.NewGuid(), "https://old.jpg");
        pm.Update("https://new.jpg", "video", 2, false, null, "alt text", "file.mp4");
        Assert.Equal("https://new.jpg", pm.Url);
        Assert.Equal("video", pm.MediaType);
        Assert.Equal(2, pm.SortOrder);
        Assert.False(pm.IsPrimary);
    }

    // ─── CustomerVehicle ──────────────────────────────────────────

    [Fact]
    public void CustomerVehicle_Create_Valid_ShouldSetProperties()
    {
        var cv = CustomerVehicle.Create(Guid.NewGuid(), "DHA-1234", "Toyota", "Corolla", 2020);
        Assert.Equal("DHA-1234", cv.RegistrationNo);
        Assert.Equal("Toyota", cv.Make);
        Assert.Equal("Corolla", cv.Model);
        Assert.Equal(2020, cv.Year);
        Assert.True(cv.IsActive);
        Assert.Equal("Toyota Corolla (DHA-1234)", cv.GetLabel());
    }

    [Fact]
    public void CustomerVehicle_Create_EmptyCustomerId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CustomerVehicle.Create(Guid.Empty, "REG", "M", "Mo"));

    [Fact]
    public void CustomerVehicle_Create_EmptyRegNo_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CustomerVehicle.Create(Guid.NewGuid(), "", "M", "Mo"));

    [Fact]
    public void CustomerVehicle_Create_InvalidYear_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CustomerVehicle.Create(Guid.NewGuid(), "REG", "M", "Mo", 1800));

    [Fact]
    public void CustomerVehicle_Create_NegativeMileage_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CustomerVehicle.Create(Guid.NewGuid(), "REG", "M", "Mo", 2020, mileage: -1));

    [Fact]
    public void CustomerVehicle_ActivateDeactivate_ShouldToggle()
    {
        var cv = CustomerVehicle.Create(Guid.NewGuid(), "REG", "M", "Mo");
        cv.Deactivate();
        Assert.False(cv.IsActive);
        cv.Activate();
        Assert.True(cv.IsActive);
    }

    [Fact]
    public void CustomerVehicle_Update_ShouldModify()
    {
        var cv = CustomerVehicle.Create(Guid.NewGuid(), "OLD-REG", "Old", "Model");
        cv.Update("NEW-REG", "New", "Model2", 2023, "Petrol", "VIN123", "Red", 50000, "note", null);
        Assert.Equal("NEW-REG", cv.RegistrationNo);
        Assert.Equal("New", cv.Make);
        Assert.Equal("Model2", cv.Model);
        Assert.Equal(2023, cv.Year);
        Assert.Equal("Petrol", cv.EngineType);
    }

    // ─── PaymentProvider ──────────────────────────────────────────

    [Fact]
    public void PaymentProvider_Create_Valid_ShouldSetProperties()
    {
        var pp = PaymentProvider.Create("bKash", "MOBILE_BANKING");
        Assert.Equal("bKash", pp.ProviderName);
        Assert.Equal("MOBILE_BANKING", pp.ProviderType);
        Assert.Equal("ACTIVE", pp.Status);
    }

    [Fact]
    public void PaymentProvider_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PaymentProvider.Create("", "CASH"));

    [Fact]
    public void PaymentProvider_Create_InvalidType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PaymentProvider.Create("x", "INVALID"));

    [Fact]
    public void PaymentProvider_ActivateDeactivate_ShouldToggle()
    {
        var pp = PaymentProvider.Create("Cash", "CASH");
        pp.Deactivate();
        Assert.Equal("INACTIVE", pp.Status);
        pp.Activate();
        Assert.Equal("ACTIVE", pp.Status);
    }

    [Fact]
    public void PaymentProvider_SetBankDetails_ShouldSet()
    {
        var pp = PaymentProvider.Create("BOC", "BANK_TRANSFER");
        pp.SetBankDetails("Bank of China", "123456", "RT-001", "Beneficiary");
        Assert.Equal("Bank of China", pp.BankName);
        Assert.Equal("123456", pp.BankAccountNumber);
    }

    [Fact]
    public void PaymentProvider_SetMobileBankingDetails_ShouldSet()
    {
        var pp = PaymentProvider.Create("bKash", "MOBILE_BANKING");
        pp.SetMobileBankingDetails("01711111111", "John Doe", "agent123");
        Assert.Equal("01711111111", pp.MobileNumber);
        Assert.Equal("John Doe", pp.AccountHolderName);
    }

    [Fact]
    public void PaymentProvider_CalculateFee_Fixed_ShouldReturnFixed()
    {
        var pp = PaymentProvider.Create("Cash", "CASH");
        pp.SetTransactionFees("FIXED", 10m);
        Assert.Equal(10m, pp.CalculateFee(1000m));
    }

    [Fact]
    public void PaymentProvider_CalculateFee_Percentage_ShouldReturnPercent()
    {
        var pp = PaymentProvider.Create("Cash", "CASH");
        pp.SetTransactionFees("PERCENTAGE", 2m);
        Assert.Equal(20m, pp.CalculateFee(1000m));
    }

    [Fact]
    public void PaymentProvider_CalculateFee_BelowMinimum_ShouldReturnZero()
    {
        var pp = PaymentProvider.Create("Cash", "CASH");
        pp.SetTransactionFees("FIXED", 10m, minimumAmount: 100m);
        Assert.Equal(0, pp.CalculateFee(50m));
    }

    [Fact]
    public void PaymentProvider_TestConnection_ShouldSetDate()
    {
        var pp = PaymentProvider.Create("Cash", "CASH");
        pp.TestConnection();
        Assert.NotNull(pp.LastTestedDate);
    }

    [Fact]
    public void PaymentProvider_SetCurrencies_SetWebhook_SetApiKey_SetMerchantId_ShouldWork()
    {
        var pp = PaymentProvider.Create("Stripe", "ONLINE_GATEWAY");
        pp.SetCurrencies("USD,EUR");
        pp.SetWebhookUrl("https://hook.example.com");
        pp.SetApiKey("sk_test_xxx");
        pp.SetMerchantId("merch_123");
        Assert.Equal("USD,EUR", pp.SupportedCurrencies);
        Assert.Equal("sk_test_xxx", pp.ApiKey);
        Assert.Equal("merch_123", pp.MerchantId);
    }

    // ─── SupplierPaymentAccount ───────────────────────────────────

    [Fact]
    public void SupplierPaymentAccount_Create_Valid_ShouldSetProperties()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "BANK_TRANSFER", "BOC Savings");
        Assert.Equal("BANK_TRANSFER", spa.AccountType);
        Assert.Equal("BOC Savings", spa.AccountName);
        Assert.True(spa.IsActive);
        Assert.False(spa.IsDefault);
    }

    [Fact]
    public void SupplierPaymentAccount_Create_EmptySupplierId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SupplierPaymentAccount.Create(Guid.Empty, "CASH", "Name"));

    [Fact]
    public void SupplierPaymentAccount_Create_EmptyType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SupplierPaymentAccount.Create(Guid.NewGuid(), "", "Name"));

    [Fact]
    public void SupplierPaymentAccount_Create_InvalidType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SupplierPaymentAccount.Create(Guid.NewGuid(), "INVALID", "Name"));

    [Fact]
    public void SupplierPaymentAccount_SetBankDetails_ShouldSet()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "BANK_TRANSFER", "BOC");
        spa.SetBankDetails("BOC", "123", "Ben", "Mirpur", "BR-01");
        Assert.Equal("BOC", spa.BankName);
        Assert.Equal("123", spa.BankAccountNumber);
        Assert.Equal("Mirpur", spa.BankBranchName);
    }

    [Fact]
    public void SupplierPaymentAccount_SetMobileBanking_ShouldSet()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "MOBILE_BANKING", "bKash");
        spa.SetMobileBankingDetails("01711111111", "John", "bKash");
        Assert.Equal("01711111111", spa.MobileNumber);
        Assert.Equal("bKash", spa.MobileProvider);
    }

    [Fact]
    public void SupplierPaymentAccount_GetDisplayText_BankTransfer_ShouldFormat()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "BANK_TRANSFER", "BOC");
        spa.SetBankDetails("BOC", "123456", "Ben");
        Assert.Equal("BOC - 123456", spa.GetDisplayText());
    }

    [Fact]
    public void SupplierPaymentAccount_GetDisplayText_MobileBanking_ShouldFormat()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "MOBILE_BANKING", "bKash");
        spa.SetMobileBankingDetails("01711111111", "John", "bKash");
        Assert.Equal("bKash - 01711111111", spa.GetDisplayText());
    }

    [Fact]
    public void SupplierPaymentAccount_ActivateDeactivate_ShouldToggle()
    {
        var spa = SupplierPaymentAccount.Create(Guid.NewGuid(), "CASH", "Petty");
        spa.Deactivate();
        Assert.False(spa.IsActive);
        spa.Activate();
        Assert.True(spa.IsActive);
    }

    // ─── Currency ─────────────────────────────────────────────────

    [Fact]
    public void Currency_Create_Valid_ShouldSetProperties()
    {
        var c = Currency.Create("BDT", "Bangladeshi Taka", "৳", 2, true, true);
        Assert.Equal("BDT", c.Code);
        Assert.Equal("Bangladeshi Taka", c.Name);
        Assert.Equal("৳", c.Symbol);
        Assert.Equal(2, c.DecimalPlaces);
        Assert.True(c.IsBaseCurrency);
    }

    [Fact]
    public void Currency_Create_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() => Currency.Create("", "Name", "$"));

    [Fact]
    public void Currency_Create_CodeNot3Chars_Throws() =>
        Assert.Throws<ArgumentException>(() => Currency.Create("BD", "Name", "$"));

    [Fact]
    public void Currency_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => Currency.Create("USD", "", "$"));

    [Fact]
    public void Currency_Create_EmptySymbol_Throws() =>
        Assert.Throws<ArgumentException>(() => Currency.Create("USD", "Dollar", ""));

    [Fact]
    public void Currency_Create_DecimalPlacesOutOfRange_Throws() =>
        Assert.Throws<ArgumentException>(() => Currency.Create("USD", "Dollar", "$", 5));

    [Fact]
    public void Currency_ActivateDeactivate_ShouldToggle()
    {
        var c = Currency.Create("USD", "Dollar", "$");
        c.Deactivate();
        Assert.False(c.IsActive);
        c.Activate();
        Assert.True(c.IsActive);
    }

    [Fact]
    public void Currency_DeactivateBaseCurrency_Throws()
    {
        var c = Currency.Create("BDT", "Taka", "৳", 2, true, true);
        Assert.Throws<InvalidOperationException>(() => c.Deactivate());
    }

    [Fact]
    public void Currency_DeleteBaseCurrency_Throws()
    {
        var c = Currency.Create("BDT", "Taka", "৳", 2, true, true);
        Assert.Throws<InvalidOperationException>(() => c.Delete());
    }

    [Fact]
    public void Currency_SetAsBaseCurrency_ShouldWork()
    {
        var c = Currency.Create("USD", "Dollar", "$");
        c.SetAsBaseCurrency();
        Assert.True(c.IsBaseCurrency);
        c.RemoveBaseCurrencyStatus();
        Assert.False(c.IsBaseCurrency);
    }

    [Fact]
    public void Currency_Update_ShouldModify()
    {
        var c = Currency.Create("BDT", "Taka", "৳");
        c.Update("US Dollar", "$", 2, false, 1);
        Assert.Equal("US Dollar", c.Name);
        Assert.Equal("$", c.Symbol);
        Assert.False(c.IsActive);
    }

    // ─── ExchangeRate ─────────────────────────────────────────────

    [Fact]
    public void ExchangeRate_Create_Valid_ShouldSetProperties()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today, null, "MANUAL");
        Assert.Equal(110m, er.Rate);
        Assert.True(er.IsActive);
    }

    [Fact]
    public void ExchangeRate_Create_EmptyFromId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ExchangeRate.Create(Guid.Empty, Guid.NewGuid(), 1m, DateTime.Today));

    [Fact]
    public void ExchangeRate_Create_EmptyToId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ExchangeRate.Create(Guid.NewGuid(), Guid.Empty, 1m, DateTime.Today));

    [Fact]
    public void ExchangeRate_Create_SameIds_Throws()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            ExchangeRate.Create(id, id, 1m, DateTime.Today));
    }

    [Fact]
    public void ExchangeRate_Create_ZeroRate_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 0, DateTime.Today));

    [Fact]
    public void ExchangeRate_Create_ExpiryBeforeEffective_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 1m, DateTime.Today, DateTime.Today.AddDays(-1)));

    [Fact]
    public void ExchangeRate_IsValidForDate_ShouldCheck()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today, DateTime.Today.AddMonths(1));
        Assert.True(er.IsValidForDate(DateTime.Today.AddDays(15)));
        Assert.False(er.IsValidForDate(DateTime.Today.AddDays(-1)));
        Assert.False(er.IsValidForDate(DateTime.Today.AddMonths(2)));
    }

    [Fact]
    public void ExchangeRate_IsValidForDate_Inactive_ReturnsFalse()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        er.Deactivate();
        Assert.False(er.IsValidForDate(DateTime.Today));
    }

    [Fact]
    public void ExchangeRate_Convert_ShouldMultiply()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        Assert.Equal(550m, er.Convert(5m));
    }

    [Fact]
    public void ExchangeRate_ActivateDeactivate_ShouldToggle()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        er.Deactivate();
        Assert.False(er.IsActive);
        er.Activate();
        Assert.True(er.IsActive);
    }

    [Fact]
    public void ExchangeRate_Update_ShouldModify()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        er.Update(120m, DateTime.Today.AddDays(1), DateTime.Today.AddMonths(2), "BANK", "updated");
        Assert.Equal(120m, er.Rate);
        Assert.Equal("BANK", er.Source);
    }

    [Fact]
    public void ExchangeRate_Delete_ShouldSetIsdeleted()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        er.Delete();
        Assert.True(er.Isdeleted);
        Assert.False(er.IsActive);
    }

    [Fact]
    public void ExchangeRate_IsValidForDate_Deleted_ReturnsFalse()
    {
        var er = ExchangeRate.Create(Guid.NewGuid(), Guid.NewGuid(), 110m, DateTime.Today);
        er.Delete();
        Assert.False(er.IsValidForDate(DateTime.Today));
    }

    // ─── NotificationLog ──────────────────────────────────────────

    [Fact]
    public void NotificationLog_Create_ShouldSetPending()
    {
        var n = NotificationLog.Create("SMS", "+8801711111111", "Your order is ready");
        Assert.Equal("SMS", n.Channel);
        Assert.Equal("+8801711111111", n.Recipient);
        Assert.Equal("PENDING", n.Status);
    }

    [Fact]
    public void NotificationLog_MarkSent_ShouldTransition()
    {
        var n = NotificationLog.Create("EMAIL", "a@b.com", "Hello");
        n.MarkSent();
        Assert.Equal("SENT", n.Status);
        Assert.NotNull(n.SentAt);
    }

    [Fact]
    public void NotificationLog_MarkFailed_ShouldSetError()
    {
        var n = NotificationLog.Create("EMAIL", "a@b.com", "Hello");
        n.MarkFailed("SMTP error");
        Assert.Equal("FAILED", n.Status);
        Assert.Equal("SMTP error", n.ErrorMessage);
    }

    // ─── AuditLog ─────────────────────────────────────────────────

    [Fact]
    public void AuditLog_Properties_ShouldBeSettable()
    {
        var al = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Product",
            EntityId = "123",
            Action = "UPDATE",
            PropertyName = "Price",
            OldValue = "100",
            NewValue = "120",
            PerformedBy = "admin",
            PerformedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Chrome"
        };
        Assert.Equal("Product", al.EntityName);
        Assert.Equal("UPDATE", al.Action);
        Assert.Equal("Price", al.PropertyName);
        Assert.Equal("100", al.OldValue);
        Assert.Equal("120", al.NewValue);
    }
}
