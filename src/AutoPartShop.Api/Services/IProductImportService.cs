using AutoPartShop.Application.DTOs.PartDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Bulk product (parts) import from an Excel (.xlsx) workbook.
/// Workflow: download template → validate (dry-run) → commit confirmed rows.
/// Foreign keys (category, brand, unit) are referenced by name and must already exist.
/// </summary>
public interface IProductImportService
{
    /// <summary>Builds a ready-to-fill .xlsx template with column headers and one example row.</summary>
    byte[] GenerateTemplate();

    /// <summary>
    /// Parses and validates every row of an uploaded workbook without writing anything.
    /// Returns a per-row report of validity and errors.
    /// </summary>
    Task<ProductImportValidationResult> ValidateAsync(Stream xlsxStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-validates and persists the supplied rows. Valid rows are created as parts in a single
    /// transaction; invalid rows are skipped and reported in <see cref="ProductImportCommitResult.Failures"/>.
    /// </summary>
    Task<ProductImportCommitResult> CommitAsync(IEnumerable<ProductImportRow> rows, CancellationToken cancellationToken = default);
}
