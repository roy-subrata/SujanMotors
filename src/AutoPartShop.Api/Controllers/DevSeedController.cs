using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Dev-only endpoint: wipes and re-seeds the e-commerce catalog.
/// Call POST /api/dev/seed-catalog to reset demo data.
/// </summary>
[Route("api/dev")]
[ApiController]
public class DevSeedController(AutoPartDbContext _db, ICodeGenerateService _code, ILogger<DevSeedController> _logger) : ControllerBase
{
    [HttpPost("seed-catalog")]
    public async Task<IActionResult> SeedCatalog(CancellationToken ct)
    {
        _logger.LogInformation("Dev seed: clearing and re-seeding catalog...");

        // ── Clear catalog data via raw SQL in correct FK order ───────────────
        var deletes = new[]
        {
            "DELETE FROM CartReservations",
            "DELETE FROM StockMovements",
            "DELETE FROM VariantStockLevels",
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
        var (bosch, denso, ngk, brembo, monroe, mann, castrol, hella) = (
            B("Bosch",   "Germany"), B("Denso",  "Japan"),  B("NGK",    "Japan"),
            B("Brembo",  "Italy"),   B("Monroe", "USA"),    B("Mann",   "Germany"),
            B("Castrol", "UK"),      B("Hella",  "Germany"));
        _db.Brands.AddRange(bosch, denso, ngk, brembo, monroe, mann, castrol, hella);
        await _db.SaveChangesAsync(ct);

        // ── Categories ───────────────────────────────────────────────────────
        var (catEng, catBrk, catSus, catElc, catOil, catFlt, catBdy, catTyr) = (
            C("Engine Parts",   "CAT-ENG", 1),
            C("Brake System",   "CAT-BRK", 2),
            C("Suspension",     "CAT-SUS", 3),
            C("Electrical",     "CAT-ELC", 4),
            C("Oils & Fluids",  "CAT-OIL", 5),
            C("Filters",        "CAT-FLT", 6),
            C("Body Parts",     "CAT-BDY", 7),
            C("Tires & Wheels", "CAT-TYR", 8));
        _db.Categories.AddRange(catEng, catBrk, catSus, catElc, catOil, catFlt, catBdy, catTyr);
        await _db.SaveChangesAsync(ct);

        // ── Vehicles ─────────────────────────────────────────────────────────
        var vehicles = new[]
        {
            V("Toyota",     "Corolla",    2022, "1.6L Inline-4"),
            V("Toyota",     "Corolla",    2020, "1.6L Inline-4"),
            V("Toyota",     "Hilux",      2022, "2.4L Diesel"),
            V("Toyota",     "Land Cruiser",2021,"4.0L V6"),
            V("Honda",      "Civic",      2022, "1.5T Inline-4"),
            V("Honda",      "Accord",     2021, "2.0T Inline-4"),
            V("Honda",      "CR-V",       2022, "1.5T Inline-4"),
            V("Nissan",     "Sunny",      2021, "1.5L Inline-4"),
            V("Nissan",     "X-Trail",    2022, "2.5L Inline-4"),
            V("Mitsubishi", "Lancer",     2019, "2.0L Inline-4"),
            V("Mitsubishi", "Pajero",     2021, "3.2L Diesel"),
            V("Suzuki",     "Swift",      2023, "1.2L Inline-3"),
            V("Suzuki",     "Wagon R",    2022, "1.0L Inline-3"),
            V("Hyundai",    "Tucson",     2022, "2.0L Inline-4"),
            V("Hyundai",    "Elantra",    2022, "1.6L Inline-4"),
            V("BMW",        "3 Series",   2021, "2.0L B48"),
            V("BMW",        "5 Series",   2020, "2.0L B47 Diesel"),
            V("Mercedes",   "C-Class",    2021, "2.0L M264"),
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
        // Electrical: Grade
        CA(catElc.Id, aGrade.Id,   "select", 1);
        // Oils: Viscosity, Volume
        CA(catOil.Id, aVisc.Id,    "select", 1); CA(catOil.Id, aVol.Id,     "select", 2);
        // Filters: Grade
        CA(catFlt.Id, aGrade.Id,   "select", 1);
        // Body: Position, Material
        CA(catBdy.Id, aPos.Id,     "select", 1); CA(catBdy.Id, aMat.Id,     "select", 2);
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

            // Part-level stock
            var sl = StockLevel.Create(part.Id, wh.Id, reorderLevel: 5, reorderQuantity: 20, unitId: pcs.Id);
            sl.AddStock(variants.Sum(v => v.stock));
            sl.CreatedBy = sl.ModifiedBy = "System";
            _db.StockLevels.Add(sl);

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

                // Variant stock
                var vsl = VariantStockLevel.Create(variant.Id, wh.Id);
                vsl.AddStock(stock);
                vsl.CreatedBy = vsl.ModifiedBy = "System";
                _db.VariantStockLevels.Add(vsl);
                await _db.SaveChangesAsync(ct);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // ENGINE PARTS
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Bosch Timing Belt Kit", "TBK-001", catEng.Id, bosch.Id,
            cost: 1800, sell: 2800,
            desc: "Complete timing belt kit with tensioner and idler pulley. Fits 1.6L–2.0L petrol engines. Replace every 60,000 km for optimal performance and engine protection.",
            shortDesc: "Complete timing belt kit with tensioner — OEM quality",
            img: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: true, rank: 1, pop: 95,
            warranty: true, wMonths: 12, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, "Steel",    null, null, "Petrol", 1.0m, 1.0m, 20),
                ("Premium",  null, null, "Steel",    null, null, "Petrol", 1.2m, 1.3m, 15),
                ("OEM",      null, null, "Steel",    null, null, "Diesel", 1.4m, 1.6m, 10),
            });

