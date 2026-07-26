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

    // Canonical column headers, in template order. A trailing "*" marks a required column.
    private static readonly string[] Headers =
    [
        "Name*",
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
        "Width (cm)",
        "Height (cm)",
        "Depth (cm)",
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

        var example1 = new[]
        {
            "Front Brake Pad Set", "BP-1001", "Brake System > Front Brakes", "Bosch", "Pieces",
            "450", "650", "10", "8901234567890", "OEM-77231", "brake,pad,front",
            "Ceramic front brake pad set", "PHYSICAL", "STANDARD",
            "TRUE", "12", "MANUFACTURER", "1.2", "15", "8", "20",
            "Standard", "BP-STD", "", "", "", "450", "650"
        };
        var example2 = new[]
        {
            "Front Brake Pad Set", "BP-1001", "Brake System > Front Brakes", "Bosch", "Pieces",
            "", "", "", "", "", "",
            "", "", "",
            "", "", "", "", "", "", "",
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

    // ── Validate (dry-run) ───────────────────────────────────────────────────────

    public async Task<ProductImportValidationResult> ValidateAsync(Stream xlsxStream, CancellationToken cancellationToken = default)
    {
        var rows = ParseRows(xlsxStream);
        var existingPartNumbers = await _db.Parts.AsNoTracking()
            .Where(p => p.PartNumber != null)
            .Select(p => p.PartNumber!.Value)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        var seenPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenVariantPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new ProductImportValidationResult { TotalRows = rows.Count };

        foreach (var (row, parseErrors) in rows)
        {
            var errors = new List<string>(parseErrors);
            ValidateRow(row, existingPartNumbers, seenPartNumbers, seenVariantPartNumbers, errors);

            var ok = errors.Count == 0;
            result.Rows.Add(new ProductImportRowResult
            {
                RowNumber = row.RowNumber,
                Name = row.Name,
                PartNumber = row.PartNumber,
                IsValid = ok,
                Errors = errors,
                Row = ok ? row : null
            });
        }

        result.ValidCount = result.Rows.Count(r => r.IsValid);
        result.ErrorCount = result.Rows.Count - result.ValidCount;
        return result;
    }

    // ── Commit ─────────────────────────────────────────────────────────────────

    public async Task<ProductImportCommitResult> CommitAsync(IEnumerable<ProductImportRow> rows, CancellationToken cancellationToken = default)
    {
        var orderedRows = rows.OrderBy(r => r.RowNumber).ToList();

        var existingPartNumbers = await _db.Parts.AsNoTracking()
            .Where(p => p.PartNumber != null)
            .Select(p => p.PartNumber!.Value)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        var seenPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenVariantPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var user = _currentUserService.GetCurrentUsername();

        var result = new ProductImportCommitResult();

        // Pre-validate
        var validRows = new List<ProductImportRow>();
        foreach (var row in orderedRows)
        {
            var errors = new List<string>();
            ValidateRow(row, existingPartNumbers, seenPartNumbers, seenVariantPartNumbers, errors);

            if (errors.Count > 0)
            {
                result.Failures.Add(new ProductImportRowResult
                {
                    RowNumber = row.RowNumber,
                    Name = row.Name,
                    PartNumber = row.PartNumber,
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

        // ── Resolve reference entities (find-or-create) ──────────────────────

        // Brands
        var brandNames = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand))
            .Select(r => r.Brand!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var brandMap = await ResolveBrandsAsync(brandNames, user, cancellationToken);
        result.CreatedBrandsCount = brandMap.Values.Count(b => b.IsNew);

        // Categories — collect all unique full paths, resolve hierarchy
        var categoryPaths = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Category))
            .Select(r => r.Category!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categoryLeafMap = await ResolveCategoryPathsAsync(categoryPaths, user, cancellationToken);
        result.CreatedCategoriesCount = categoryLeafMap.Values.Count(c => c.IsNew);

        // Units
        var unitNames = validRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Unit))
            .Select(r => r.Unit!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var unitMap = await ResolveUnitsAsync(unitNames, user, cancellationToken);
        result.CreatedUnitsCount = unitMap.Values.Count(u => u.IsNew);

        // Persist reference entities
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save reference entities during import");
            result.FailedCount = validRows.Count;
            foreach (var r in validRows)
            {
                result.Failures.Add(new ProductImportRowResult
                {
                    RowNumber = r.RowNumber,
                    Name = r.Name,
                    PartNumber = r.PartNumber,
                    IsValid = false,
                    Errors = [$"Failed to save reference data: {ex.InnerException?.Message ?? ex.Message}"],
                    Row = r
                });
            }
            return result;
        }

        // ── Group rows by product (Name + PartNumber) ────────────────────────

        var productGroups = validRows
            .GroupBy(r => $"{Key(r.Name ?? "")}|{Key(r.PartNumber ?? "")}")
            .ToList();

        var toCreateProducts = new List<Product>();
        var variantsByProduct = new Dictionary<Guid, List<ProductVariant>>();

        foreach (var group in productGroups)
        {
            var groupRows = group.ToList();
            var firstRow = groupRows[0];

            try
            {
                var part = BuildPart(firstRow, brandMap, categoryLeafMap, unitMap, user);
                toCreateProducts.Add(part);

                var variantRows = groupRows.Where(r => r.HasVariantData).ToList();
                if (variantRows.Count > 0)
                {
                    var variants = new List<ProductVariant>();
                    foreach (var vr in variantRows)
                        variants.Add(BuildVariant(part, vr, user));
                    variantsByProduct[part.Id] = variants;
                }
            }
            catch (Exception ex)
            {
                foreach (var r in groupRows)
                {
                    result.Failures.Add(new ProductImportRowResult
                    {
                        RowNumber = r.RowNumber,
                        Name = r.Name,
                        PartNumber = r.PartNumber,
                        IsValid = false,
                        Errors = [ex.Message],
                        Row = r
                    });
                }
            }
        }

        if (toCreateProducts.Count > 0)
        {
            try
            {
                _db.Parts.AddRange(toCreateProducts);
                await _db.SaveChangesAsync(cancellationToken);

                var allVariants = new List<ProductVariant>();
                foreach (var (_, variants) in variantsByProduct)
                {
                    foreach (var v in variants)
                    {
                        _db.ProductVariants.Add(v);
                        allVariants.Add(v);
                    }
                }

                if (allVariants.Count > 0)
                {
                    await _db.SaveChangesAsync(cancellationToken);

                    foreach (var v in allVariants.Where(v => v.SellingPrice > 0))
                    {
                        var priceHistory = ProductVariantPriceHistory.Create(
                            v.PartId, v.SellingPrice, DateTime.UtcNow, v.Id,
                            v.Currency, "INITIAL_PRICE");
                        priceHistory.CreatedBy = user;
                        priceHistory.ModifiedBy = user;
                        _db.ProductVariantPriceHistories.Add(priceHistory);
                    }
                }

                foreach (var part in toCreateProducts.Where(p => p.SellingPrice > 0 && !variantsByProduct.ContainsKey(p.Id)))
                {
                    var priceHistory = ProductVariantPriceHistory.Create(
                        part.Id, part.SellingPrice, DateTime.UtcNow, null,
                        part.SellingPriceCurrency, "INITIAL_PRICE");
                    priceHistory.CreatedBy = user;
                    priceHistory.ModifiedBy = user;
                    _db.ProductVariantPriceHistories.Add(priceHistory);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save products/variants during import");
                result.FailedCount = toCreateProducts.Count + result.Failures.Count;
                result.Failures.Add(new ProductImportRowResult
                {
                    RowNumber = 0,
                    IsValid = false,
                    Errors = [$"Failed to save products: {ex.InnerException?.Message ?? ex.Message}"]
                });
                return result;
            }
        }

        result.CreatedCount = toCreateProducts.Count;
        result.CreatedVariantsCount = variantsByProduct.Values.Sum(v => v.Count);
        result.FailedCount = result.Failures.Count;

        _logger.LogInformation(
            "Product import committed: {Created} parts, {Variants} variants, {Brands} brands, {Categories} categories, {Units} units by {User}",
            result.CreatedCount, result.CreatedVariantsCount, result.CreatedBrandsCount, result.CreatedCategoriesCount, result.CreatedUnitsCount, user);

        return result;
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
        var newlyCreated = new List<Category>();

        foreach (var path in paths)
        {
            var segments = path.Split(CategorySeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) continue;

            Guid? parentId = null;
            var parentDepth = -1;
            Category? leaf = null;

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
                    if (parentId is null)
                    {
                        found = candidates.FirstOrDefault(c => c.ParentCategoryId is null);
                    }
                    else
                    {
                        found = candidates.FirstOrDefault(c => c.ParentCategoryId == parentId);
                    }
                }

                if (found is not null)
                {
                    leaf = found;
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
                    newlyCreated.Add(newCat);

                    // Update parent's child count
                    if (parentId is not null && catByName.TryGetValue(parentId.Value, out var parentCat))
                        parentCat.IncrementChildCount();

                    // Register in lookups so subsequent segments can find it
                    if (!nameToEntities.ContainsKey(segKey))
                        nameToEntities[segKey] = new List<Category>();
                    nameToEntities[segKey].Add(newCat);
                    catByName[newCat.Id] = newCat;

                    leaf = newCat;
                    parentId = newCat.Id;
                    parentDepth = newCat.DepthLevel;

                    if (isLast)
                    {
                        var fullKey = Key(path);
                        result[fullKey] = new RefEntity<Category>(newCat, isNew: true);
                    }
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

        Guid? brandId = !string.IsNullOrWhiteSpace(row.Brand) && brandMap.ContainsKey(Key(row.Brand!))
            ? brandMap[Key(row.Brand!)].Entity.Id : null;

        Guid? unitId = !string.IsNullOrWhiteSpace(row.Unit) && unitMap.ContainsKey(Key(row.Unit!))
            ? unitMap[Key(row.Unit!)].Entity.Id : null;

        var sku = _codeGenerateService.GenerateAsync("SKU").GetAwaiter().GetResult();
        var partNumber = string.IsNullOrWhiteSpace(row.PartNumber)
            ? null
            : PartNumber.Create(row.PartNumber.Trim());

        var hasWarranty = row.HasWarranty ?? false;

        var part = Product.Create(
            row.Name!.Trim(), partNumber, sku, catRef.Entity.Id,
            brandId, unitId, unitId,
            row.Description?.Trim() ?? string.Empty, richDescription: null,
            row.CostPrice ?? 0, row.SellingPrice ?? 0, row.MinimumStock ?? 0,
            hasWarranty, row.WarrantyPeriodMonths, row.WarrantyType,
            warrantyTerms: null, warrantyCertificateTemplate: null,
            row.Barcode?.Trim(), row.Tags?.Trim(),
            string.IsNullOrWhiteSpace(row.ProductType) ? "PHYSICAL" : row.ProductType.Trim().ToUpperInvariant(),
            isPerishable: false,
            row.WeightKg,
            row.TaxCode?.Trim(), row.OemNumber?.Trim());

        part.CreatedBy = user;
        part.ModifiedBy = user;
        return part;
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

    // ── Validation ───────────────────────────────────────────────────────────────

    private static void ValidateRow(
        ProductImportRow row,
        HashSet<string> existingPartNumbers,
        HashSet<string> seenPartNumbers,
        HashSet<string> seenVariantPartNumbers,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.Name))
            errors.Add("Name is required");
        else if (row.Name.Trim().Length > 200)
            errors.Add("Name cannot exceed 200 characters");

        // Part Number is optional — brands that don't publish a catalog code are
        // still identified by their auto-generated SKU.
        var pn = row.PartNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(pn))
        {
            if (pn.Length is < 3 or > 20)
                errors.Add("Part Number must be between 3 and 20 characters");
            else if (!char.IsLetter(pn[0]))
                errors.Add("Part Number must start with a letter");

            if (existingPartNumbers.Contains(pn))
                errors.Add($"Part Number '{pn}' already exists");
            else if (!seenPartNumbers.Add(pn))
                errors.Add($"Part Number '{pn}' is duplicated within the file");
        }

        if (string.IsNullOrWhiteSpace(row.Category))
            errors.Add("Category is required");

        // Variant validation
        var hasVariantCols = row.HasVariantData || !string.IsNullOrWhiteSpace(row.VariantName);
        if (row.HasVariantData)
        {
            if (string.IsNullOrWhiteSpace(row.VariantCode))
                errors.Add("Variant Code is required when providing variant data");
            else if (row.VariantCode.Trim().Length > 50)
                errors.Add("Variant Code cannot exceed 50 characters");

            var vpn = row.VariantPartNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(vpn))
            {
                if (vpn.Length is < 3 or > 20)
                    errors.Add("Variant Part Number must be between 3 and 20 characters");
                else if (!char.IsLetter(vpn[0]))
                    errors.Add("Variant Part Number must start with a letter");
                else if (existingPartNumbers.Contains(vpn) || !seenVariantPartNumbers.Add(vpn))
                    errors.Add($"Variant Part Number '{vpn}' already exists");
            }
        }
        else if (hasVariantCols && !row.HasVariantData)
        {
            errors.Add("Variant Code is required when providing variant data");
        }

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
    }

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
                Name = Str(xlRow, Col("Name*")),
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
