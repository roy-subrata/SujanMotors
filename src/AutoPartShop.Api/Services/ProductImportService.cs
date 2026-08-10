using AutoPartShop.Application.DTOs.PartDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using AutoPartsShop.Domain.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AutoPartShop.Api.Services;

/// <inheritdoc />
public sealed class ProductImportService(
    AutoPartDbContext _db,
    ICodeGenerateService _codeGenerateService,
    ICurrentUserService _currentUserService,
    ILogger<ProductImportService> _logger) : IProductImportService
{
    private const string SheetName = "Products";
    private const string CategorySeparator = ">";
    private const string PriceUpdateReason = "IMPORT_PRICE_UPDATE";
    private const string InitialPriceReason = "INITIAL_PRICE";

    // Canonical column headers, in template order. A trailing "*" marks a column that is
    // required when creating a part (on an update, blank means "leave unchanged").
    // SKU is the import key: blank creates, filled updates.
    private static readonly string[] Headers =
    [
        "SKU",
        "Name*",
        "Local Name",
        "Part Number",
        "Category*",
        "Brand",
        "Unit",
        "Cost Price",
        "Selling Price",
        "Minimum Stock",
        "Barcode",
        "OEM Number",
        "Tags",
        "Description",
        "Product Type",
        "Tax Code",
        "Has Warranty",
        "Warranty Period (months)",
        "Warranty Type",
        "Weight (kg)",
        "Variant Name",
        "Variant Code",
        "Variant Part Number",
        "Variant OEM Number",
        "Variant Barcode",
        "Variant Cost Price",
        "Variant Selling Price"
    ];

    private static readonly string[] ValidProductTypes = ["PHYSICAL", "DIGITAL", "SERVICE"];

    // ── Template ───────────────────────────────────────────────────────────────

    public byte[] GenerateTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName);

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Two rows for the same product (same Name + Part Number) — one per variant.
        // Product-level columns only need filling on the first row of the group.
        // SKU is left blank so both rows create a new part; fill it to update an existing one.
        var example1 = new[]
        {
            "", "Front Brake Pad Set", "ব্রেক প্যাড সেট", "BP-1001", "Brake System > Front Brakes", "Bosch", "Pieces",
            "450", "650", "10", "8901234567890", "OEM-77231", "brake,pad,front",
            "Ceramic front brake pad set", "PHYSICAL", "STANDARD",
            "TRUE", "12", "MANUFACTURER", "1.2",
            "Standard", "BP-STD", "", "", "", "450", "650"
        };
        var example2 = new[]
        {
            "", "Front Brake Pad Set", "", "BP-1001", "Brake System > Front Brakes", "Bosch", "Pieces",
            "", "", "", "", "", "",
            "", "", "",
            "", "", "", "",
            "Premium", "BP-PRM", "", "", "", "600", "900"
        };
        for (var i = 0; i < example1.Length; i++)
            ws.Cell(2, i + 1).Value = example1[i];
        for (var i = 0; i < example2.Length; i++)
            ws.Cell(3, i + 1).Value = example2[i];

        ws.Row(1).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Export (round-trip source) ─────────────────────────────────────────────

    public async Task<byte[]> GenerateExportAsync(CancellationToken cancellationToken = default)
    {
        var parts = await _db.Parts.AsNoTracking()
            .Where(p => !p.Isdeleted)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Include(p => p.Variants.Where(v => !v.Isdeleted))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var categoryPaths = await LoadCategoryPathsAsync(cancellationToken);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName);

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var r = 2;
        foreach (var part in parts)
        {
            var variants = part.Variants.OrderBy(v => v.Code).ToList();

            if (variants.Count == 0)
            {
                WriteProductColumns(ws, r, part, categoryPaths);
                r++;
                continue;
            }

            // One row per variant. Product columns repeat on every row of the part so the
            // sheet reads well and re-imports as the same group.
            foreach (var variant in variants)
            {
                WriteProductColumns(ws, r, part, categoryPaths);
                WriteText(ws, r, "Variant Name", variant.Name);
                WriteText(ws, r, "Variant Code", variant.Code);
                WriteText(ws, r, "Variant Part Number", variant.PartNumber?.Value);
                WriteText(ws, r, "Variant OEM Number", variant.OemNumber);
                WriteText(ws, r, "Variant Barcode", variant.Barcode);
                WriteNumber(ws, r, "Variant Cost Price", variant.CostPrice);
                WriteNumber(ws, r, "Variant Selling Price", variant.SellingPrice);
                r++;
            }
        }

        ws.Row(1).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteProductColumns(IXLWorksheet ws, int row, Product part, Dictionary<Guid, string> categoryPaths)
    {
        WriteText(ws, row, "SKU", part.SKU);
        WriteText(ws, row, "Name*", part.Name);
        WriteText(ws, row, "Local Name", part.LocalName);
        WriteText(ws, row, "Part Number", part.PartNumber?.Value);
        WriteText(ws, row, "Category*", categoryPaths.TryGetValue(part.CategoryId, out var path) ? path : null);
        WriteText(ws, row, "Brand", part.Brand?.Name);
        WriteText(ws, row, "Unit", part.Unit?.Name ?? part.BaseUnit?.Name);
        WriteNumber(ws, row, "Cost Price", part.CostPrice);
        WriteNumber(ws, row, "Selling Price", part.SellingPrice);
        WriteNumber(ws, row, "Minimum Stock", part.MinimumStock);
        WriteText(ws, row, "Barcode", part.Barcode);
        WriteText(ws, row, "OEM Number", part.OemNumber);
        WriteText(ws, row, "Tags", part.Tags);
        WriteText(ws, row, "Description", part.Description);
        WriteText(ws, row, "Product Type", part.ProductType);
        WriteText(ws, row, "Tax Code", part.TaxCode);
        WriteText(ws, row, "Has Warranty", part.HasWarranty ? "TRUE" : "FALSE");
        if (part.WarrantyPeriodMonths.HasValue)
            WriteNumber(ws, row, "Warranty Period (months)", part.WarrantyPeriodMonths.Value);
        WriteText(ws, row, "Warranty Type", part.WarrantyType);
        if (part.WeightKg.HasValue)
            WriteNumber(ws, row, "Weight (kg)", part.WeightKg.Value);
    }

    /// <summary>Full "Parent &gt; Child" path per category id, built by walking the tree.</summary>
    private async Task<Dictionary<Guid, string>> LoadCategoryPathsAsync(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.ParentCategoryId })
            .ToListAsync(ct);

        var byId = categories.ToDictionary(c => c.Id);
        var paths = new Dictionary<Guid, string>();

        foreach (var category in categories)
        {
            var segments = new List<string>();
            var current = category;
            var guard = 0;

            while (guard++ < 20)
            {
                segments.Insert(0, current.Name);
                if (current.ParentCategoryId is null || !byId.TryGetValue(current.ParentCategoryId.Value, out var parent))
                    break;
                current = parent;
            }

            paths[category.Id] = string.Join($" {CategorySeparator} ", segments);
        }

        return paths;
    }

    /// <summary>Column number of a canonical header, 1-based — keeps writes tied to the header list.</summary>
    private static int ColumnOf(string header) => Array.IndexOf(Headers, header) + 1;

    private static void WriteText(IXLWorksheet ws, int row, string header, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        // Identifiers like barcodes must stay text or Excel reformats them as numbers.
        var cell = ws.Cell(row, ColumnOf(header));
        cell.Style.NumberFormat.Format = "@";
        cell.Value = value;
    }

    private static void WriteNumber(IXLWorksheet ws, int row, string header, decimal value)
        => ws.Cell(row, ColumnOf(header)).Value = value;

    private static void WriteNumber(IXLWorksheet ws, int row, string header, int value)
        => ws.Cell(row, ColumnOf(header)).Value = value;

    // ── Validate (dry-run) ───────────────────────────────────────────────────────

    public async Task<ProductImportValidationResult> ValidateAsync(
        Stream xlsxStream, ProductImportMode mode, CancellationToken cancellationToken = default)
    {
        var rows = ParseRows(xlsxStream);
        var ctx = await LoadContextAsync(cancellationToken);
        var state = new RowValidationState();

        var result = new ProductImportValidationResult { TotalRows = rows.Count };

        foreach (var (row, parseErrors) in rows)
        {
            var errors = new List<string>(parseErrors);
            var action = ValidateRow(row, mode, ctx, state, errors);

            var ok = errors.Count == 0;
            result.Rows.Add(new ProductImportRowResult
            {
                RowNumber = row.RowNumber,
                Name = row.Name,
                PartNumber = row.PartNumber,
                Sku = row.Sku,
                Action = action,
                IsValid = ok,
                Errors = errors,
                Row = ok ? row : null
            });
        }

        result.ValidCount = result.Rows.Count(r => r.IsValid);
        result.ErrorCount = result.Rows.Count - result.ValidCount;

        // Counts are per part, not per row — several variant rows describe one part.
        var validRows = result.Rows.Where(r => r is { IsValid: true, Row: not null }).ToList();
        result.CreateCount = validRows.Where(r => r.Action == ProductImportAction.Create)
            .Select(r => GroupKey(r.Row!)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        result.UpdateCount = validRows.Where(r => r.Action == ProductImportAction.Update)
            .Select(r => GroupKey(r.Row!)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        // Surface master data that would be auto-created, so a typo ("Bosh") is caught
        // before it becomes a real brand rather than after.
        var (newBrands, newCategories, newUnits) =
            await PreviewNewReferenceDataAsync(validRows.Select(r => r.Row!).ToList(), cancellationToken);
        result.NewBrands = newBrands;
        result.NewCategories = newCategories;
        result.NewUnits = newUnits;

        return result;
    }

    // ── Commit ─────────────────────────────────────────────────────────────────

    public async Task<ProductImportCommitResult> CommitAsync(
        IEnumerable<ProductImportRow> rows, ProductImportMode mode, CancellationToken cancellationToken = default)
    {
        var orderedRows = rows.OrderBy(r => r.RowNumber).ToList();
        var ctx = await LoadContextAsync(cancellationToken);
        var state = new RowValidationState();
        var user = _currentUserService.GetCurrentUsername();

        var result = new ProductImportCommitResult();

        // Re-validate server-side — the client round-trips the rows, so nothing here is trusted.
        var validRows = new List<ProductImportRow>();
        foreach (var row in orderedRows)
        {
            var errors = new List<string>();
            var action = ValidateRow(row, mode, ctx, state, errors);

            if (errors.Count > 0)
            {
                result.Failures.Add(new ProductImportRowResult
                {
                    RowNumber = row.RowNumber,
                    Name = row.Name,
                    PartNumber = row.PartNumber,
                    Sku = row.Sku,
                    Action = action,
                    IsValid = false,
                    Errors = errors,
                    Row = row
                });
            }
            else
            {
                validRows.Add(row);
            }
        }

        if (validRows.Count == 0)
        {
            result.FailedCount = result.Failures.Count;
            return result;
        }

        // Reference data, parts, variants and price history are one unit of work: either the
        // whole batch lands or none of it does. EnableRetryOnFailure is configured globally,
        // so the transaction has to be owned by an execution strategy.
        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await PersistAsync(validRows, ctx, user, result, cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product import failed; the whole batch was rolled back");

            result.CreatedCount = 0;
            result.UpdatedCount = 0;
            result.CreatedVariantsCount = 0;
            result.UpdatedVariantsCount = 0;
            result.CreatedBrandsCount = 0;
            result.CreatedCategoriesCount = 0;
            result.CreatedUnitsCount = 0;
            result.Failures.Add(new ProductImportRowResult
            {
                RowNumber = 0,
                IsValid = false,
                Errors = [$"Nothing was imported — the batch was rolled back: {ex.InnerException?.Message ?? ex.Message}"]
            });
            result.FailedCount = result.Failures.Count;
            return result;
        }

        result.FailedCount = result.Failures.Count;

        _logger.LogInformation(
            "Product import committed: {Created} created, {Updated} updated, {NewVariants} variants created, "
            + "{UpdatedVariants} variants updated, {Brands} brands, {Categories} categories, {Units} units by {User}",
            result.CreatedCount, result.UpdatedCount, result.CreatedVariantsCount, result.UpdatedVariantsCount,
            result.CreatedBrandsCount, result.CreatedCategoriesCount, result.CreatedUnitsCount, user);

        return result;
    }

    /// <summary>
    /// Writes one validated batch. Runs inside the caller's transaction and may be re-run
    /// by the execution strategy, so it must be safe to repeat from a clean slate.
    /// </summary>
    private async Task PersistAsync(
        List<ProductImportRow> validRows,
        ImportContext ctx,
        string user,
        ProductImportCommitResult result,
        CancellationToken ct)
    {
        // A retry re-runs this method with the previous attempt's entities still tracked as
        // Added — they would be inserted a second time. Start each attempt from empty.
        _db.ChangeTracker.Clear();

        // ── Resolve reference entities (find-or-create) ──────────────────────
        var brandNames = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand))
            .Select(r => r.Brand!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var brandMap = await ResolveBrandsAsync(brandNames, user, ct);

        var categoryPaths = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Category))
            .Select(r => r.Category!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categoryLeafMap = await ResolveCategoryPathsAsync(categoryPaths, user, ct);

        var unitNames = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Unit))
            .Select(r => r.Unit!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var unitMap = await ResolveUnitsAsync(unitNames, user, ct);

        await _db.SaveChangesAsync(ct);

        // ── Load the parts being updated (tracked, with their variants) ──────
        var updateIds = validRows
            .Where(r => IsUpdate(r, ctx))
            .Select(r => ctx.ProductIdBySku[r.Sku!.Trim()])
            .Distinct()
            .ToList();

        var partsById = updateIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await _db.Parts
                .Include(p => p.Variants)
                .Where(p => updateIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

        // ── Apply rows, grouped into one product each ────────────────────────
        var created = 0;
        var updated = 0;
        var createdVariants = 0;
        var updatedVariants = 0;

        // Selling prices that changed, recorded after the entities are saved.
        var newPrices = new List<(Guid PartId, Guid? VariantId, decimal Price, string Currency, string Reason)>();

        var newParts = new List<Product>();
        var newVariants = new List<ProductVariant>();

        foreach (var group in validRows.GroupBy(GroupKey, StringComparer.OrdinalIgnoreCase))
        {
            var groupRows = group.ToList();
            var firstRow = groupRows[0];
            var variantRows = groupRows.Where(r => r.HasVariantData).ToList();

            if (IsUpdate(firstRow, ctx))
            {
                var part = partsById[ctx.ProductIdBySku[firstRow.Sku!.Trim()]];
                var oldPartPrice = part.SellingPrice;

                ApplyPartUpdate(part, firstRow, brandMap, categoryLeafMap, unitMap, user);
                updated++;

                foreach (var vr in variantRows)
                {
                    var code = vr.VariantCode!.Trim();
                    var existing = part.Variants.FirstOrDefault(
                        v => string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase));

                    if (existing is null)
                    {
                        var variant = BuildVariant(part, vr, user);
                        part.Variants.Add(variant);
                        newVariants.Add(variant);
                        createdVariants++;

                        if (variant.SellingPrice > 0)
                            newPrices.Add((part.Id, variant.Id, variant.SellingPrice, variant.Currency, InitialPriceReason));
                    }
                    else
                    {
                        var oldVariantPrice = existing.SellingPrice;
                        ApplyVariantUpdate(existing, vr, user);
                        updatedVariants++;

                        if (existing.SellingPrice > 0 && existing.SellingPrice != oldVariantPrice)
                            newPrices.Add((part.Id, existing.Id, existing.SellingPrice, existing.Currency, PriceUpdateReason));
                    }
                }

                // Base-product price schedule only applies when the part has no variants of its own.
                if (part.Variants.Count == 0 && part.SellingPrice > 0 && part.SellingPrice != oldPartPrice)
                    newPrices.Add((part.Id, null, part.SellingPrice, part.SellingPriceCurrency, PriceUpdateReason));
            }
            else
            {
                var part = BuildPart(firstRow, brandMap, categoryLeafMap, unitMap, user);
                newParts.Add(part);
                created++;

                foreach (var vr in variantRows)
                {
                    var variant = BuildVariant(part, vr, user);
                    newVariants.Add(variant);
                    createdVariants++;

                    if (variant.SellingPrice > 0)
                        newPrices.Add((part.Id, variant.Id, variant.SellingPrice, variant.Currency, InitialPriceReason));
                }

                if (variantRows.Count == 0 && part.SellingPrice > 0)
                    newPrices.Add((part.Id, null, part.SellingPrice, part.SellingPriceCurrency, InitialPriceReason));
            }
        }

        // Parts first so variants have a parent row to point at.
        if (newParts.Count > 0)
            _db.Parts.AddRange(newParts);
        await _db.SaveChangesAsync(ct);

        if (newVariants.Count > 0)
        {
            foreach (var v in newVariants.Where(v => _db.Entry(v).State == EntityState.Detached))
                _db.ProductVariants.Add(v);
            await _db.SaveChangesAsync(ct);
        }

        await RecordPriceHistoryAsync(newPrices, user, ct);
        await _db.SaveChangesAsync(ct);

        result.CreatedCount = created;
        result.UpdatedCount = updated;
        result.CreatedVariantsCount = createdVariants;
        result.UpdatedVariantsCount = updatedVariants;
        result.CreatedBrandsCount = brandMap.Values.Count(b => b.IsNew);
        result.CreatedCategoriesCount = categoryLeafMap.Values.Count(c => c.IsNew);
        result.CreatedUnitsCount = unitMap.Values.Count(u => u.IsNew);
    }

    /// <summary>Closes the active price record for each changed price and opens a new one.</summary>
    private async Task RecordPriceHistoryAsync(
        List<(Guid PartId, Guid? VariantId, decimal Price, string Currency, string Reason)> prices,
        string user,
        CancellationToken ct)
    {
        if (prices.Count == 0) return;

        var partIds = prices.Select(p => p.PartId).Distinct().ToList();
        var active = await _db.ProductVariantPriceHistories
            .Where(h => partIds.Contains(h.PartId) && h.EndDate == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var (partId, variantId, price, currency, reason) in prices)
        {
            // Future-dated schedules are left alone — Close() would reject an end date
            // before their start, and a scheduled price change isn't ours to cancel.
            foreach (var open in active.Where(h =>
                h.PartId == partId && h.ProductVariantId == variantId && h.EndDate == null && h.StartDate <= now.Date))
                open.Close(now);

            var record = ProductVariantPriceHistory.Create(partId, price, now, variantId, currency, reason);
            record.CreatedBy = user;
            record.ModifiedBy = user;
            _db.ProductVariantPriceHistories.Add(record);
        }
    }

    // ── Existing-data lookups ──────────────────────────────────────────────────

    /// <summary>Snapshot of the identifiers already in the database, used by validation.</summary>
    private sealed class ImportContext
    {
        public Dictionary<string, Guid> ProductIdBySku { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Guid> ProductIdByPartNumber { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Variant part numbers mapped to their owning part — a variant PN is globally unique.</summary>
        public Dictionary<string, Guid> ProductIdByVariantPartNumber { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ImportContext> LoadContextAsync(CancellationToken ct)
    {
        var skus = await _db.Parts.AsNoTracking()
            .Select(p => new { p.Id, p.SKU })
            .ToListAsync(ct);

        var partNumbers = await _db.Parts.AsNoTracking()
            .Where(p => p.PartNumber != null)
            .Select(p => new { p.Id, PartNumber = p.PartNumber!.Value })
            .ToListAsync(ct);

        // ProductVariant.PartNumber is a converted property, so pull the value object and
        // read it client-side rather than projecting through the conversion.
        var variantPartNumbers = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.PartNumber != null)
            .Select(v => new { v.PartId, v.PartNumber })
            .ToListAsync(ct);

        var ctx = new ImportContext();

        foreach (var p in skus)
            if (!string.IsNullOrWhiteSpace(p.SKU))
                ctx.ProductIdBySku[p.SKU.Trim()] = p.Id;

        foreach (var p in partNumbers)
            if (!string.IsNullOrWhiteSpace(p.PartNumber))
                ctx.ProductIdByPartNumber[p.PartNumber.Trim()] = p.Id;

        foreach (var v in variantPartNumbers)
            if (!string.IsNullOrWhiteSpace(v.PartNumber?.Value))
                ctx.ProductIdByVariantPartNumber[v.PartNumber.Value.Trim()] = v.PartId;

        return ctx;
    }

    /// <summary>
    /// Works out which brands, category paths and units the batch would create, without
    /// writing anything. Mirrors the resolution rules used by the commit step.
    /// </summary>
    private async Task<(List<string> Brands, List<string> Categories, List<string> Units)>
        PreviewNewReferenceDataAsync(List<ProductImportRow> rows, CancellationToken ct)
    {
        var existingBrands = (await _db.Brands.AsNoTracking().Select(b => b.Name).ToListAsync(ct))
            .Select(Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingUnits = (await _db.Units.AsNoTracking().Select(u => u.Name).ToListAsync(ct))
            .Select(Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newBrands = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand))
            .Select(r => r.Brand!.Trim())
            .Where(b => !existingBrands.Contains(Key(b)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var newUnits = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Unit))
            .Select(r => r.Unit!.Trim())
            .Where(u => !existingUnits.Contains(Key(u)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var categories = await _db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.ParentCategoryId })
            .ToListAsync(ct);

        // name → id, keyed by parent (Guid.Empty = root level)
        var childrenByParent = categories
            .GroupBy(c => c.ParentCategoryId ?? Guid.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(c => Key(c.Name)).ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase));

        var newCategories = new List<string>();
        var alreadyListed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Category))
            .Select(r => r.Category!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var segments = path.Split(CategorySeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parent = Guid.Empty;
            var creating = false;
            var prefix = new List<string>();

            foreach (var segment in segments)
            {
                prefix.Add(segment);

                if (!creating
                    && childrenByParent.TryGetValue(parent, out var children)
                    && children.TryGetValue(Key(segment), out var existingId))
                {
                    parent = existingId;
                    continue;
                }

                // Once a level is missing, every level below it is new too.
                creating = true;
                var fullPrefix = string.Join(" > ", prefix);
                if (alreadyListed.Add(Key(fullPrefix)))
                    newCategories.Add(fullPrefix);
            }
        }

        return (newBrands, newCategories, newUnits);
    }

    // ── Reference entity resolution ────────────────────────────────────────────

    private async Task<Dictionary<string, RefEntity<Brand>>> ResolveBrandsAsync(
        List<string> names, string user, CancellationToken ct)
    {
        var existing = await _db.Brands.AsNoTracking()
            .ToDictionaryAsync(b => Key(b.Name), b => b, StringComparer.OrdinalIgnoreCase, ct);

        var result = new Dictionary<string, RefEntity<Brand>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            var key = Key(name);
            if (existing.TryGetValue(key, out var found))
            {
                result[key] = new RefEntity<Brand>(found, isNew: false);
            }
            else
            {
                var brand = Brand.Create(name);
                brand.CreatedBy = user;
                brand.ModifiedBy = user;
                _db.Brands.Add(brand);
                existing[key] = brand;
                result[key] = new RefEntity<Brand>(brand, isNew: true);
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves full category paths like "Brake System > Front Brakes" into entities,
    /// creating any missing levels in the hierarchy. Returns a map keyed by the full
    /// normalized path pointing to the leaf (deepest) category.
    /// </summary>
    private async Task<Dictionary<string, RefEntity<Category>>> ResolveCategoryPathsAsync(
        List<string> paths, string user, CancellationToken ct)
    {
        // Load ALL categories once so we can match by name at any depth
        var allCategories = await _db.Categories.AsNoTracking().ToListAsync(ct);
        var catByName = allCategories.ToDictionary(c => c.Id, c => c);
        var nameToEntities = allCategories
            .GroupBy(c => Key(c.Name))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, RefEntity<Category>>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            var segments = path.Split(CategorySeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) continue;

            Guid? parentId = null;
            var parentDepth = -1;

            for (var i = 0; i < segments.Length; i++)
            {
                var segKey = Key(segments[i]);
                var isLast = i == segments.Length - 1;

                // Try to find existing category at this level under the current parent
                Category? found = null;
                if (nameToEntities.TryGetValue(segKey, out var candidates))
                {
                    // For root level: match categories with no parent
                    // For nested levels: match categories whose parent matches
                    found = parentId is null
                        ? candidates.FirstOrDefault(c => c.ParentCategoryId is null)
                        : candidates.FirstOrDefault(c => c.ParentCategoryId == parentId);
                }

                if (found is not null)
                {
                    parentId = found.Id;
                    parentDepth = found.DepthLevel;

                    // Cache the full path key for the leaf
                    if (isLast)
                    {
                        var fullKey = Key(path);
                        if (!result.ContainsKey(fullKey))
                            result[fullKey] = new RefEntity<Category>(found, isNew: false);
                    }
                }
                else
                {
                    // Create missing category
                    var depth = parentDepth + 1;
                    var breadcrumb = parentId is null
                        ? segments[i]
                        : string.Join(" >", segments.Take(i + 1));
                    var newCat = Category.Create(segments[i], description: "",
                        parentCategoryId: parentId, breadcrumbPath: breadcrumb, depthLevel: depth);
                    newCat.CreatedBy = user;
                    newCat.ModifiedBy = user;
                    _db.Categories.Add(newCat);

                    // Update parent's child count
                    if (parentId is not null && catByName.TryGetValue(parentId.Value, out var parentCat))
                        parentCat.IncrementChildCount();

                    // Register in lookups so subsequent segments can find it
                    if (!nameToEntities.TryGetValue(segKey, out var bucket))
                    {
                        bucket = [];
                        nameToEntities[segKey] = bucket;
                    }
                    bucket.Add(newCat);
                    catByName[newCat.Id] = newCat;

                    parentId = newCat.Id;
                    parentDepth = newCat.DepthLevel;

                    if (isLast)
                        result[Key(path)] = new RefEntity<Category>(newCat, isNew: true);
                }
            }
        }

        return result;
    }

    private async Task<Dictionary<string, RefEntity<Unit>>> ResolveUnitsAsync(
        List<string> names, string user, CancellationToken ct)
    {
        var existing = await _db.Units.AsNoTracking()
            .ToDictionaryAsync(u => Key(u.Name), u => u, StringComparer.OrdinalIgnoreCase, ct);

        var result = new Dictionary<string, RefEntity<Unit>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            var key = Key(name);
            if (existing.TryGetValue(key, out var found))
            {
                result[key] = new RefEntity<Unit>(found, isNew: false);
            }
            else
            {
                var symbol = name.Length <= 5 ? name.Trim() : name.Trim()[..5];
                var unit = Unit.Create(name.Trim(), symbol);
                unit.CreatedBy = user;
                unit.ModifiedBy = user;
                _db.Units.Add(unit);
                existing[key] = unit;
                result[key] = new RefEntity<Unit>(unit, isNew: true);
            }
        }

        return result;
    }

    private sealed class RefEntity<T>(T entity, bool isNew)
    {
        public T Entity => entity;
        public bool IsNew => isNew;
    }

    // ── Part & Variant building ────────────────────────────────────────────────

    private Product BuildPart(
        ProductImportRow row,
        Dictionary<string, RefEntity<Brand>> brandMap,
        Dictionary<string, RefEntity<Category>> categoryLeafMap,
        Dictionary<string, RefEntity<Unit>> unitMap,
        string user)
    {
        var catKey = Key(row.Category!);
        if (!categoryLeafMap.TryGetValue(catKey, out var catRef))
            throw new InvalidOperationException($"Category '{row.Category}' could not be resolved");

        Guid? brandId = !string.IsNullOrWhiteSpace(row.Brand) && brandMap.TryGetValue(Key(row.Brand), out var brandRef)
            ? brandRef.Entity.Id : null;

        Guid? unitId = !string.IsNullOrWhiteSpace(row.Unit) && unitMap.TryGetValue(Key(row.Unit), out var unitRef)
            ? unitRef.Entity.Id : null;

        var sku = _codeGenerateService.GenerateAsync("SKU").GetAwaiter().GetResult();
        var partNumber = string.IsNullOrWhiteSpace(row.PartNumber)
            ? null
            : PartNumber.Create(row.PartNumber.Trim());

        var hasWarranty = row.HasWarranty ?? false;

        var part = Product.Create(
            row.Name!.Trim(), partNumber, sku, catRef.Entity.Id,
            brandId, unitId, unitId,
            row.Description?.Trim() ?? string.Empty,
            row.CostPrice ?? 0, row.SellingPrice ?? 0, row.MinimumStock ?? 0,
            hasWarranty, row.WarrantyPeriodMonths, row.WarrantyType,
            warrantyTerms: null, warrantyCertificateTemplate: null,
            row.Barcode?.Trim(), row.Tags?.Trim(),
            string.IsNullOrWhiteSpace(row.ProductType) ? "PHYSICAL" : row.ProductType.Trim().ToUpperInvariant(),
            isPerishable: false,
            row.WeightKg,
            row.TaxCode?.Trim(), row.OemNumber?.Trim(), row.LocalName?.Trim());

        part.CreatedBy = user;
        part.ModifiedBy = user;
        return part;
    }

    /// <summary>
    /// Applies an update row to an existing part. A blank cell leaves the current value
    /// alone — an import never clears data the spreadsheet simply didn't carry.
    /// </summary>
    private static void ApplyPartUpdate(
        Product part,
        ProductImportRow row,
        Dictionary<string, RefEntity<Brand>> brandMap,
        Dictionary<string, RefEntity<Category>> categoryLeafMap,
        Dictionary<string, RefEntity<Unit>> unitMap,
        string user)
    {
        var categoryId = !string.IsNullOrWhiteSpace(row.Category) && categoryLeafMap.TryGetValue(Key(row.Category), out var catRef)
            ? catRef.Entity.Id : part.CategoryId;

        var brandId = !string.IsNullOrWhiteSpace(row.Brand) && brandMap.TryGetValue(Key(row.Brand), out var brandRef)
            ? brandRef.Entity.Id : part.BrandId;

        var hasUnit = !string.IsNullOrWhiteSpace(row.Unit) && unitMap.TryGetValue(Key(row.Unit!), out var unitRef);
        var unitId = hasUnit ? unitMap[Key(row.Unit!)].Entity.Id : part.UnitId;
        var baseUnitId = hasUnit ? unitMap[Key(row.Unit!)].Entity.Id : part.BaseUnitId;

        var hasWarranty = row.HasWarranty ?? part.HasWarranty;

        part.Update(
            name: string.IsNullOrWhiteSpace(row.Name) ? part.Name : row.Name.Trim(),
            description: row.Description?.Trim() ?? part.Description,
            sku: part.SKU,                                    // the import key — never rewritten
            categoryId: categoryId,
            brandId: brandId,
            baseUnitId: baseUnitId,
            unitId: unitId,
            costPrice: row.CostPrice ?? part.CostPrice,
            sellingPrice: row.SellingPrice ?? part.SellingPrice,
            minimumStock: row.MinimumStock ?? part.MinimumStock,
            isActive: part.IsActive,
            hasWarranty: hasWarranty,
            warrantyPeriodMonths: row.WarrantyPeriodMonths ?? part.WarrantyPeriodMonths,
            warrantyType: string.IsNullOrWhiteSpace(row.WarrantyType) ? part.WarrantyType : row.WarrantyType.Trim(),
            warrantyTerms: part.WarrantyTerms,
            warrantyCertificateTemplate: part.WarrantyCertificateTemplate,
            barcode: row.Barcode?.Trim() ?? part.Barcode,
            tags: row.Tags?.Trim() ?? part.Tags,
            productType: string.IsNullOrWhiteSpace(row.ProductType) ? part.ProductType : row.ProductType.Trim().ToUpperInvariant(),
            isPerishable: part.IsPerishable,
            weightKg: row.WeightKg ?? part.WeightKg,
            taxCode: row.TaxCode?.Trim() ?? part.TaxCode,
            oemNumber: row.OemNumber?.Trim() ?? part.OemNumber,
            localName: row.LocalName?.Trim() ?? part.LocalName);

        if (!string.IsNullOrWhiteSpace(row.PartNumber))
            part.SetPartNumber(PartNumber.Create(row.PartNumber.Trim()));

        part.ModifiedBy = user;
    }

    private ProductVariant BuildVariant(Product parent, ProductImportRow row, string user)
    {
        var variantName = string.IsNullOrWhiteSpace(row.VariantName)
            ? parent.Name
            : row.VariantName!.Trim();
        var variantCode = row.VariantCode!.Trim().ToUpperInvariant();

        var costPrice = row.VariantCostPrice ?? row.CostPrice ?? parent.CostPrice;
        var sellingPrice = row.VariantSellingPrice ?? row.SellingPrice ?? parent.SellingPrice;

        var sku = _codeGenerateService.GenerateAsync("SKU").GetAwaiter().GetResult();

        PartNumber? variantPartNumber = null;
        if (!string.IsNullOrWhiteSpace(row.VariantPartNumber))
            variantPartNumber = PartNumber.Create(row.VariantPartNumber!.Trim());

        var variant = ProductVariant.Create(
            parent.Id, variantName, variantCode,
            costPrice, sellingPrice,
            sku,
            row.VariantBarcode?.Trim(),
            currency: "BDT",
            isActive: true,
            weightKg: row.WeightKg,
            partNumber: variantPartNumber,
            oemNumber: row.VariantOemNumber?.Trim());

        variant.CreatedBy = user;
        variant.ModifiedBy = user;
        return variant;
    }

    /// <summary>Applies an update row to an existing variant. Blank cells keep their current value.</summary>
    private static void ApplyVariantUpdate(ProductVariant variant, ProductImportRow row, string user)
    {
        var partNumber = string.IsNullOrWhiteSpace(row.VariantPartNumber)
            ? variant.PartNumber
            : PartNumber.Create(row.VariantPartNumber.Trim());

        variant.Update(
            name: string.IsNullOrWhiteSpace(row.VariantName) ? variant.Name : row.VariantName.Trim(),
            code: variant.Code,                               // matched on — never rewritten
            costPrice: row.VariantCostPrice ?? row.CostPrice ?? variant.CostPrice,
            sellingPrice: row.VariantSellingPrice ?? row.SellingPrice ?? variant.SellingPrice,
            sku: variant.SKU,
            barcode: row.VariantBarcode?.Trim() ?? variant.Barcode,
            currency: variant.Currency,
            isActive: variant.IsActive,
            weightKg: row.WeightKg ?? variant.WeightKg,
            partNumber: partNumber,
            oemNumber: row.VariantOemNumber?.Trim() ?? variant.OemNumber);

        variant.ModifiedBy = user;
    }

    // ── Validation ───────────────────────────────────────────────────────────────

    /// <summary>Per-file state carried across rows so duplicates within the upload are caught.</summary>
    private sealed class RowValidationState
    {
        /// <summary>Part number → the product group that claimed it (rows of one product share it).</summary>
        public Dictionary<string, string> PartNumberOwner { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> VariantPartNumbers { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>"groupKey|variantCode" — a code may appear once per product.</summary>
        public HashSet<string> VariantCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static ProductImportAction ValidateRow(
        ProductImportRow row,
        ProductImportMode mode,
        ImportContext ctx,
        RowValidationState state,
        List<string> errors)
    {
        var action = ProductImportAction.Create;
        Guid? targetPartId = null;

        // ── Import key ────────────────────────────────────────────────────────
        var sku = row.Sku?.Trim();
        if (!string.IsNullOrWhiteSpace(sku))
        {
            if (mode == ProductImportMode.CreateOnly)
            {
                errors.Add($"SKU '{sku}' can only be used in 'Create and update' mode. Leave SKU blank to create a new part.");
            }
            else if (sku.Length > 100)
            {
                errors.Add("SKU cannot exceed 100 characters");
            }
            else if (!ctx.ProductIdBySku.TryGetValue(sku, out var foundId))
            {
                errors.Add($"SKU '{sku}' was not found. Leave SKU blank to create a new part.");
            }
            else
            {
                action = ProductImportAction.Update;
                targetPartId = foundId;
            }
        }

        var isUpdate = action == ProductImportAction.Update;
        var groupKey = GroupKey(row);

        // ── Product-level fields ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.Name))
        {
            // Required for a new part; on an update the existing name is kept.
            if (!isUpdate) errors.Add("Name is required");
        }
        else if (row.Name.Trim().Length > 200)
        {
            errors.Add("Name cannot exceed 200 characters");
        }

        // Local Name is optional — the local-language label shown to staff (e.g. Bengali).
        if (row.LocalName?.Trim().Length > 200)
            errors.Add("Local Name cannot exceed 200 characters");

        // Part Number is optional — brands that don't publish a catalog code are
        // still identified by their auto-generated SKU.
        var pn = row.PartNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(pn))
        {
            if (pn.Length is < 3 or > 20)
                errors.Add("Part Number must be between 3 and 20 characters");
            else if (!char.IsLetter(pn[0]))
                errors.Add("Part Number must start with a letter");

            if (ctx.ProductIdByPartNumber.TryGetValue(pn, out var ownerId) && ownerId != targetPartId)
            {
                errors.Add($"Part Number '{pn}' already exists");
            }
            else if (state.PartNumberOwner.TryGetValue(pn, out var claimedBy) && !claimedBy.Equals(groupKey, StringComparison.OrdinalIgnoreCase))
            {
                // Rows of the same product legitimately repeat the part number — only a
                // different product reusing it is a duplicate.
                errors.Add($"Part Number '{pn}' is used by more than one part in the file");
            }
            else
            {
                state.PartNumberOwner[pn] = groupKey;
            }
        }

        // Category is required for a new part; on an update the existing one is kept.
        if (string.IsNullOrWhiteSpace(row.Category) && !isUpdate)
            errors.Add("Category is required");

        // ── Variant fields ────────────────────────────────────────────────────
        if (row.HasVariantData)
        {
            var code = row.VariantCode!.Trim();
            if (code.Length > 50)
                errors.Add("Variant Code cannot exceed 50 characters");
            else if (!state.VariantCodes.Add($"{groupKey}|{Key(code)}"))
                errors.Add($"Variant Code '{code}' appears more than once for the same part");

            var vpn = row.VariantPartNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(vpn))
            {
                if (vpn.Length is < 3 or > 20)
                {
                    errors.Add("Variant Part Number must be between 3 and 20 characters");
                }
                else if (!char.IsLetter(vpn[0]))
                {
                    errors.Add("Variant Part Number must start with a letter");
                }
                else
                {
                    // Unique against both base products and other variants (the DB enforces
                    // a filtered unique index on each), except on the part being updated.
                    var clashesWithPart = ctx.ProductIdByPartNumber.TryGetValue(vpn, out var partOwner) && partOwner != targetPartId;
                    var clashesWithVariant = ctx.ProductIdByVariantPartNumber.TryGetValue(vpn, out var variantOwner) && variantOwner != targetPartId;

                    if (clashesWithPart || clashesWithVariant)
                        errors.Add($"Variant Part Number '{vpn}' already exists");
                    else if (!state.VariantPartNumbers.Add(vpn))
                        errors.Add($"Variant Part Number '{vpn}' is duplicated within the file");
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(row.VariantName))
        {
            errors.Add("Variant Code is required when providing variant data");
        }

        // ── Numbers, enums, warranty ──────────────────────────────────────────
        if (row.CostPrice is < 0) errors.Add("Cost Price cannot be negative");
        if (row.SellingPrice is < 0) errors.Add("Selling Price cannot be negative");
        if (row.MinimumStock is < 0) errors.Add("Minimum Stock cannot be negative");
        if (row.WeightKg is < 0) errors.Add("Weight cannot be negative");
        if (row.VariantCostPrice is < 0) errors.Add("Variant Cost Price cannot be negative");
        if (row.VariantSellingPrice is < 0) errors.Add("Variant Selling Price cannot be negative");

        if (!string.IsNullOrWhiteSpace(row.ProductType) &&
            !ValidProductTypes.Contains(row.ProductType.Trim().ToUpperInvariant()))
            errors.Add("Product Type must be PHYSICAL, DIGITAL, or SERVICE");

        if (row.HasWarranty == true)
        {
            if (row.WarrantyPeriodMonths is null or <= 0)
                errors.Add("Warranty Period (months) is required and must be greater than 0 when Has Warranty is TRUE");
            if (string.IsNullOrWhiteSpace(row.WarrantyType))
                errors.Add("Warranty Type is required when Has Warranty is TRUE");
        }

        return action;
    }

    /// <summary>
    /// Identity of the product a row belongs to: the SKU when updating, otherwise
    /// Name + Part Number. Rows sharing a key are one product with several variants.
    /// </summary>
    private static string GroupKey(ProductImportRow row)
        => !string.IsNullOrWhiteSpace(row.Sku)
            ? $"sku:{Key(row.Sku)}"
            : $"new:{Key(row.Name ?? string.Empty)}|{Key(row.PartNumber ?? string.Empty)}";

    private static bool IsUpdate(ProductImportRow row, ImportContext ctx)
        => !string.IsNullOrWhiteSpace(row.Sku) && ctx.ProductIdBySku.ContainsKey(row.Sku.Trim());

    // ── Parsing ────────────────────────────────────────────────────────────────

    private static List<(ProductImportRow Row, List<string> ParseErrors)> ParseRows(Stream xlsxStream)
    {
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The workbook contains no worksheets.");

        var headerRow = ws.FirstRowUsed()
            ?? throw new InvalidOperationException("The worksheet is empty.");

        var columnByHeader = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrEmpty(header))
                columnByHeader.TryAdd(header, cell.Address.ColumnNumber);
        }

        int? Col(string canonicalHeader)
            => columnByHeader.TryGetValue(NormalizeHeader(canonicalHeader), out var c) ? c : null;

        var rows = new List<(ProductImportRow, List<string>)>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
        {
            var xlRow = ws.Row(r);
            if (xlRow.IsEmpty()) continue;

            var parseErrors = new List<string>();
            var row = new ProductImportRow
            {
                RowNumber = r,
                Sku = Str(xlRow, Col("SKU")),
                Name = Str(xlRow, Col("Name*")),
                LocalName = Str(xlRow, Col("Local Name")),
                PartNumber = Str(xlRow, Col("Part Number")),
                Category = Str(xlRow, Col("Category*")),
                Brand = Str(xlRow, Col("Brand")),
                Unit = Str(xlRow, Col("Unit")),
                CostPrice = Dec(xlRow, Col("Cost Price"), "Cost Price", parseErrors),
                SellingPrice = Dec(xlRow, Col("Selling Price"), "Selling Price", parseErrors),
                MinimumStock = Int(xlRow, Col("Minimum Stock"), "Minimum Stock", parseErrors),
                Barcode = Str(xlRow, Col("Barcode")),
                OemNumber = Str(xlRow, Col("OEM Number")),
                Tags = Str(xlRow, Col("Tags")),
                Description = Str(xlRow, Col("Description")),
                ProductType = Str(xlRow, Col("Product Type")),
                TaxCode = Str(xlRow, Col("Tax Code")),
                HasWarranty = Bool(xlRow, Col("Has Warranty"), "Has Warranty", parseErrors),
                WarrantyPeriodMonths = Int(xlRow, Col("Warranty Period (months)"), "Warranty Period (months)", parseErrors),
                WarrantyType = Str(xlRow, Col("Warranty Type")),
                WeightKg = Dec(xlRow, Col("Weight (kg)"), "Weight (kg)", parseErrors),
                VariantName = Str(xlRow, Col("Variant Name")),
                VariantCode = Str(xlRow, Col("Variant Code")),
                VariantPartNumber = Str(xlRow, Col("Variant Part Number")),
                VariantOemNumber = Str(xlRow, Col("Variant OEM Number")),
                VariantBarcode = Str(xlRow, Col("Variant Barcode")),
                VariantCostPrice = Dec(xlRow, Col("Variant Cost Price"), "Variant Cost Price", parseErrors),
                VariantSellingPrice = Dec(xlRow, Col("Variant Selling Price"), "Variant Selling Price", parseErrors)
            };

            rows.Add((row, parseErrors));
        }

        return rows;
    }

    private static string NormalizeHeader(string? header)
        => (header ?? string.Empty).Replace("*", string.Empty).Trim().ToLowerInvariant();

    private static string? Str(IXLRow row, int? col)
    {
        if (col is null) return null;
        var value = row.Cell(col.Value).GetString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal? Dec(IXLRow row, int? col, string field, List<string> errors)
    {
        if (col is null) return null;
        var cell = row.Cell(col.Value);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<decimal>(out var d)) return d;
        var s = cell.GetString().Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        errors.Add($"{field} is not a valid number ('{s}')");
        return null;
    }

    private static int? Int(IXLRow row, int? col, string field, List<string> errors)
    {
        if (col is null) return null;
        var cell = row.Cell(col.Value);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<int>(out var i)) return i;
        var s = cell.GetString().Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)) return (int)dec;
        errors.Add($"{field} is not a valid whole number ('{s}')");
        return null;
    }

    private static bool? Bool(IXLRow row, int? col, string field, List<string> errors)
    {
        if (col is null) return null;
        var cell = row.Cell(col.Value);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<bool>(out var b)) return b;
        var s = cell.GetString().Trim().ToLowerInvariant();
        return s switch
        {
            "" => null,
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => Fail()
        };

        bool? Fail()
        {
            errors.Add($"{field} must be TRUE or FALSE ('{cell.GetString().Trim()}')");
            return null;
        }
    }

    private static string Key(string value) => value.Trim().ToLowerInvariant();
}
