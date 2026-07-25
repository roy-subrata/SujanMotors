namespace AutoPartShop.Application.DTOs.PartDtos;

/// <summary>
/// A single parsed row from the import spreadsheet. Foreign keys (category, brand, unit)
/// are referenced by name and resolved server-side. Used as both the parse result of the
/// validate step and the payload of the commit step (round-tripped through the client).
///
/// Category supports hierarchy via ">" separator (e.g., "Brake System > Front Brakes").
/// Rows with the same Name + PartNumber are grouped as one product. Variant columns
/// (Variant Name, Variant Code, etc.) create ProductVariant records under that product.
/// If no variant columns are filled, no DB variant records are created (the API synthesizes
/// a "Default" variant in responses).
/// </summary>
public class ProductImportRow
{
    /// <summary>1-based row number in the source spreadsheet (header is row 1, first data row is 2).</summary>
    public int RowNumber { get; set; }

    // ── Product-level fields ───────────────────────────────────────────────────
    public string? Name { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }  // supports "Parent > Child > GrandChild"
    public string? Brand { get; set; }
    public string? Unit { get; set; }

    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public int? MinimumStock { get; set; }

    public string? Barcode { get; set; }
    public string? OemNumber { get; set; }
    public string? Tags { get; set; }
    public string? Description { get; set; }
    public string? ProductType { get; set; }
    public string? TaxCode { get; set; }

    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }

    public decimal? WeightKg { get; set; }

    // ── Variant-level fields (optional) ────────────────────────────────────────
    public string? VariantName { get; set; }
    public string? VariantCode { get; set; }
    public string? VariantPartNumber { get; set; }
    public string? VariantOemNumber { get; set; }
    public string? VariantBarcode { get; set; }
    public decimal? VariantCostPrice { get; set; }
    public decimal? VariantSellingPrice { get; set; }

    /// <summary>Whether this row carries variant data (has a VariantCode).</summary>
    public bool HasVariantData => !string.IsNullOrWhiteSpace(VariantCode);
}

/// <summary>Per-row outcome of validation or commit.</summary>
public class ProductImportRowResult
{
    public int RowNumber { get; set; }
    public string? Name { get; set; }
    public string? PartNumber { get; set; }
    public bool IsValid { get; set; }

    /// <summary>Human-readable validation errors for this row (empty when valid).</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>The validated row data, echoed back so the client can submit it on commit.</summary>
    public ProductImportRow? Row { get; set; }
}

/// <summary>Result of the validate (dry-run) step. Nothing is written to the database.</summary>
public class ProductImportValidationResult
{
    public int TotalRows { get; set; }
    public int ValidCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ProductImportRowResult> Rows { get; set; } = [];
}

/// <summary>Payload for the commit step: the rows the user confirmed for import.</summary>
public class ProductImportCommitRequest
{
    public List<ProductImportRow> Rows { get; set; } = [];
}

/// <summary>Result of the commit step.</summary>
public class ProductImportCommitResult
{
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public int CreatedBrandsCount { get; set; }
    public int CreatedCategoriesCount { get; set; }
    public int CreatedUnitsCount { get; set; }
    public int CreatedVariantsCount { get; set; }

    /// <summary>Rows that failed during commit (re-validation or persistence errors).</summary>
    public List<ProductImportRowResult> Failures { get; set; } = [];
}
