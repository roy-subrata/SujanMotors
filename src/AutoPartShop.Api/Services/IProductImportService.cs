using AutoPartShop.Application.DTOs.PartDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Bulk product (parts) import from an Excel (.xlsx) workbook.
/// Workflow: download template → validate (dry-run) → commit confirmed rows.
/// Category, Brand and Unit are referenced by name and auto-created when missing.
///
/// SKU is the import key. A blank SKU creates a new part; a filled one updates the
/// matching part, but only when the run is in <see cref="ProductImportMode.CreateAndUpdate"/>.
/// </summary>
public interface IProductImportService
{
    /// <summary>Builds a ready-to-fill .xlsx template with column headers and example rows.</summary>
    byte[] GenerateTemplate();

    /// <summary>
    /// Exports the live catalog in the import's own column layout, SKU filled in — edit the
    /// rows and upload the file back in create-and-update mode to apply the changes.
    /// One row per variant; parts without variants get a single row.
    /// </summary>
    Task<byte[]> GenerateExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses and validates every row of an uploaded workbook without writing anything.
    /// Returns a per-row report of validity, the action each row would take, and the
    /// master data (brands/categories/units) the batch would create.
    /// </summary>
    /// <param name="allowNewReferenceData">
    /// When false (the default), a brand/category/unit name that does not already exist is a row
    /// error instead of a silent insert — a typo must not create master data.
    /// </param>
    Task<ProductImportValidationResult> ValidateAsync(
        Stream xlsxStream, ProductImportMode mode, bool allowNewReferenceData = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-validates and persists the supplied rows in a single transaction — reference data,
    /// parts, variants and price history all land together or not at all. Rows that fail
    /// re-validation are skipped and reported in <see cref="ProductImportCommitResult.Failures"/>.
    /// </summary>
    Task<ProductImportCommitResult> CommitAsync(
        IEnumerable<ProductImportRow> rows, ProductImportMode mode, bool allowNewReferenceData = false,
        CancellationToken cancellationToken = default);
}
