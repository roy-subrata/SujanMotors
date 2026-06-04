using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Dev-only endpoint: wipes and re-seeds the e-commerce catalog.
/// Call POST /api/dev/seed-catalog to reset demo data.
/// Restricted to Admins and only available in the Development environment.
/// </summary>
[Route("api/dev")]
[Route("api/v1/dev")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DevSeedController(AutoPartDbContext _db, ICodeGenerateService _code, IHostEnvironment _env, ILogger<DevSeedController> _logger) : ControllerBase
{
    [HttpPost("seed-catalog")]
    public async Task<IActionResult> SeedCatalog(CancellationToken ct)
    {
        // Destructive operation — never expose outside Development.
        if (!_env.IsDevelopment())
            return NotFound();

        _logger.LogInformation("Dev seed: clearing and re-seeding catalog...");

        // ── Clear catalog data via raw SQL in correct FK order ───────────────
        var deletes = new[]
        {
            "DELETE FROM CartReservations",
            "DELETE FROM StockMovements",
            "DELETE FROM StockLotMovements",
            "DELETE FROM StockLots",
            "DELETE FROM StockLevels",
            "DELETE FROM WarrantyClaims",
            "DELETE FROM WarrantyRegistrations",
            "DELETE FROM Discounts",
            "DELETE FROM ProductCatalogEntries",
            "DELETE FROM VariantAttributeValues",
            "DELETE FROM ProductVariants",
            "DELETE FROM CategoryAttributes",
            "DELETE FROM PartVehicleCompatibilities",
            "DELETE FROM Parts",
            "DELETE FROM ProductAttributeOptions",
            "DELETE FROM ProductAttributes",
            "DELETE FROM ProductAttributeGroups",
            "DELETE FROM Vehicles",
            "DELETE FROM Brands",
            "DELETE FROM Warehouses",
            "DELETE FROM Units",
            "DELETE FROM Categories",
        };
        foreach (var sql in deletes)
            await _db.Database.ExecuteSqlRawAsync(sql, ct);

        // ── Units ────────────────────────────────────────────────────────────
        var pcs  = U("Piece",  "PCS",  "pcs");
        var ltr  = U("Litre",  "LTR",  "L");
        var set  = U("Set",    "SET",  "set");
        var pair = U("Pair",   "PAIR", "pair");
        _db.Units.AddRange(pcs, ltr, set, pair);
        await _db.SaveChangesAsync(ct);

        // ── Warehouse ────────────────────────────────────────────────────────
        var wh = Warehouse.Create("Main Warehouse", "WH-001", "House 12, Road 4, Mirpur", "Dhaka", "Dhaka Division", "Bangladesh", "1216");
        wh.CreatedBy = wh.ModifiedBy = "System";
        _db.Warehouses.Add(wh); await _db.SaveChangesAsync(ct);

        // ── Brands ───────────────────────────────────────────────────────────
        var (bosch, denso, ngk, brembo, castrol) = (
            B("Bosch",   "Germany"), B("Denso",  "Japan"),  B("NGK", "Japan"),
            B("Brembo",  "Italy"),   B("Castrol","UK"));
        _db.Brands.AddRange(bosch, denso, ngk, brembo, castrol);
        await _db.SaveChangesAsync(ct);

        // ── Categories ───────────────────────────────────────────────────────
        var (catEng, catBrk, catSus, catOil, catFlt) = (
            C("Engine Parts",   "CAT-ENG", 1),
            C("Brake System",   "CAT-BRK", 2),
            C("Suspension",     "CAT-SUS", 3),
            C("Oils & Fluids",  "CAT-OIL", 4),
            C("Filters",        "CAT-FLT", 5));
        _db.Categories.AddRange(catEng, catBrk, catSus, catOil, catFlt);
        await _db.SaveChangesAsync(ct);

        // ── Vehicles ─────────────────────────────────────────────────────────
        var vehicles = new[]
        {
            V("Toyota",     "Corolla", 2022, "1.6L Inline-4"),
            V("Honda",      "Civic",   2022, "1.5T Inline-4"),
            V("Nissan",     "X-Trail", 2022, "2.5L Inline-4"),
            V("Suzuki",     "Swift",   2023, "1.2L Inline-3"),
            V("Mitsubishi", "Pajero",  2021, "3.2L Diesel"),
        };
        _db.Vehicles.AddRange(vehicles);
        await _db.SaveChangesAsync(ct);

        // ── Attribute Groups ─────────────────────────────────────────────────
        var grpGen  = AG("General",     1);
        var grpPerf = AG("Performance", 2);
        var grpFit  = AG("Fitment",     3);
        var grpSpec = AG("Specifications", 4);
        _db.ProductAttributeGroups.AddRange(grpGen, grpPerf, grpFit, grpSpec);
        await _db.SaveChangesAsync(ct);

        // ── Attributes ───────────────────────────────────────────────────────
        var aGrade    = PA(grpGen.Id,  "Grade",       "GRADE",    "option");
        var aPos      = PA(grpFit.Id,  "Position",    "POS",      "option");
        var aBrakeTyp = PA(grpPerf.Id, "Brake Type",  "BRAKE_TYP","option");
        var aVisc     = PA(grpSpec.Id, "Viscosity",   "VISCOSITY","option");
        var aVol      = PA(grpSpec.Id, "Volume",      "VOLUME",   "option", "L");
        var aMat      = PA(grpPerf.Id, "Material",    "MATERIAL", "option");
        var aEngType  = PA(grpSpec.Id, "Engine Type", "ENG_TYPE", "option");
        _db.ProductAttributes.AddRange(aGrade, aPos, aBrakeTyp, aVisc, aVol, aMat, aEngType);
        await _db.SaveChangesAsync(ct);

        // ── Attribute Options ────────────────────────────────────────────────
        var opts = new Dictionary<string, ProductAttributeOption>
        {
            ["Standard"]       = AO(aGrade.Id, "Standard",       1),
            ["Premium"]        = AO(aGrade.Id, "Premium",        2),
            ["OEM"]            = AO(aGrade.Id, "OEM",            3),
            ["Front"]          = AO(aPos.Id,   "Front",          1),
            ["Rear"]           = AO(aPos.Id,   "Rear",           2),
            ["Left"]           = AO(aPos.Id,   "Left",           3),
            ["Right"]          = AO(aPos.Id,   "Right",          4),
            ["Ceramic"]        = AO(aBrakeTyp.Id,"Ceramic",      1),
            ["Semi-Metallic"]  = AO(aBrakeTyp.Id,"Semi-Metallic",2),
            ["Organic"]        = AO(aBrakeTyp.Id,"Organic",      3),
            ["0W-20"]          = AO(aVisc.Id,  "0W-20",          1),
            ["5W-30"]          = AO(aVisc.Id,  "5W-30",          2),
            ["5W-40"]          = AO(aVisc.Id,  "5W-40",          3),
            ["10W-40"]         = AO(aVisc.Id,  "10W-40",         4),
            ["15W-50"]         = AO(aVisc.Id,  "15W-50",         5),
            ["1L"]             = AO(aVol.Id,   "1L",             1),
            ["4L"]             = AO(aVol.Id,   "4L",             2),
            ["5L"]             = AO(aVol.Id,   "5L",             3),
            ["Steel"]          = AO(aMat.Id,   "Steel",          1),
            ["Aluminium"]      = AO(aMat.Id,   "Aluminium",      2),
            ["Carbon Fibre"]   = AO(aMat.Id,   "Carbon Fibre",   3),
            ["Petrol"]         = AO(aEngType.Id,"Petrol",        1),
            ["Diesel"]         = AO(aEngType.Id,"Diesel",        2),
            ["Hybrid"]         = AO(aEngType.Id,"Hybrid",        3),
        };
        _db.ProductAttributeOptions.AddRange(opts.Values);
        await _db.SaveChangesAsync(ct);

        // ── Category ↔ Attribute links ────────────────────────────────────────
        // Engine: Grade, Material, Engine Type
        CA(catEng.Id, aGrade.Id,   "select", 1); CA(catEng.Id, aMat.Id,     "select", 2); CA(catEng.Id, aEngType.Id,"select", 3);
        // Brake: Grade, Brake Type, Position
        CA(catBrk.Id, aGrade.Id,   "select", 1); CA(catBrk.Id, aBrakeTyp.Id,"select", 2); CA(catBrk.Id, aPos.Id,    "select", 3);
        // Suspension: Grade, Position, Material
        CA(catSus.Id, aGrade.Id,   "select", 1); CA(catSus.Id, aPos.Id,     "select", 2); CA(catSus.Id, aMat.Id,    "select", 3);
        // Oils: Viscosity, Volume
        CA(catOil.Id, aVisc.Id,    "select", 1); CA(catOil.Id, aVol.Id,     "select", 2);
        // Filters: Grade
        CA(catFlt.Id, aGrade.Id,   "select", 1);
        await _db.SaveChangesAsync(ct);

        // ── Products ─────────────────────────────────────────────────────────
        // Helper lambdas
        Unit U(string n, string c, string s) { var u = Unit.Create(n, c, s); u.CreatedBy = u.ModifiedBy = "System"; return u; }
        Brand B(string n, string country) { var b = Brand.Create(n, n.ToUpper()[..Math.Min(6,n.Length)], country: country); b.CreatedBy = b.ModifiedBy = "System"; return b; }
        Category C(string n, string code, int order)
        {
            var c = Category.Create(n, n, code, order);
            c.CreatedBy = c.ModifiedBy = "System";
            return c;
        }
        Vehicle V(string mk, string md, int yr, string eng) { var v = Vehicle.Create(mk, md, yr, eng); v.CreatedBy = v.ModifiedBy = "System"; return v; }
        ProductAttributeGroup AG(string n, int order) { var g = ProductAttributeGroup.Create(n, order); g.CreatedBy = g.ModifiedBy = "System"; return g; }
        ProductAttribute PA(Guid gid, string n, string code, string dt, string unit = "")
        { var a = ProductAttribute.Create(gid, n, code, dt, unit); a.CreatedBy = a.ModifiedBy = "System"; return a; }
        ProductAttributeOption AO(Guid aid, string val, int order)
        { var o = ProductAttributeOption.Create(aid, val, order); o.CreatedBy = o.ModifiedBy = "System"; return o; }
        void CA(Guid cid, Guid aid, string ft, int order)
        {
            var ca = CategoryAttribute.Create(cid, aid, isFilterable: true, filterType: ft, sortOrder: order);
            ca.CreatedBy = ca.ModifiedBy = "System";
            _db.CategoryAttributes.Add(ca);
        }

        int counter = 1;

        // Variant tuple type alias
        static (string grade, string? pos, string? brakeType, string? mat, string? visc, string? vol, string? engType, decimal costMult, decimal sellMult, int stock)
            Vt(string grade, string? pos, string? bt, string? mat, string? visc, string? vol, string? eng, decimal cm, decimal sm, int s)
            => (grade, pos, bt, mat, visc, vol, eng, cm, sm, s);

        async Task AddProduct(
            string name, string sku, Guid catId, Guid brandId,
            decimal cost, decimal sell, string desc, string shortDesc, string img,
            bool featured, int rank, decimal pop,
            bool warranty, int wMonths, string wType,
            (string grade, string? pos, string? brakeType, string? mat, string? visc, string? vol, string? engType, decimal costMult, decimal sellMult, int stock)[] variants,
            decimal? discPct = null)
        {
            var pn = $"PN-{counter++:D4}";
            var part = Product.Create(name, PartNumber.Create(pn), sku, catId,
                brandId: brandId, baseUnitId: pcs.Id, unitId: pcs.Id,
                description: desc, costPrice: cost, sellingPrice: sell,
                hasWarranty: warranty, warrantyPeriodMonths: wMonths, warrantyType: wType);
            part.CreatedBy = part.ModifiedBy = "System";
            _db.Parts.Add(part);
            await _db.SaveChangesAsync(ct);

            var slug = name.ToLowerInvariant()
                .Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("/", "-")
                .Replace(",", "").Replace(".", "").Replace("&", "and");

            var entry = ProductCatalogEntry.Create(part.Id, slug, shortDesc,
                isPublished: true, publishedAt: DateTime.UtcNow.AddDays(-60),
                isFeatured: featured, featuredRank: rank, popularityScore: pop,
                primaryImageUrl: img);
            entry.CreatedBy = entry.ModifiedBy = "System";
            _db.ProductCatalogEntries.Add(entry);

            // Part-level stock — only for parts WITHOUT variants. Variant parts get variant-scoped
            // rows below; mixing both would double-count the part's on-hand.
            if (variants.Length == 0)
            {
                var sl = StockLevel.Create(part.Id, wh.Id, reorderLevel: 5, reorderQuantity: 20, unitId: pcs.Id);
                sl.AddStock(25);
                sl.CreatedBy = sl.ModifiedBy = "System";
                _db.StockLevels.Add(sl);
            }

            // Discount on part
            if (discPct.HasValue)
            {
                var d = Discount.Create($"{discPct}% off {name}", "PERCENTAGE", discPct.Value,
                    DateTime.UtcNow.AddDays(-3), partId: part.Id,
                    endDate: DateTime.UtcNow.AddDays(30),
                    description: $"Limited time {discPct}% discount");
                d.CreatedBy = d.ModifiedBy = "System";
                _db.Discounts.Add(d);
            }

            await _db.SaveChangesAsync(ct);

            // Variants
            int vnum = 1;
            foreach (var (grade, pos, brakeType, mat, visc, vol, engType, costMult, sellMult, stock) in variants)
            {
                var vCode = $"{sku}-V{vnum++}";
                var vSku  = $"SKU-{sku}-{grade.ToUpper()[..Math.Min(3,grade.Length)]}";
                var variantCost = Math.Round(cost * costMult);
                var variantSell = Math.Round(sell * sellMult);

                var variant = ProductVariant.Create(part.Id,
                    $"{name} — {grade}", vCode,
                    costPrice: variantCost, sellingPrice: variantSell,
                    sku: vSku, isActive: true);
                variant.CreatedBy = variant.ModifiedBy = "System";
                _db.ProductVariants.Add(variant);
                await _db.SaveChangesAsync(ct);

                // Attribute values
                void AV(Guid attrId, string optVal)
                {
                    if (!opts.TryGetValue(optVal, out var opt)) return;
                    var av = VariantAttributeValue.Create(variant.Id, attrId, optionId: opt.Id);
                    av.CreatedBy = av.ModifiedBy = "System";
                    _db.VariantAttributeValues.Add(av);
                }
                AV(aGrade.Id, grade);
                if (pos != null)      AV(aPos.Id,      pos);
                if (brakeType != null) AV(aBrakeTyp.Id, brakeType);
                if (mat != null)      AV(aMat.Id,      mat);
                if (visc != null)     AV(aVisc.Id,     visc);
                if (vol != null)      AV(aVol.Id,      vol);
                if (engType != null)  AV(aEngType.Id,  engType);
                await _db.SaveChangesAsync(ct);

                // Variant-scoped stock (unified StockLevels table)
                var vsl = StockLevel.Create(part.Id, wh.Id, variantId: variant.Id);
                vsl.AddStock(stock);
                vsl.CreatedBy = vsl.ModifiedBy = "System";
                _db.StockLevels.Add(vsl);
                await _db.SaveChangesAsync(ct);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Lean demo catalog — 5 products covering the key variant-stock scenarios.
        // ════════════════════════════════════════════════════════════════════

        // 1) Brake pads — Front/Rear × compound (multi-variant, discounted)
        await AddProduct("Brembo Brake Pad Set", "BP-001", catBrk.Id, brembo.Id,
            cost: 950, sell: 1800,
            desc: "Brembo high-performance brake pads in ceramic and semi-metallic compounds. OEM fitment for most vehicles.",
            shortDesc: "Brembo brake pads — superior stopping power",
            img: "https://images.unsplash.com/photo-1621252179027-94459d278660?w=640&q=80",
            featured: true, rank: 1, pop: 98,
            warranty: true, wMonths: 6, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Front", "Ceramic",       null, null, null, null, 1.0m, 1.0m, 30),
                ("Premium",  "Front", "Ceramic",       null, null, null, null, 1.2m, 1.4m, 20),
                ("Standard", "Rear",  "Semi-Metallic", null, null, null, null, 0.9m, 0.9m, 25),
            },
            discPct: 20);

        // 2) Lower control arm — Left/Right (canonical distinct physical SKU example)
        await AddProduct("Lower Control Arm", "LCA-001", catSus.Id, bosch.Id,
            cost: 1200, sell: 2200,
            desc: "Heavy-duty lower control arm with pre-installed ball joint. Distinct left and right units.",
            shortDesc: "Lower control arm with ball joint — heavy duty",
            img: "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            featured: true, rank: 2, pop: 70,
            warranty: true, wMonths: 12, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Left",  null, "Steel", null, null, null, 1.0m, 1.0m, 10),
                ("Standard", "Right", null, "Steel", null, null, null, 1.0m, 1.0m,  5),
            });

        // 3) Timing belt kit — Standard/Premium/OEM (grade variants)
        await AddProduct("Bosch Timing Belt Kit", "TBK-001", catEng.Id, bosch.Id,
            cost: 1800, sell: 2800,
            desc: "Complete timing belt kit with tensioner and idler pulley. Fits 1.6L–2.0L petrol engines.",
            shortDesc: "Complete timing belt kit with tensioner — OEM quality",
            img: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: true, rank: 3, pop: 90,
            warranty: true, wMonths: 12, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, "Steel", null, null, "Petrol", 1.0m, 1.0m, 20),
                ("Premium",  null, null, "Steel", null, null, "Petrol", 1.2m, 1.3m, 15),
                ("OEM",      null, null, "Steel", null, null, "Diesel", 1.4m, 1.6m, 10),
            });

        // 4) Engine oil — viscosity/volume variants (discounted)
        await AddProduct("Castrol GTX Engine Oil", "OIL-001", catOil.Id, castrol.Id,
            cost: 650, sell: 1050,
            desc: "Castrol GTX fully synthetic engine oil. ACEA A3/B4 and API SN/CF approved.",
            shortDesc: "Castrol GTX engine oil — fully synthetic",
            img: "https://images.unsplash.com/photo-1629451399979-59fb59ebeef3?w=640&q=80",
            featured: true, rank: 4, pop: 99,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, "5W-30",  "4L", "Petrol", 1.0m, 1.0m,  40),
                ("Standard", null, null, null, "10W-40", "4L", "Petrol", 0.9m, 0.95m, 35),
                ("Premium",  null, null, null, "5W-30",  "4L", "Petrol", 1.25m,1.4m,  25),
            },
            discPct: 12);

        // 5) Air filter — NO variants (exercises the part-level / VariantId-null path)
        await AddProduct("Bosch Air Filter", "AF-001", catFlt.Id, bosch.Id,
            cost: 220, sell: 420,
            desc: "Bosch high-flow air filter. OEM replacement quality. No variants — a plain part-level SKU.",
            shortDesc: "Bosch high-flow air filter — OEM quality",
            img: "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: false, rank: 0, pop: 80,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: System.Array.Empty<(string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)>());

        await _db.SaveChangesAsync(ct);

        var productCount  = await _db.Parts.CountAsync(ct);
        var variantCount  = await _db.ProductVariants.CountAsync(ct);
        var discountCount = await _db.Discounts.CountAsync(ct);

        _logger.LogInformation("Dev seed complete: {P} products, {V} variants, {D} discounts", productCount, variantCount, discountCount);

        return Ok(new
        {
            message = "Catalog seeded successfully",
            products  = productCount,
            variants  = variantCount,
            discounts = discountCount,
            categories = 5,
            attributes = 7,
            vehicles = vehicles.Length
        });
    }
}
