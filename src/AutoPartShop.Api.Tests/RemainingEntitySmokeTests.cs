using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class RemainingEntitySmokeTests
{
    // ─── Vehicle (Catalog) ────────────────────────────────────────

    [Fact]
    public void Vehicle_Create_Valid_ShouldSetProperties()
    {
        var v = Vehicle.Create("Toyota", "Corolla", 2020, "Petrol", "Popular sedan");
        Assert.Equal("Toyota", v.Make);
        Assert.Equal("Corolla", v.Model);
        Assert.Equal(2020, v.Year);
        Assert.Equal("Petrol", v.EngineType);
        Assert.True(v.IsActive);
    }

    [Fact]
    public void Vehicle_Create_EmptyMake_Throws() =>
        Assert.Throws<ArgumentException>(() => Vehicle.Create("", "Corolla", 2020, "Petrol"));

    [Fact]
    public void Vehicle_Create_EmptyModel_Throws() =>
        Assert.Throws<ArgumentException>(() => Vehicle.Create("Toyota", "", 2020, "Petrol"));

    [Fact]
    public void Vehicle_Create_EmptyEngineType_Throws() =>
        Assert.Throws<ArgumentException>(() => Vehicle.Create("Toyota", "Corolla", 2020, ""));

    [Fact]
    public void Vehicle_Create_InvalidYear_Throws() =>
        Assert.Throws<ArgumentException>(() => Vehicle.Create("Toyota", "Corolla", 1899, "Petrol"));

    [Fact]
    public void Vehicle_ActivateDeactivate_ShouldToggle()
    {
        var v = Vehicle.Create("Toyota", "Corolla", 2020, "Petrol");
        v.Deactivate();
        Assert.False(v.IsActive);
        v.Activate();
        Assert.True(v.IsActive);
    }

    [Fact]
    public void Vehicle_Update_ShouldModify()
    {
        var v = Vehicle.Create("Old", "Old", 2020, "Petrol");
        v.Update("New", "New", 2023, "Diesel", "Updated", false);
        Assert.Equal("New", v.Make);
        Assert.Equal("Diesel", v.EngineType);
        Assert.False(v.IsActive);
    }

    // ─── PartVehicleCompatibility ─────────────────────────────────

    [Fact]
    public void PartVehicleCompatibility_Create_Valid_ShouldSetProperties()
    {
        var pvc = PartVehicleCompatibility.Create(Guid.NewGuid(), Guid.NewGuid(), false, "Not compatible");
        Assert.Equal("Not compatible", pvc.Notes);
        Assert.False(pvc.IsCompatible);
    }

    [Fact]
    public void PartVehicleCompatibility_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PartVehicleCompatibility.Create(Guid.Empty, Guid.NewGuid()));

    [Fact]
    public void PartVehicleCompatibility_Create_EmptyVehicleId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PartVehicleCompatibility.Create(Guid.NewGuid(), Guid.Empty));

    [Fact]
    public void PartVehicleCompatibility_Create_SameIds_Throws()
    {
        var id = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            PartVehicleCompatibility.Create(id, id));
    }

    [Fact]
    public void PartVehicleCompatibility_Update_ShouldModify()
    {
        var pvc = PartVehicleCompatibility.Create(Guid.NewGuid(), Guid.NewGuid());
        pvc.Update(false, "Updated notes");
        Assert.False(pvc.IsCompatible);
        Assert.Equal("Updated notes", pvc.Notes);
    }

    // ─── ProductAttributeGroup ────────────────────────────────────

    [Fact]
    public void ProductAttributeGroup_Create_Valid_ShouldSetProperties()
    {
        var g = ProductAttributeGroup.Create("Engine Specs", 1);
        Assert.Equal("Engine Specs", g.Name);
        Assert.Equal(1, g.SortOrder);
        Assert.True(g.IsActive);
    }

    [Fact]
    public void ProductAttributeGroup_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => ProductAttributeGroup.Create(""));

    [Fact]
    public void ProductAttributeGroup_Create_NegativeSortOrder_ClampsToZero()
    {
        var g = ProductAttributeGroup.Create("Test", -5);
        Assert.Equal(0, g.SortOrder);
    }

    [Fact]
    public void ProductAttributeGroup_Update_ShouldModify()
    {
        var g = ProductAttributeGroup.Create("Old", 1);
        g.Update("New", 5, false);
        Assert.Equal("New", g.Name);
        Assert.Equal(5, g.SortOrder);
        Assert.False(g.IsActive);
    }

    // ─── ProductAttribute ─────────────────────────────────────────

    [Fact]
    public void ProductAttribute_Create_Valid_ShouldSetProperties()
    {
        var a = ProductAttribute.Create(Guid.NewGuid(), "RAM Size", "RAM", "option", "GB");
        Assert.Equal("RAM Size", a.Name);
        Assert.Equal("RAM", a.Code);
        Assert.Equal("option", a.DataType);
        Assert.Equal("GB", a.Unit);
    }

    [Fact]
    public void ProductAttribute_Create_EmptyGroupId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductAttribute.Create(Guid.Empty, "Name", "CODE"));

    [Fact]
    public void ProductAttribute_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductAttribute.Create(Guid.NewGuid(), "", "CODE"));

    [Fact]
    public void ProductAttribute_Create_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductAttribute.Create(Guid.NewGuid(), "Name", ""));

    [Fact]
    public void ProductAttribute_Update_ShouldModify()
    {
        var a = ProductAttribute.Create(Guid.NewGuid(), "Old", "OLD", "text");
        a.Update("New", "NEW", "number", "cm", false);
        Assert.Equal("New", a.Name);
        Assert.Equal("NEW", a.Code);
        Assert.Equal("number", a.DataType);
        Assert.False(a.IsActive);
    }

    // ─── ProductAttributeOption ───────────────────────────────────

    [Fact]
    public void ProductAttributeOption_Create_Valid_ShouldSetProperties()
    {
        var o = ProductAttributeOption.Create(Guid.NewGuid(), "8GB", 1);
        Assert.Equal("8GB", o.Value);
        Assert.Equal(1, o.SortOrder);
    }

    [Fact]
    public void ProductAttributeOption_Create_EmptyAttributeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductAttributeOption.Create(Guid.Empty, "Value"));

    [Fact]
    public void ProductAttributeOption_Create_EmptyValue_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductAttributeOption.Create(Guid.NewGuid(), ""));

    [Fact]
    public void ProductAttributeOption_Create_NegativeSortOrder_ClampsToZero()
    {
        var o = ProductAttributeOption.Create(Guid.NewGuid(), "Val", -1);
        Assert.Equal(0, o.SortOrder);
    }

    [Fact]
    public void ProductAttributeOption_Update_ShouldModify()
    {
        var o = ProductAttributeOption.Create(Guid.NewGuid(), "Old", 1);
        o.Update("New", 5);
        Assert.Equal("New", o.Value);
        Assert.Equal(5, o.SortOrder);
    }

    // ─── CategoryAttribute ────────────────────────────────────────

    [Fact]
    public void CategoryAttribute_Create_Valid_ShouldSetProperties()
    {
        var ca = CategoryAttribute.Create(Guid.NewGuid(), Guid.NewGuid(), true, false, "range", 2);
        Assert.True(ca.IsRequired);
        Assert.False(ca.IsFilterable);
        Assert.Equal("range", ca.FilterType);
        Assert.Equal(2, ca.SortOrder);
    }

    [Fact]
    public void CategoryAttribute_Create_EmptyCategoryId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CategoryAttribute.Create(Guid.Empty, Guid.NewGuid()));

    [Fact]
    public void CategoryAttribute_Create_EmptyAttributeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CategoryAttribute.Create(Guid.NewGuid(), Guid.Empty));

    [Fact]
    public void CategoryAttribute_Update_ShouldModify()
    {
        var ca = CategoryAttribute.Create(Guid.NewGuid(), Guid.NewGuid());
        ca.Update(true, false, "multi", 3);
        Assert.True(ca.IsRequired);
        Assert.False(ca.IsFilterable);
        Assert.Equal("multi", ca.FilterType);
        Assert.Equal(3, ca.SortOrder);
    }

    // ─── VariantAttributeValue ────────────────────────────────────

    [Fact]
    public void VariantAttributeValue_Create_Valid_ShouldSetProperties()
    {
        var vav = VariantAttributeValue.Create(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Red", null, null);
        Assert.Equal("Red", vav.ValueText);
        Assert.NotNull(vav.OptionId);
    }

    [Fact]
    public void VariantAttributeValue_Create_EmptyVariantId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            VariantAttributeValue.Create(Guid.Empty, Guid.NewGuid()));

    [Fact]
    public void VariantAttributeValue_Create_EmptyAttributeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            VariantAttributeValue.Create(Guid.NewGuid(), Guid.Empty));

    [Fact]
    public void VariantAttributeValue_Create_WithNumericValue_ShouldSet()
    {
        var vav = VariantAttributeValue.Create(Guid.NewGuid(), Guid.NewGuid(), null, "", 15.5m, null);
        Assert.Equal(15.5m, vav.ValueNumber);
    }

    [Fact]
    public void VariantAttributeValue_Create_WithBoolValue_ShouldSet()
    {
        var vav = VariantAttributeValue.Create(Guid.NewGuid(), Guid.NewGuid(), null, "", null, true);
        Assert.True(vav.ValueBool);
    }

    [Fact]
    public void VariantAttributeValue_Update_ShouldModify()
    {
        var vav = VariantAttributeValue.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Old", null, null);
        vav.Update(Guid.NewGuid(), "New", 10m, false);
        Assert.Equal("New", vav.ValueText);
        Assert.Equal(10m, vav.ValueNumber);
        Assert.False(vav.ValueBool);
    }

    // ─── CompatibilityRule ────────────────────────────────────────

    [Fact]
    public void CompatibilityRule_Create_Valid_ShouldSetProperties()
    {
        var cr = CompatibilityRule.Create("Part", Guid.NewGuid(), "Variant", Guid.NewGuid(), false, "Not compatible");
        Assert.Equal("Part", cr.SourceType);
        Assert.Equal("Variant", cr.TargetType);
        Assert.False(cr.IsCompatible);
    }

    [Fact]
    public void CompatibilityRule_Create_EmptySourceType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CompatibilityRule.Create("", Guid.NewGuid(), "Variant", Guid.NewGuid()));

    [Fact]
    public void CompatibilityRule_Create_EmptyTargetType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CompatibilityRule.Create("Part", Guid.NewGuid(), "", Guid.NewGuid()));

    [Fact]
    public void CompatibilityRule_Create_EmptySourceId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CompatibilityRule.Create("Part", Guid.Empty, "Variant", Guid.NewGuid()));

    [Fact]
    public void CompatibilityRule_Create_EmptyTargetId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            CompatibilityRule.Create("Part", Guid.NewGuid(), "Variant", Guid.Empty));

    [Fact]
    public void CompatibilityRule_Update_ShouldModify()
    {
        var cr = CompatibilityRule.Create("Part", Guid.NewGuid(), "Part", Guid.NewGuid());
        cr.Update(false, "New notes");
        Assert.False(cr.IsCompatible);
        Assert.Equal("New notes", cr.Notes);
    }

    // ─── ProductCatalogEntry ──────────────────────────────────────

    [Fact]
    public void ProductCatalogEntry_Create_Valid_ShouldSetProperties()
    {
        var pce = ProductCatalogEntry.Create(Guid.NewGuid(), "engine-oil-10w40",
            "High quality engine oil", true, DateTime.Today, true, 1, 95m,
            "https://img.test/oil.jpg", "https://vid.test/oil.mp4",
            "Engine Oil 10W40", "Best engine oil for your car");
        Assert.Equal("engine-oil-10w40", pce.Slug);
        Assert.True(pce.IsPublished);
        Assert.True(pce.IsFeatured);
        Assert.Equal(1, pce.FeaturedRank);
        Assert.Equal(95m, pce.PopularityScore);
    }

    [Fact]
    public void ProductCatalogEntry_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductCatalogEntry.Create(Guid.Empty, "slug"));

    [Fact]
    public void ProductCatalogEntry_Create_EmptySlug_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductCatalogEntry.Create(Guid.NewGuid(), ""));

    [Fact]
    public void ProductCatalogEntry_UpdateListing_ShouldModify()
    {
        var pce = ProductCatalogEntry.Create(Guid.NewGuid(), "old-slug");
        pce.UpdateListing("new-slug", "New desc", false, null, true, 5, 80m,
            "https://img.new/img.jpg", "https://vid.new/vid.mp4",
            "New Title", "New meta desc");
        Assert.Equal("new-slug", pce.Slug);
        Assert.False(pce.IsPublished);
        Assert.True(pce.IsFeatured);
        Assert.Equal(5, pce.FeaturedRank);
        Assert.Equal(80m, pce.PopularityScore);
    }

    // ─── ProductVariantPriceHistory ───────────────────────────────

    [Fact]
    public void ProductVariantPriceHistory_Create_Valid_ShouldSetProperties()
    {
        var pph = ProductVariantPriceHistory.Create(Guid.NewGuid(), 1500m, DateTime.Today,
            Guid.NewGuid(), "BDT", "Price increase");
        Assert.Equal(1500m, pph.SellingPrice);
        Assert.Equal("BDT", pph.Currency);
        Assert.Null(pph.EndDate);
        Assert.True(pph.IsCurrentlyActive);
    }

    [Fact]
    public void ProductVariantPriceHistory_Create_EmptyPartId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductVariantPriceHistory.Create(Guid.Empty, 100m, DateTime.Today));

    [Fact]
    public void ProductVariantPriceHistory_Create_ZeroPrice_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductVariantPriceHistory.Create(Guid.NewGuid(), 0, DateTime.Today));

    [Fact]
    public void ProductVariantPriceHistory_Close_ShouldSetEndDate()
    {
        var pph = ProductVariantPriceHistory.Create(Guid.NewGuid(), 1500m, DateTime.Today);
        pph.Close(DateTime.Today.AddMonths(3));
        Assert.NotNull(pph.EndDate);
        Assert.False(pph.IsCurrentlyActive);
    }

    [Fact]
    public void ProductVariantPriceHistory_Close_AlreadyClosed_Throws()
    {
        var pph = ProductVariantPriceHistory.Create(Guid.NewGuid(), 1500m, DateTime.Today);
        pph.Close(DateTime.Today.AddMonths(1));
        Assert.Throws<InvalidOperationException>(() => pph.Close(DateTime.Today.AddMonths(2)));
    }

    [Fact]
    public void ProductVariantPriceHistory_Close_EndBeforeStart_Throws()
    {
        var pph = ProductVariantPriceHistory.Create(Guid.NewGuid(), 1500m, DateTime.Today);
        Assert.Throws<ArgumentException>(() => pph.Close(DateTime.Today.AddDays(-1)));
    }

    [Fact]
    public void ProductVariantPriceHistory_IsActiveOn_ShouldCheck()
    {
        var pph = ProductVariantPriceHistory.Create(Guid.NewGuid(), 1500m, DateTime.Today);
        Assert.True(pph.IsActiveOn(DateTime.Today));
        Assert.True(pph.IsActiveOn(DateTime.Today.AddMonths(6)));
        pph.Close(DateTime.Today.AddMonths(3));
        Assert.True(pph.IsActiveOn(DateTime.Today.AddMonths(2)));
        Assert.False(pph.IsActiveOn(DateTime.Today.AddMonths(4)));
        Assert.False(pph.IsActiveOn(DateTime.Today.AddDays(-1)));
    }

    // ─── Technician ───────────────────────────────────────────────

    [Fact]
    public void Technician_Create_Valid_ShouldSetProperties()
    {
        var t = Technician.Create("TECH-001", "John Smith", "+8801711111111",
            "john@repair.com", "Smith Repair", "123 Main", "Dhaka", "Good tech");
        Assert.Equal("TECH-001", t.TechnicianCode);
        Assert.Equal("John Smith", t.Name);
        Assert.Equal("ACTIVE", t.Status);
    }

    [Fact]
    public void Technician_Create_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Technician.Create("", "John", "123"));

    [Fact]
    public void Technician_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Technician.Create("T001", "", "123"));

    [Fact]
    public void Technician_Create_EmptyPhone_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Technician.Create("T001", "John", ""));

    [Fact]
    public void Technician_ActivateDeactivate_ShouldToggle()
    {
        var t = Technician.Create("T001", "John", "123");
        t.Deactivate();
        Assert.Equal("INACTIVE", t.Status);
        t.Activate();
        Assert.Equal("ACTIVE", t.Status);
    }

    [Fact]
    public void Technician_UpdateInfo_ShouldModify()
    {
        var t = Technician.Create("T001", "John", "123");
        t.UpdateInfo("Jane", "456", "j@b.com", "Shop", "Addr", "City", "Notes");
        Assert.Equal("Jane", t.Name);
        Assert.Equal("456", t.Phone);
    }

    // ─── ProductEmbedding ─────────────────────────────────────────

    [Fact]
    public void ProductEmbedding_Create_Valid_ShouldSetProperties()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var pe = ProductEmbedding.Create(Guid.NewGuid(), vector, "text-embedding-3",
            "Engine oil 10W40", "OIL-10W40", "OEM-123");
        Assert.Equal("text-embedding-3", pe.Model);
        Assert.Equal(3, pe.Dimensions);
        Assert.Equal("OIL-10W40", pe.PartNumber);
        Assert.Equal("OEM-123", pe.OemNumber);
    }

    [Fact]
    public void ProductEmbedding_Create_EmptyProductId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductEmbedding.Create(Guid.Empty, [1f], "m", "s", "p", null));

    [Fact]
    public void ProductEmbedding_Create_EmptyVector_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            ProductEmbedding.Create(Guid.NewGuid(), [], "m", "s", "p", null));

    [Fact]
    public void ProductEmbedding_Update_ShouldModify()
    {
        var pe = ProductEmbedding.Create(Guid.NewGuid(), [1f], "m1", "src1", "PN1", "OEM1");
        pe.Update([2f, 3f], "m2", "src2", "PN2", "OEM2");
        Assert.Equal("m2", pe.Model);
        Assert.Equal(2, pe.Dimensions);
        Assert.Equal("PN2", pe.PartNumber);
    }

    // ─── WarrantyRegistration ─────────────────────────────────────

    private static WarrantyRegistration CreateWarranty() =>
        WarrantyRegistration.Create("WR-2025-00001", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.Today.AddDays(-90), DateTime.Today, 12, "MANUFACTURER",
            "Standard 12-month warranty", "CERT-001");

    [Fact]
    public void WarrantyRegistration_Create_Valid_ShouldSetProperties()
    {
        var wr = CreateWarranty();
        Assert.Equal("WR-2025-00001", wr.WarrantyNumber);
        Assert.Equal("ACTIVE", wr.Status);
        Assert.Equal("MANUFACTURER", wr.WarrantyType);
        Assert.Equal(12, wr.WarrantyPeriodMonths);
        Assert.True(wr.IsValid());
    }

    [Fact]
    public void WarrantyRegistration_Create_EmptyWarrantyNumber_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyRegistration.Create("", Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, DateTime.Today,
                12, "MANUFACTURER", "Terms", "CERT"));

    [Fact]
    public void WarrantyRegistration_Create_InvalidWarrantyType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyRegistration.Create("WR-001", Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, DateTime.Today,
                12, "INVALID", "Terms", "CERT"));

    [Fact]
    public void WarrantyRegistration_Create_ZeroPeriod_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyRegistration.Create("WR-001", Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, DateTime.Today,
                0, "MANUFACTURER", "Terms", "CERT"));

    [Fact]
    public void WarrantyRegistration_Void_ShouldTransition()
    {
        var wr = CreateWarranty();
        wr.Void("Customer returned");
        Assert.Equal("VOID", wr.Status);
        Assert.Equal("Customer returned", wr.VoidReason);
    }

    [Fact]
    public void WarrantyRegistration_Void_AlreadyVoided_Throws()
    {
        var wr = CreateWarranty();
        wr.Void("reason");
        Assert.Throws<InvalidOperationException>(() => wr.Void("again"));
    }

    [Fact]
    public void WarrantyRegistration_Void_EmptyReason_Throws() =>
        Assert.Throws<ArgumentException>(() => CreateWarranty().Void(""));

    [Fact]
    public void WarrantyRegistration_MarkAsClaimed_ShouldTransition()
    {
        var wr = CreateWarranty();
        wr.MarkAsClaimed();
        Assert.Equal("CLAIMED", wr.Status);
    }

    [Fact]
    public void WarrantyRegistration_MarkAsClaimed_Voided_Throws()
    {
        var wr = CreateWarranty();
        wr.Void("reason");
        Assert.Throws<InvalidOperationException>(() => wr.MarkAsClaimed());
    }

    [Fact]
    public void WarrantyRegistration_MarkAsClaimed_Expired_Throws()
    {
        var wr = WarrantyRegistration.Create("WR-001", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-2), 12,
            "MANUFACTURER", "Terms", "CERT");
        wr.CheckAndUpdateExpiry();
        Assert.Throws<InvalidOperationException>(() => wr.MarkAsClaimed());
    }

    [Fact]
    public void WarrantyRegistration_CheckAndUpdateExpiry_ShouldExpire()
    {
        var wr = WarrantyRegistration.Create("WR-001", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-2), 12,
            "MANUFACTURER", "Terms", "CERT");
        wr.CheckAndUpdateExpiry();
        Assert.Equal("EXPIRED", wr.Status);
        Assert.False(wr.IsValid());
    }

    [Fact]
    public void WarrantyRegistration_ReactivateAfterClaimRejection_ActiveWarranty_ShouldRevert()
    {
        var wr = CreateWarranty();
        wr.MarkAsClaimed();
        wr.ReactivateAfterClaimRejection();
        Assert.Equal("ACTIVE", wr.Status);
    }

    [Fact]
    public void WarrantyRegistration_ReactivateAfterClaimRejection_ExpiredWarranty_ShouldExpire()
    {
        var wr = WarrantyRegistration.Create("WR-001", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-2), 12,
            "MANUFACTURER", "Terms", "CERT");
        wr.CheckAndUpdateExpiry();
        Assert.Equal("EXPIRED", wr.Status);
    }

    [Fact]
    public void WarrantyRegistration_ReactivateAfterClaimClosure_ShouldReactivate()
    {
        var wr = CreateWarranty();
        wr.MarkAsClaimed();
        wr.ReactivateAfterClaimClosure();
        Assert.Equal("ACTIVE", wr.Status);
    }

    [Fact]
    public void WarrantyRegistration_SyncFromPartWarranty_Disable_ShouldVoid()
    {
        var wr = CreateWarranty();
        wr.SyncFromPartWarranty(false, null, null, null, null, "admin");
        Assert.Equal("VOID", wr.Status);
        Assert.Equal("Part warranty was disabled", wr.VoidReason);
    }

    [Fact]
    public void WarrantyRegistration_SyncFromPartWarranty_Enable_ShouldUpdate()
    {
        var wr = CreateWarranty();
        wr.SyncFromPartWarranty(true, 24, "SELLER", "Extended terms", "CERT-TPL", "admin");
        Assert.Equal(24, wr.WarrantyPeriodMonths);
        Assert.Equal("SELLER", wr.WarrantyType);
        Assert.Equal("CERT-TPL-WR-2025-00001", wr.CertificateNumber);
    }

    [Fact]
    public void WarrantyRegistration_SyncFromPartWarranty_Claimed_ShouldSkip()
    {
        var wr = CreateWarranty();
        wr.MarkAsClaimed();
        wr.SyncFromPartWarranty(false, null, null, null, null, "admin");
        Assert.Equal("CLAIMED", wr.Status);
    }

    // ─── WarrantyClaim ────────────────────────────────────────────

    private static WarrantyClaim CreateClaim() =>
        WarrantyClaim.Create("WC-2025-00001", Guid.NewGuid(), Guid.NewGuid(),
            DateTime.Today, "Engine noise", "REPAIR");

    [Fact]
    public void WarrantyClaim_Create_Valid_ShouldSetProperties()
    {
        var wc = CreateClaim();
        Assert.Equal("WC-2025-00001", wc.ClaimNumber);
        Assert.Equal("PENDING", wc.Status);
        Assert.Equal("REPAIR", wc.ServiceType);
        Assert.True(wc.IsOpen());
        Assert.True(wc.CanBeModified());
    }

    [Fact]
    public void WarrantyClaim_Create_EmptyClaimNumber_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaim.Create("", Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "Issue", "REPAIR"));

    [Fact]
    public void WarrantyClaim_Create_InvalidServiceType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaim.Create("WC-001", Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "Issue", "INVALID"));

    [Fact]
    public void WarrantyClaim_Create_EmptyIssue_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaim.Create("WC-001", Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "", "REPAIR"));

    [Fact]
    public void WarrantyClaim_SubmitForReview_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        Assert.Equal("UNDER_REVIEW", wc.Status);
        Assert.True(wc.CanBeModified());
    }

    [Fact]
    public void WarrantyClaim_SubmitForReview_NonPending_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        Assert.Throws<InvalidOperationException>(() => wc.SubmitForReview());
    }

    [Fact]
    public void WarrantyClaim_Approve_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Manager");
        Assert.Equal("APPROVED", wc.Status);
        Assert.NotNull(wc.ApprovedDate);
        Assert.Equal("Manager", wc.ApprovedBy);
    }

    [Fact]
    public void WarrantyClaim_Approve_NonUnderReview_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<InvalidOperationException>(() => wc.Approve("Mgr"));
    }

    [Fact]
    public void WarrantyClaim_Approve_EmptyApprover_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        Assert.Throws<ArgumentException>(() => wc.Approve(""));
    }

    [Fact]
    public void WarrantyClaim_Reject_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.Reject("Out of warranty", "Manager");
        Assert.Equal("REJECTED", wc.Status);
        Assert.Equal("Out of warranty", wc.RejectionReason);
        Assert.False(wc.IsOpen());
    }

    [Fact]
    public void WarrantyClaim_Reject_AlreadyRejected_Throws()
    {
        var wc = CreateClaim();
        wc.Reject("reason", "Mgr");
        Assert.Throws<InvalidOperationException>(() => wc.Reject("again", "Mgr"));
    }

    [Fact]
    public void WarrantyClaim_Reject_Completed_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        wc.Complete("Fixed");
        Assert.Throws<InvalidOperationException>(() => wc.Reject("reason", "Mgr"));
    }

    [Fact]
    public void WarrantyClaim_Reject_EmptyReason_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<ArgumentException>(() => wc.Reject("", "Mgr"));
    }

    [Fact]
    public void WarrantyClaim_AssignTechnician_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        var techId = Guid.NewGuid();
        wc.AssignTechnician(techId);
        Assert.Equal("IN_PROGRESS", wc.Status);
        Assert.Equal(techId, wc.TechnicianId);
        Assert.NotNull(wc.ServiceStartDate);
    }

    [Fact]
    public void WarrantyClaim_AssignTechnician_NonApproved_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<InvalidOperationException>(() =>
            wc.AssignTechnician(Guid.NewGuid()));
    }

    [Fact]
    public void WarrantyClaim_AssignTechnician_EmptyId_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        Assert.Throws<ArgumentException>(() => wc.AssignTechnician(Guid.Empty));
    }

    [Fact]
    public void WarrantyClaim_StartServiceWithoutTechnician_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.StartServiceWithoutTechnician();
        Assert.Equal("IN_PROGRESS", wc.Status);
    }

    [Fact]
    public void WarrantyClaim_UpdateServiceCost_ShouldSet()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        wc.UpdateServiceCost(500m, "Parts replaced");
        Assert.Equal(500m, wc.ServiceCost);
        Assert.Equal("Parts replaced", wc.ServiceNotes);
    }

    [Fact]
    public void WarrantyClaim_UpdateServiceCost_Negative_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<ArgumentException>(() => wc.UpdateServiceCost(-1m));
    }

    [Fact]
    public void WarrantyClaim_UpdateServiceCost_Closed_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        wc.Complete("Done");
        wc.Close();
        Assert.Throws<InvalidOperationException>(() => wc.UpdateServiceCost(100m));
    }

    [Fact]
    public void WarrantyClaim_Complete_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        wc.Complete("Replaced faulty part");
        Assert.Equal("COMPLETED", wc.Status);
        Assert.NotNull(wc.ServiceCompletedDate);
        Assert.Equal("Replaced faulty part", wc.ResolutionDetails);
    }

    [Fact]
    public void WarrantyClaim_Complete_NonInProgress_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<InvalidOperationException>(() => wc.Complete("Done"));
    }

    [Fact]
    public void WarrantyClaim_Complete_EmptyResolution_Throws()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => wc.Complete(""));
    }

    [Fact]
    public void WarrantyClaim_Close_ShouldTransition()
    {
        var wc = CreateClaim();
        wc.SubmitForReview();
        wc.Approve("Mgr");
        wc.AssignTechnician(Guid.NewGuid());
        wc.Complete("Fixed");
        wc.Close("Customer satisfied");
        Assert.Equal("CLOSED", wc.Status);
        Assert.False(wc.IsOpen());
    }

    [Fact]
    public void WarrantyClaim_Close_FromRejected_ShouldWork()
    {
        var wc = CreateClaim();
        wc.Reject("Out of warranty", "Mgr");
        wc.Close();
        Assert.Equal("CLOSED", wc.Status);
    }

    [Fact]
    public void WarrantyClaim_Close_NonCompletedOrRejected_Throws()
    {
        var wc = CreateClaim();
        Assert.Throws<InvalidOperationException>(() => wc.Close());
    }

    [Fact]
    public void WarrantyClaim_FullLifecycle_ShouldWork()
    {
        var wc = CreateClaim();
        Assert.Equal("PENDING", wc.Status);
        wc.SubmitForReview();
        Assert.Equal("UNDER_REVIEW", wc.Status);
        wc.Approve("Manager");
        Assert.Equal("APPROVED", wc.Status);
        wc.AssignTechnician(Guid.NewGuid());
        Assert.Equal("IN_PROGRESS", wc.Status);
        wc.UpdateServiceCost(300m);
        wc.Complete("Replaced unit");
        Assert.Equal("COMPLETED", wc.Status);
        wc.Close("Claim resolved");
        Assert.Equal("CLOSED", wc.Status);
        Assert.False(wc.IsOpen());
        Assert.False(wc.CanBeModified());
    }

    // ─── WarrantyClaimEvent ───────────────────────────────────────

    [Fact]
    public void WarrantyClaimEvent_Create_Valid_ShouldSetProperties()
    {
        var wce = WarrantyClaimEvent.Create(Guid.NewGuid(), "SENT_FOR_REPAIR",
            "MANUFACTURER", "ABC Parts Co", "Staff-01", "REF-123",
            DateTime.Today.AddDays(14), "Sent via courier", "admin");
        Assert.Equal("SENT_FOR_REPAIR", wce.EventType);
        Assert.Equal("MANUFACTURER", wce.PartnerType);
        Assert.Equal("ABC Parts Co", wce.PartnerName);
        Assert.Equal("REF-123", wce.ReferenceNumber);
    }

    [Fact]
    public void WarrantyClaimEvent_Create_EmptyClaimId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaimEvent.Create(Guid.Empty, "SENT_FOR_REPAIR", "MFG", "Partner",
                "Staff", null, null, null, "admin"));

    [Fact]
    public void WarrantyClaimEvent_Create_EmptyEventType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaimEvent.Create(Guid.NewGuid(), "", "MFG", "Partner",
                "Staff", null, null, null, "admin"));

    [Fact]
    public void WarrantyClaimEvent_Create_EmptyPartnerName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaimEvent.Create(Guid.NewGuid(), "SENT_FOR_REPAIR", "MFG", "",
                "Staff", null, null, null, "admin"));

    [Fact]
    public void WarrantyClaimEvent_Create_EmptyResponsibleBy_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            WarrantyClaimEvent.Create(Guid.NewGuid(), "SENT_FOR_REPAIR", "MFG", "Partner",
                "", null, null, null, "admin"));
}
