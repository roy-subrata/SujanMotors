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

    // Canonical column headers, in template order. A trailing "*" marks a required column.
    private static readonly string[] Headers =
    [
        "Name*",
        "Part Number*",
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
        "Depth (cm)"
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

        // Example row to show the expected format.
        var example = new[]
        {
            "Front Brake Pad Set", "BP-1001", "Brake System", "Bosch", "Pieces",
            "450", "650", "10", "8901234567890", "OEM-77231", "brake,pad,front",
            "Ceramic front brake pad set", "PHYSICAL", "STANDARD",
            "TRUE", "12", "MANUFACTURER", "1.2", "15", "8", "20"
        };
        for (var i = 0; i < example.Length; i++)
            ws.Cell(2, i + 1).Value = example[i];

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
        var ctx = await BuildLookupsAsync(cancellationToken);

        var seenPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new ProductImportValidationResult { TotalRows = rows.Count };

        foreach (var (row, parseErrors) in rows)
        {
            var errors = new List<string>(parseErrors);
            ValidateRow(row, ctx, seenPartNumbers, errors);

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
        var ctx = await BuildLookupsAsync(cancellationToken);
        var seenPartNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var user = _currentUserService.GetCurrentUsername();

        var result = new ProductImportCommitResult();
        var toCreate = new List<Product>();

        foreach (var row in rows.OrderBy(r => r.RowNumber))
        {
            var errors = new List<string>();
            ValidateRow(row, ctx, seenPartNumbers, errors);

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
                continue;
            }

            try
            {
                var part = await BuildPartAsync(row, ctx, user, cancellationToken);
                toCreate.Add(part);
            }
            catch (Exception ex)
            {
                result.Failures.Add(new ProductImportRowResult
                {
                    RowNumber = row.RowNumber,
                    Name = row.Name,
                    PartNumber = row.PartNumber,
                    IsValid = false,
                    Errors = [ex.Message],
                    Row = row
                });
            }
        }

        if (toCreate.Count > 0)
        {
            _db.Parts.AddRange(toCreate);

            foreach (var part in toCreate.Where(p => p.SellingPrice > 0))
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

        result.CreatedCount = toCreate.Count;
        result.FailedCount = result.Failures.Count;

        _logger.LogInformation("Product import committed: {Created} created, {Failed} failed by {User}",
            result.CreatedCount, result.FailedCount, user);

        return result;
    }

    private async Task<Product> BuildPartAsync(ProductImportRow row, LookupContext ctx, string user, CancellationToken ct)
    {
        var category = ctx.Categories[Key(row.Category!)];
        Guid? brandId = !string.IsNullOrWhiteSpace(row.Brand) ? ctx.Brands[Key(row.Brand)].Id : null;
        Guid? unitId = !string.IsNullOrWhiteSpace(row.Unit) ? ctx.Units[Key(row.Unit)].Id : null;

        var sku = await _codeGenerateService.GenerateAsync("SKU", ct);
        var partNumber = PartNumber.Create(row.PartNumber!.Trim());

        var hasWarranty = row.HasWarranty ?? false;

        var part = Product.Create(
            row.Name!.Trim(), partNumber, sku, category.Id,
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

    // ── Validation ───────────────────────────────────────────────────────────────

    private static void ValidateRow(ProductImportRow row, LookupContext ctx, HashSet<string> seenPartNumbers, List<string> errors)
    {
        // Name
        if (string.IsNullOrWhiteSpace(row.Name))
            errors.Add("Name is required");
        else if (row.Name.Trim().Length > 200)
            errors.Add("Name cannot exceed 200 characters");

        // Part number
        var pn = row.PartNumber?.Trim();
        if (string.IsNullOrWhiteSpace(pn))
        {
            errors.Add("Part Number is required");
        }
        else
        {
            if (pn.Length is < 3 or > 20)
                errors.Add("Part Number must be between 3 and 20 characters");
            else if (!char.IsLetter(pn[0]))
                errors.Add("Part Number must start with a letter");

            if (ctx.ExistingPartNumbers.Contains(pn))
                errors.Add($"Part Number '{pn}' already exists");
            else if (!seenPartNumbers.Add(pn))
                errors.Add($"Part Number '{pn}' is duplicated within the file");
        }

        // Category (required, must exist)
        if (string.IsNullOrWhiteSpace(row.Category))
            errors.Add("Category is required");
        else if (!ctx.Categories.ContainsKey(Key(row.Category)))
            errors.Add($"Category '{row.Category.Trim()}' was not found");

        // Brand (optional, must exist if given)
        if (!string.IsNullOrWhiteSpace(row.Brand) && !ctx.Brands.ContainsKey(Key(row.Brand)))
            errors.Add($"Brand '{row.Brand.Trim()}' was not found");

        // Unit (optional, must exist if given)
        if (!string.IsNullOrWhiteSpace(row.Unit) && !ctx.Units.ContainsKey(Key(row.Unit)))
            errors.Add($"Unit '{row.Unit.Trim()}' was not found");

        // Numerics
        if (row.CostPrice is < 0) errors.Add("Cost Price cannot be negative");
        if (row.SellingPrice is < 0) errors.Add("Selling Price cannot be negative");
        if (row.MinimumStock is < 0) errors.Add("Minimum Stock cannot be negative");
        if (row.WeightKg is < 0) errors.Add("Weight cannot be negative");

        // Product type
        if (!string.IsNullOrWhiteSpace(row.ProductType) &&
            !ValidProductTypes.Contains(row.ProductType.Trim().ToUpperInvariant()))
            errors.Add("Product Type must be PHYSICAL, DIGITAL, or SERVICE");

        // Warranty
        if (row.HasWarranty == true)
        {
            if (row.WarrantyPeriodMonths is null or <= 0)
                errors.Add("Warranty Period (months) is required and must be greater than 0 when Has Warranty is TRUE");
            if (string.IsNullOrWhiteSpace(row.WarrantyType))
                errors.Add("Warranty Type is required when Has Warranty is TRUE");
        }
    }

    // ── Lookups ────────────────────────────────────────────────────────────────

    private async Task<LookupContext> BuildLookupsAsync(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(ct);
        var brands = await _db.Brands.AsNoTracking().ToListAsync(ct);
        var units = await _db.Units.AsNoTracking().ToListAsync(ct);
        var partNumbers = await _db.Parts.AsNoTracking().Select(p => p.PartNumber.Value).ToListAsync(ct);

        return new LookupContext
        {
            Categories = BuildNameMap(categories, c => c.Name),
            Brands = BuildNameMap(brands, b => b.Name),
            Units = BuildNameMap(units, u => u.Name),
            ExistingPartNumbers = new HashSet<string>(partNumbers, StringComparer.OrdinalIgnoreCase)
        };
    }

    // Map by normalized name; first occurrence wins if names collide.
    private static Dictionary<string, T> BuildNameMap<T>(IEnumerable<T> items, Func<T, string> nameSelector)
    {
        var map = new Dictionary<string, T>();
        foreach (var item in items)
        {
            var key = Key(nameSelector(item));
            if (!string.IsNullOrEmpty(key))
                map.TryAdd(key, item);
        }
        return map;
    }

    private static string Key(string value) => value.Trim().ToLowerInvariant();

    private sealed class LookupContext
    {
        public required Dictionary<string, Category> Categories { get; init; }
        public required Dictionary<string, Brand> Brands { get; init; }
        public required Dictionary<string, Unit> Units { get; init; }
        public required HashSet<string> ExistingPartNumbers { get; init; }
    }

    // ── Parsing ────────────────────────────────────────────────────────────────

    private static List<(ProductImportRow Row, List<string> ParseErrors)> ParseRows(Stream xlsxStream)
    {
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The workbook contains no worksheets.");

        // Build header → column index map (normalized, '*' and case ignored).
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
                PartNumber = Str(xlRow, Col("Part Number*")),
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
                WeightKg = Dec(xlRow, Col("Weight (kg)"), "Weight (kg)", parseErrors)
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
}
