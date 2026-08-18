namespace AutoPartShop.Application.DTOs.PartDtos;

/// <summary>
/// How an import run is allowed to affect existing parts.
/// </summary>
public enum ProductImportMode
{
    /// <summary>Only create new parts. A row carrying a SKU is rejected. The safe default.</summary>
    CreateOnly = 0,

    /// <summary>Rows with a SKU update that part; rows without one create a new part.</summary>
    CreateAndUpdate = 1
}

/// <summary>What the import will do with a given row.</summary>
public enum ProductImportAction
{
    Create = 0,
    Update = 1
}

/// <summary>
/// A single parsed row from the import spreadsheet. Foreign keys (category, brand, unit)
/// are referenced by name and resolved server-side. Used as both the parse result of the
/// validate step and the payload of the commit step (round-tripped through the client).
///
/// SKU is the import key: blank creates a new part, filled matches an existing one and
/// updates it (only in <see cref="ProductImportMode.CreateAndUpdate"/>). On an update, a
/// blank cell means "leave this field as it is" — it never clears existing data.
///
/// Category supports hierarchy via ">" separator (e.g., "Brake System > Front Brakes").
/// Rows are grouped into one product by SKU, or by Name + PartNumber when no SKU is given.
/// Variant columns (Variant Name, Variant Code, etc.) create or update ProductVariant
/// records under that product, matched by Variant Code.
/// </summary>
public class ProductImportRow
{
    /// <summary>1-based row number in the source spreadsheet (header is row 1, first data row is 2).</summary>
    public int RowNumber { get; set; }

    // ── Product-level fields ───────────────────────────────────────────────────

    /// <summary>Import key. Blank = create a new part; filled = update the part with this SKU.</summary>
    public string? Sku { get; set; }

    public string? Name { get; set; }

    /// <summary>Local-language name (e.g. Bengali) shown to staff. Optional.</summary>
    public string? LocalName { get; set; }

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

    /// <summary>The SKU the row targets, when it is an update.</summary>
    public string? Sku { get; set; }

    /// <summary>Whether this row creates a new part or updates an existing one.</summary>
    public ProductImportAction Action { get; set; } = ProductImportAction.Create;

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

    /// <summary>Distinct parts that would be created from the valid rows.</summary>
    public int CreateCount { get; set; }

    /// <summary>Distinct existing parts that would be updated by the valid rows.</summary>
    public int UpdateCount { get; set; }

    /// <summary>Brands that don't exist yet and would be created on commit.</summary>
    public List<string> NewBrands { get; set; } = [];

    /// <summary>Category paths that don't exist yet and would be created on commit.</summary>
    public List<string> NewCategories { get; set; } = [];

    /// <summary>Units that don't exist yet and would be created on commit.</summary>
    public List<string> NewUnits { get; set; } = [];

    public List<ProductImportRowResult> Rows { get; set; } = [];
}

/// <summary>Payload for the commit step: the rows the user confirmed for import.</summary>
public class ProductImportCommitRequest
{
    public List<ProductImportRow> Rows { get; set; } = [];

    /// <summary>Must match the mode the rows were validated under. Defaults to create-only.</summary>
    public ProductImportMode Mode { get; set; } = ProductImportMode.CreateOnly;

    /// <summary>
    /// Opt in to creating brands, categories and units that the workbook names but the catalogue
    /// does not have. Off by default: a spreadsheet typo must not become permanent master data.
    /// Set this only after reviewing NewBrands/NewCategories/NewUnits from the validate step.
    /// </summary>
    public bool AllowNewReferenceData { get; set; } = false;
}

/// <summary>Result of the commit step.</summary>
public class ProductImportCommitResult
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int FailedCount { get; set; }
    public int CreatedBrandsCount { get; set; }
    public int CreatedCategoriesCount { get; set; }
    public int CreatedUnitsCount { get; set; }
    public int CreatedVariantsCount { get; set; }
    public int UpdatedVariantsCount { get; set; }

    /// <summary>Rows that failed during commit (re-validation or persistence errors).</summary>
    public List<ProductImportRowResult> Failures { get; set; } = [];
}