        await AddProduct("NGK Iridium Spark Plug Set (4pcs)", "SPK-001", catEng.Id, ngk.Id,
            cost: 380, sell: 650,
            desc: "Set of 4 NGK iridium spark plugs. Superior ignitability with 100,000 km service life. Compatible with most 4-cylinder petrol engines.",
            shortDesc: "NGK iridium spark plugs × 4 — 100,000 km life",
            img: "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: true, rank: 2, pop: 90,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, "Petrol", 1.0m, 1.0m, 50),
                ("Premium",  null, null, null, null, null, "Petrol", 1.1m, 1.25m, 30),
            },
            discPct: 15);

        await AddProduct("Bosch Fuel Injector", "FI-001", catEng.Id, bosch.Id,
            cost: 2200, sell: 3500,
            desc: "High-precision petrol fuel injector. Ensures optimal fuel atomisation for improved combustion efficiency and reduced emissions.",
            shortDesc: "High-precision fuel injector — improved combustion",
            img: "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=640&q=80",
            featured: false, rank: 0, pop: 72,
            warranty: true, wMonths: 6, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, "Petrol", 1.0m, 1.0m, 12),
                ("OEM",      null, null, null, null, null, "Diesel", 1.3m, 1.5m,  8),
            });

        await AddProduct("Water Pump with Gasket", "WP-001", catEng.Id, bosch.Id,
            cost: 900, sell: 1600,
            desc: "OEM-spec water pump complete with new gasket. Reliable engine cooling for all driving conditions. Fits Toyota, Honda, and Nissan models.",
            shortDesc: "OEM water pump with gasket — reliable cooling",
            img: "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            featured: true, rank: 4, pop: 65,
            warranty: true, wMonths: 12, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, "Aluminium", null, null, "Petrol", 1.0m, 1.0m, 15),
                ("OEM",      null, null, "Aluminium", null, null, "Diesel", 1.2m, 1.4m, 10),
            });

        // ════════════════════════════════════════════════════════════════════
        // BRAKE SYSTEM
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Brembo Brake Pad Set", "BP-F-001", catBrk.Id, brembo.Id,
            cost: 950, sell: 1800,
            desc: "Brembo high-performance brake pads. Available in ceramic, semi-metallic and organic compounds. OEM fitment for most vehicles.",
            shortDesc: "Brembo brake pads — superior stopping power",
            img: "https://images.unsplash.com/photo-1621252179027-94459d278660?w=640&q=80",
            featured: true, rank: 3, pop: 98,
            warranty: true, wMonths: 6, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Front", "Ceramic",      null, null, null, null, 1.0m, 1.0m, 30),
                ("Premium",  "Front", "Ceramic",      null, null, null, null, 1.2m, 1.4m, 20),
                ("Standard", "Rear",  "Semi-Metallic",null, null, null, null, 0.9m, 0.9m, 25),
                ("Premium",  "Rear",  "Ceramic",      null, null, null, null, 1.1m, 1.3m, 15),
                ("Standard", "Front", "Organic",      null, null, null, null, 0.8m, 0.85m, 20),
            },
            discPct: 20);

        await AddProduct("Vented Brake Disc Rotors (Pair)", "BDR-001", catBrk.Id, brembo.Id,
            cost: 1400, sell: 2600,
            desc: "Precision-balanced vented brake disc rotors, sold as a pair. High-carbon steel for superior heat dissipation and fade resistance.",
            shortDesc: "Vented brake disc rotors pair — high-carbon steel",
            img: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: false, rank: 0, pop: 82,
            warranty: true, wMonths: 12, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Front", null, "Steel",       null, null, null, 1.0m, 1.0m, 18),
                ("Premium",  "Front", null, "Steel",       null, null, null, 1.2m, 1.35m, 12),
                ("Standard", "Rear",  null, "Steel",       null, null, null, 1.0m, 0.95m, 18),
                ("OEM",      "Front", null, "Carbon Fibre",null, null, null, 1.5m, 1.8m,  6),
            });

        // ════════════════════════════════════════════════════════════════════
        // SUSPENSION
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Monroe Shock Absorbers", "SA-001", catSus.Id, monroe.Id,
            cost: 2800, sell: 4800,
            desc: "Monroe OESpectrum shock absorbers. Restores original ride comfort and vehicle stability. Available for front and rear axle.",
            shortDesc: "Monroe shock absorbers — front & rear — OESpectrum",
            img: "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=640&q=80",
            featured: true, rank: 5, pop: 85,
            warranty: true, wMonths: 24, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Front", null, "Steel",     null, null, null, 1.0m, 1.0m, 12),
                ("Premium",  "Front", null, "Steel",     null, null, null, 1.15m, 1.3m, 8),
                ("Standard", "Rear",  null, "Steel",     null, null, null, 0.95m, 0.95m,12),
                ("Premium",  "Rear",  null, "Steel",     null, null, null, 1.1m,  1.25m, 8),
            },
            discPct: 10);

        await AddProduct("Lower Control Arm", "LCA-001", catSus.Id, bosch.Id,
            cost: 1200, sell: 2200,
            desc: "Heavy-duty lower control arm with pre-installed ball joint. Fits Toyota Corolla 2018–2023 and Honda Civic 2016–2022.",
            shortDesc: "Lower control arm with ball joint — heavy duty",
            img: "https://images.unsplash.com/photo-1632912515069-45e9f2bf0b36?w=640&q=80",
            featured: false, rank: 0, pop: 60,
            warranty: true, wMonths: 12, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", "Left",  null, "Steel",     null, null, null, 1.0m, 1.0m, 14),
                ("Standard", "Right", null, "Steel",     null, null, null, 1.0m, 1.0m, 14),
                ("OEM",      "Left",  null, "Aluminium", null, null, null, 1.3m, 1.4m,  6),
                ("OEM",      "Right", null, "Aluminium", null, null, null, 1.3m, 1.4m,  6),
            });

        // ════════════════════════════════════════════════════════════════════
        // ELECTRICAL
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Bosch Alternator", "ALT-001", catElc.Id, bosch.Id,
            cost: 5500, sell: 8500,
            desc: "Remanufactured Bosch alternator. Available in 65A and 90A output ratings. Fully tested. New voltage regulator included.",
            shortDesc: "Bosch remanufactured alternator — 65A / 90A",
            img: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: false, rank: 0, pop: 70,
            warranty: true, wMonths: 12, wType: "MANUFACTURER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, "Petrol", 1.0m, 1.0m, 6),
                ("Premium",  null, null, null, null, null, "Diesel", 1.15m,1.2m, 4),
                ("OEM",      null, null, null, null, null, "Petrol", 1.3m, 1.4m, 3),
            });

        await AddProduct("Hella H7 Halogen Bulbs (Pack of 2)", "HLB-001", catElc.Id, hella.Id,
            cost: 220, sell: 450,
            desc: "H7 12V 55W halogen headlight bulbs, pack of 2. ECE approved. Long-life tungsten filament for reliable illumination.",
            shortDesc: "Hella H7 halogen headlight bulbs × 2",
            img: "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: true, rank: 6, pop: 92,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, null, 1.0m, 1.0m, 60),
                ("Premium",  null, null, null, null, null, null, 1.1m, 1.3m, 40),
            },
            discPct: 25);

        // ════════════════════════════════════════════════════════════════════
        // OILS & FLUIDS
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Castrol GTX Engine Oil", "OIL-001", catOil.Id, castrol.Id,
            cost: 650, sell: 1050,
            desc: "Castrol GTX fully synthetic engine oil. Available in 5W-30 and 10W-40 viscosities. ACEA A3/B4 and API SN/CF approved.",
            shortDesc: "Castrol GTX engine oil — fully synthetic",
            img: "https://images.unsplash.com/photo-1629451399979-59fb59ebeef3?w=640&q=80",
            featured: true, rank: 7, pop: 99,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, "5W-30", "4L",  "Petrol", 1.0m, 1.0m, 80),
                ("Standard", null, null, null, "5W-30", "5L",  "Petrol", 1.1m, 1.1m, 60),
                ("Standard", null, null, null, "5W-40", "4L",  "Diesel", 1.05m,1.05m,50),
                ("Standard", null, null, null, "10W-40","4L",  "Petrol", 0.9m, 0.95m,70),
                ("Standard", null, null, null, "0W-20", "4L",  "Hybrid", 1.2m, 1.25m,30),
                ("Premium",  null, null, null, "5W-30", "4L",  "Petrol", 1.25m,1.4m, 40),
                ("Premium",  null, null, null, "5W-40", "4L",  "Diesel", 1.3m, 1.45m,35),
                ("Premium",  null, null, null, "10W-40","4L",  "Petrol", 1.15m,1.3m, 45),
                ("OEM",      null, null, null, "5W-30", "1L",  "Petrol", 0.3m, 0.28m,100),
                ("OEM",      null, null, null, "15W-50","4L",  "Diesel", 1.1m, 1.2m, 25),
            },
            discPct: 12);

        // ════════════════════════════════════════════════════════════════════
        // FILTERS
        // ════════════════════════════════════════════════════════════════════
        await AddProduct("Mann+Hummel Oil Filter", "OF-001", catFlt.Id, mann.Id,
            cost: 180, sell: 320,
            desc: "Mann+Hummel oil filter with 20-micron precision filtration. Integrated bypass valve prevents oil starvation on cold starts.",
            shortDesc: "Mann oil filter — 20-micron precision filtration",
            img: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640&q=80",
            featured: true, rank: 8, pop: 95,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, null, 1.0m, 1.0m, 80),
                ("Premium",  null, null, null, null, null, null, 1.2m, 1.4m, 50),
                ("OEM",      null, null, null, null, null, null, 1.5m, 1.7m, 25),
            },
            discPct: 18);

        await AddProduct("Bosch Air Filter", "AF-001", catFlt.Id, bosch.Id,
            cost: 220, sell: 420,
            desc: "Bosch high-flow air filter. OEM replacement quality. Blocks fine dust and debris while allowing maximum airflow for engine performance.",
            shortDesc: "Bosch high-flow air filter — OEM quality",
            img: "https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=640&q=80",
            featured: false, rank: 0, pop: 80,
            warranty: false, wMonths: 0, wType: "SELLER",
            variants: new (string, string?, string?, string?, string?, string?, string?, decimal, decimal, int)[]
            {
                ("Standard", null, null, null, null, null, null, 1.0m, 1.0m, 60),
                ("Premium",  null, null, null, null, null, null, 1.15m,1.3m, 40),
            });

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
            categories = 8,
            attributes = 7,
            vehicles = vehicles.Length
        });
    }
}
