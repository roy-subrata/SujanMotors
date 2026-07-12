using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class CatalogEntitySmokeTests
{
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _brandId = Guid.NewGuid();
    private readonly Guid _baseUnitId = Guid.NewGuid();

    // ── PartNumber tests ─────────────────────────────────────────────────────

    [Fact]
    public void PartNumber_Create_Valid_Succeeds()
    {
        var pn = PartNumber.Create("OIL-001");
        Assert.Equal("OIL-001", pn.Value);
    }

    [Fact]
    public void PartNumber_Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => PartNumber.Create(""));
    }

    [Fact]
    public void PartNumber_Create_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => PartNumber.Create("AB"));
    }

    [Fact]
    public void PartNumber_Create_MustStartWithLetter_Throws()
    {
        Assert.Throws<ArgumentException>(() => PartNumber.Create("12345"));
    }

    [Fact]
    public void PartNumber_Equality_IsCaseInsensitive()
    {
        var a = PartNumber.Create("OIL-001");
        var b = PartNumber.Create("oil-001");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── Product tests ────────────────────────────────────────────────────────

    [Fact]
    public void Product_Create_Valid_ShouldSetProperties()
    {
        var pn = PartNumber.Create("BRK-001");
        var p = Product.Create("Brake Pad", pn, "BRK-001-SKU", _categoryId,
            brandId: _brandId, baseUnitId: _baseUnitId,
            costPrice: 50m, sellingPrice: 120m, minimumStock: 5,
            hasWarranty: true, warrantyPeriodMonths: 12, warrantyType: "MANUFACTURER",
            barcode: "123456789", productType: "PHYSICAL");

        Assert.Equal("Brake Pad", p.Name);
        Assert.Equal("BRK-001-SKU", p.SKU);
        Assert.Equal(_categoryId, p.CategoryId);
        Assert.Equal(_brandId, p.BrandId);
        Assert.Equal(_baseUnitId, p.BaseUnitId);
        Assert.Equal(50m, p.CostPrice);
        Assert.Equal(120m, p.SellingPrice);
        Assert.Equal(5, p.MinimumStock);
        Assert.True(p.HasWarranty);
        Assert.Equal(12, p.WarrantyPeriodMonths);
        Assert.Equal("MANUFACTURER", p.WarrantyType);
        Assert.Equal("123456789", p.Barcode);
        Assert.Equal("PHYSICAL", p.ProductType);
        Assert.True(p.IsActive);
    }

    [Fact]
    public void Product_Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("", PartNumber.Create("X-001"), "SKU", _categoryId));
    }

    [Fact]
    public void Product_Create_NullPartNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Product.Create("Test", null!, "SKU", _categoryId));
    }

    [Fact]
    public void Product_Create_EmptySku_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "", _categoryId));
    }

    [Fact]
    public void Product_Create_EmptyCategoryId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", Guid.Empty));
    }

    [Fact]
    public void Product_Create_NegativeCostPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId, costPrice: -1));
    }

    [Fact]
    public void Product_Create_NegativeSellingPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId, sellingPrice: -1));
    }

    [Fact]
    public void Product_Create_NegativeMinimumStock_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId, minimumStock: -1));
    }

    [Fact]
    public void Product_Create_NegativeWeight_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId, weightKg: -1m));
    }

    [Fact]
    public void Product_Create_WarrantyWithoutPeriod_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId,
                hasWarranty: true, warrantyType: "MANUFACTURER"));
    }

    [Fact]
    public void Product_Create_WarrantyWithoutType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId,
                hasWarranty: true, warrantyPeriodMonths: 12));
    }

    [Fact]
    public void Product_Create_InvalidProductType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId, productType: "INVALID"));
    }

    [Fact]
    public void Product_Create_AllProductTypes_ShouldSet()
    {
        var pn = PartNumber.Create("X-001");
        foreach (var t in new[] { "PHYSICAL", "DIGITAL", "SERVICE" })
        {
            var p = Product.Create("Test", pn, "SKU-" + t, _categoryId, productType: t);
            Assert.Equal(t, p.ProductType);
        }
    }

    [Fact]
    public void Product_Create_NameLengthExceeds200_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create(new string('A', 201), PartNumber.Create("X-001"), "SKU", _categoryId));
    }

    [Fact]
    public void Product_Create_SkuLengthExceeds100_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("Test", PartNumber.Create("X-001"), new string('S', 101), _categoryId));
    }

    [Fact]
    public void Product_Update_ValidatesConstraints()
    {
        var p = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId);
        Assert.Throws<ArgumentException>(() =>
            p.Update("", "desc", "SKU", _categoryId, null, null, null, 0, 0, 0, true));
    }

    [Fact]
    public void Product_UpdateSellingPrice_Negative_Throws()
    {
        var p = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId);
        Assert.Throws<ArgumentException>(() => p.UpdateSellingPrice(-1));
    }

    [Fact]
    public void Product_UpdateSellingPrice_ShouldSet()
    {
        var p = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId);
        p.UpdateSellingPrice(150m, "USD");
        Assert.Equal(150m, p.SellingPrice);
        Assert.Equal("USD", p.SellingPriceCurrency);
    }

    [Fact]
    public void Product_ActivateDeactivate_ShouldToggle()
    {
        var p = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId);
        Assert.True(p.IsActive);
        p.Deactivate();
        Assert.False(p.IsActive);
        p.Activate();
        Assert.True(p.IsActive);
    }

    // ── ProductVariant tests ─────────────────────────────────────────────────

    [Fact]
    public void ProductVariant_Create_Valid_ShouldSetProperties()
    {
        var partId = Guid.NewGuid();
        var v = ProductVariant.Create(partId, "Red", "RED", 60m, 140m,
            sku: "BRK-001-RED", barcode: "BARCODE", currency: "BDT",
            weightKg: 0.5m, widthCm: 10m, heightCm: 5m, depthCm: 2m);

        Assert.Equal(partId, v.PartId);
        Assert.Equal("Red", v.Name);
        Assert.Equal("RED", v.Code);
        Assert.Equal(60m, v.CostPrice);
        Assert.Equal(140m, v.SellingPrice);
        Assert.Equal("BRK-001-RED", v.SKU);
        Assert.Equal("BARCODE", v.Barcode);
        Assert.Equal("BDT", v.Currency);
        Assert.True(v.IsActive);
        Assert.Equal("OVERRIDE", v.PricingMode);
    }

    [Fact]
    public void ProductVariant_Create_EmptyPartId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductVariant.Create(Guid.Empty, "Red", "RED", 60m, 140m));
    }

    [Fact]
    public void ProductVariant_Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductVariant.Create(Guid.NewGuid(), "", "RED", 60m, 140m));
    }

    [Fact]
    public void ProductVariant_Create_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductVariant.Create(Guid.NewGuid(), "Red", "", 60m, 140m));
    }

    [Fact]
    public void ProductVariant_Create_NegativeCostPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductVariant.Create(Guid.NewGuid(), "Red", "RED", -1, 140m));
    }

    [Fact]
    public void ProductVariant_Create_NegativeSellingPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, -1));
    }

    [Fact]
    public void ProductVariant_Update_ValidatesConstraints()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        Assert.Throws<ArgumentException>(() =>
            v.Update("", "BLUE", 60m, 140m));
    }

    [Fact]
    public void ProductVariant_UpdateSellingPrice_Negative_Throws()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        Assert.Throws<ArgumentException>(() => v.UpdateSellingPrice(-1));
    }

    [Fact]
    public void ProductVariant_UpdateSellingPrice_ShouldSet()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        v.UpdateSellingPrice(160m, "USD");
        Assert.Equal(160m, v.SellingPrice);
        Assert.Equal("USD", v.Currency);
    }

    [Fact]
    public void ProductVariant_SetWarrantyOverride_Valid_ShouldSet()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        v.SetWarrantyOverride(true, 24, "SELLER");
        Assert.True(v.HasWarrantyOverride);
        Assert.Equal(24, v.WarrantyPeriodMonthsOverride);
        Assert.Equal("SELLER", v.WarrantyTypeOverride);
    }

    [Fact]
    public void ProductVariant_SetWarrantyOverride_WithoutPeriod_Throws()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        Assert.Throws<ArgumentException>(() => v.SetWarrantyOverride(true, null, "SELLER"));
    }

    [Fact]
    public void ProductVariant_SetWarrantyOverride_InvalidType_Throws()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        Assert.Throws<ArgumentException>(() => v.SetWarrantyOverride(true, 12, "INVALID"));
    }

    [Fact]
    public void ProductVariant_SetWarrantyOverride_Disabled_ShouldClear()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        v.SetWarrantyOverride(true, 24, "SELLER");
        v.SetWarrantyOverride(false, null, null);
        Assert.False(v.HasWarrantyOverride);
        Assert.Null(v.WarrantyPeriodMonthsOverride);
        Assert.Null(v.WarrantyTypeOverride);
    }

    [Fact]
    public void ProductVariant_ClearWarrantyOverride_ShouldSetNull()
    {
        var v = ProductVariant.Create(Guid.NewGuid(), "Red", "RED", 60m, 140m);
        v.SetWarrantyOverride(true, 24, "SELLER");
        v.ClearWarrantyOverride();
        Assert.Null(v.HasWarrantyOverride);
    }

    [Fact]
    public void ProductVariant_ResolveWarranty_WithOverride_ReturnsOverride()
    {
        var part = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId,
            hasWarranty: true, warrantyPeriodMonths: 12, warrantyType: "MANUFACTURER");
        var v = ProductVariant.Create(part.Id, "Red", "RED", 60m, 140m);
        v.SetWarrantyOverride(true, 24, "SELLER");
        var (hasW, period, type) = v.ResolveWarranty(part);
        Assert.True(hasW);
        Assert.Equal(24, period);
        Assert.Equal("SELLER", type);
    }

    [Fact]
    public void ProductVariant_ResolveWarranty_WithoutOverride_InheritsFromPart()
    {
        var part = Product.Create("Test", PartNumber.Create("X-001"), "SKU", _categoryId,
            hasWarranty: true, warrantyPeriodMonths: 12, warrantyType: "MANUFACTURER");
        var v = ProductVariant.Create(part.Id, "Red", "RED", 60m, 140m);
        var (hasW, period, type) = v.ResolveWarranty(part);
        Assert.True(hasW);
        Assert.Equal(12, period);
        Assert.Equal("MANUFACTURER", type);
    }

    // ── Category tests ───────────────────────────────────────────────────────

    [Fact]
    public void Category_Create_Valid_ShouldSetProperties()
    {
        var c = Category.Create("Engine Parts", "Engine parts category", displayOrder: 1);
        Assert.Equal("Engine Parts", c.Name);
        Assert.Equal("Engine parts category", c.Description);
        Assert.Equal(1, c.DisplayOrder);
        Assert.True(c.IsActive);
        Assert.Equal(0, c.ChildCount);
        Assert.Equal(0, c.DepthLevel);
        Assert.Equal("Engine Parts", c.BreadcrumbPath);
    }

    [Fact]
    public void Category_Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create("", "desc"));
    }

    [Fact]
    public void Category_Create_NameExceeds100_Throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create(new string('A', 101), "desc"));
    }

    [Fact]
    public void Category_Create_NegativeDisplayOrder_Throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create("Test", "desc", displayOrder: -1));
    }

    [Fact]
    public void Category_Create_DepthExceedsMax_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Category.Create("Test", "desc", depthLevel: 8));
    }

    [Fact]
    public void Category_Create_WithParent_ShouldSetParent()
    {
        var parentId = Guid.NewGuid();
        var c = Category.Create("Sub Category", "desc", parentCategoryId: parentId,
            breadcrumbPath: "Root > Sub", depthLevel: 1);
        Assert.Equal(parentId, c.ParentCategoryId);
        Assert.Equal("Root > Sub", c.BreadcrumbPath);
        Assert.Equal(1, c.DepthLevel);
    }

    [Fact]
    public void Category_Update_ValidatesConstraints()
    {
        var c = Category.Create("Test", "desc");
        Assert.Throws<ArgumentException>(() => c.Update("", "desc", 0, true));
    }

    [Fact]
    public void Category_ActivateDeactivate_ShouldToggle()
    {
        var c = Category.Create("Test", "desc");
        Assert.True(c.IsActive);
        c.Deactivate();
        Assert.False(c.IsActive);
        c.Activate();
        Assert.True(c.IsActive);
    }

    [Fact]
    public void Category_UpdateBreadcrumbPath_Empty_Throws()
    {
        var c = Category.Create("Test", "desc");
        Assert.Throws<ArgumentException>(() => c.UpdateBreadcrumbPath(""));
    }

    [Fact]
    public void Category_UpdateBreadcrumbPath_ShouldSet()
    {
        var c = Category.Create("Test", "desc");
        c.UpdateBreadcrumbPath("Root > Category");
        Assert.Equal("Root > Category", c.BreadcrumbPath);
    }

    [Fact]
    public void Category_UpdateDepthLevel_ExceedsMax_Throws()
    {
        var c = Category.Create("Test", "desc");
        Assert.Throws<ArgumentException>(() => c.UpdateDepthLevel(8));
    }

    [Fact]
    public void Category_UpdateDepthLevel_ShouldSet()
    {
        var c = Category.Create("Test", "desc");
        c.UpdateDepthLevel(3);
        Assert.Equal(3, c.DepthLevel);
    }

    [Fact]
    public void Category_UpdateChildCount_Negative_Throws()
    {
        var c = Category.Create("Test", "desc");
        Assert.Throws<ArgumentException>(() => c.UpdateChildCount(-1));
    }

    [Fact]
    public void Category_IncrementDecrementChildCount_ShouldWork()
    {
        var c = Category.Create("Test", "desc");
        c.IncrementChildCount();
        Assert.Equal(1, c.ChildCount);
        c.DecrementChildCount();
        Assert.Equal(0, c.ChildCount);
        c.DecrementChildCount();
        Assert.Equal(0, c.ChildCount);
    }

    [Fact]
    public void Category_WouldCreateCircularReference_SelfReference_ReturnsTrue()
    {
        var c = Category.Create("Test", "desc");
        var all = new List<Category> { c };
        Assert.True(c.WouldCreateCircularReference(c.Id, all));
    }

    [Fact]
    public void Category_WouldCreateCircularReference_NullParent_ReturnsFalse()
    {
        var c = Category.Create("Test", "desc");
        var all = new List<Category> { c };
        Assert.False(c.WouldCreateCircularReference(null, all));
    }

    [Fact]
    public void Category_GetHierarchyPath_ShouldReturnPath()
    {
        var c = Category.Create("Test", "desc", breadcrumbPath: "Root > Sub > Test");
        var path = c.GetHierarchyPath().ToList();
        Assert.Equal(3, path.Count);
        Assert.Equal("Root", path[0]);
        Assert.Equal("Sub", path[1]);
        Assert.Equal("Test", path[2]);
    }

    // ── Brand tests ──────────────────────────────────────────────────────────

    [Fact]
    public void Brand_Create_Valid_ShouldSetProperties()
    {
        var b = Brand.Create("Bosch", "German auto parts", "Germany",
            logoUrl: "http://logo.com/bosch.png", website: "http://bosch.com",
            contactEmail: "info@bosch.com", contactPhone: "+49123456789",
            displayOrder: 1, isActive: true);

        Assert.Equal("Bosch", b.Name);
        Assert.Equal("German auto parts", b.Description);
        Assert.Equal("Germany", b.Country);
        Assert.Equal("http://logo.com/bosch.png", b.LogoUrl);
        Assert.Equal("http://bosch.com", b.Website);
        Assert.Equal("info@bosch.com", b.ContactEmail);
        Assert.Equal("+49123456789", b.ContactPhone);
        Assert.True(b.IsActive);
        Assert.Equal(1, b.DisplayOrder);
    }

    [Fact]
    public void Brand_Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Brand.Create(""));
    }

    [Fact]
    public void Brand_Create_NameExceeds100_Throws()
    {
        Assert.Throws<ArgumentException>(() => Brand.Create(new string('B', 101)));
    }

    [Fact]
    public void Brand_Update_EmptyName_Throws()
    {
        var b = Brand.Create("Bosch");
        Assert.Throws<ArgumentException>(() =>
            b.Update("", "desc", "", "", "", "", "", 0, true));
    }

    [Fact]
    public void Brand_SetLogo_ShouldUpdate()
    {
        var b = Brand.Create("Bosch");
        b.SetLogo("http://new-logo.png");
        Assert.Equal("http://new-logo.png", b.LogoUrl);
    }

    [Fact]
    public void Brand_SetWebsite_ShouldUpdate()
    {
        var b = Brand.Create("Bosch");
        b.SetWebsite("http://new-site.com");
        Assert.Equal("http://new-site.com", b.Website);
    }

    [Fact]
    public void Brand_ActivateDeactivate_ShouldToggle()
    {
        var b = Brand.Create("Bosch");
        Assert.True(b.IsActive);
        b.Deactivate();
        Assert.False(b.IsActive);
        b.Activate();
        Assert.True(b.IsActive);
    }
}
