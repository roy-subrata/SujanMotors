using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.PartDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Bulk import of products (parts) from an Excel workbook.
/// Three-step flow: download a template, validate the filled file (dry-run),
/// then commit the confirmed rows. Category (supports "Parent > Child" hierarchy),
/// Brand, and Unit are auto-created when not found. Variants can be imported by
/// filling the Variant columns on rows sharing the same product Name + Part Number.
///
/// The run's <see cref="ProductImportMode"/> decides whether existing parts may be
/// touched: <c>CreateOnly</c> (the default) rejects any row carrying a SKU, while
/// <c>CreateAndUpdate</c> updates the part a SKU points at and additionally requires
/// the <c>inventory.edit</c> permission.
/// </summary>
// Permissions are per-action rather than class-wide: exporting the catalog only needs
// read access, while anything that writes needs inventory.create (plus inventory.edit
// for update mode). Nested [HasPermission] attributes AND together, so they can't be relaxed.
[Route("api/v1/products/import")]
[ApiController]
[Produces("application/json")]
public class ProductImportController(
    IProductImportService _importService,
    IPermissionCheckService _permissionCheck,
    ILogger<ProductImportController> _logger) : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Download a ready-to-fill .xlsx template with column headers and an example row.</summary>
    [HttpGet("template")]
    [HasPermission(Permissions.InventoryCreate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateTemplate();
        return File(bytes, XlsxContentType, "product-import-template.xlsx");
    }

    /// <summary>
    /// Download the current catalog in the import's column layout, SKU included.
    /// Edit the rows and upload the file back in create-and-update mode to apply changes —
    /// this is the supported way to update parts in bulk.
    /// </summary>
    [HttpGet("export")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadExport(CancellationToken cancellationToken)
    {
        var bytes = await _importService.GenerateExportAsync(cancellationToken);
        return File(bytes, XlsxContentType, $"parts-export-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    /// <summary>
    /// Upload a filled workbook and receive a per-row validation report.
    /// Nothing is written to the database.
    /// </summary>
    /// <param name="file">The filled .xlsx workbook.</param>
    /// <param name="mode">Create-only (default) or create-and-update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("validate")]
    [HasPermission(Permissions.InventoryCreate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Validate(
        IFormFile? file,
        [FromQuery] ProductImportMode mode = ProductImportMode.CreateOnly,
        CancellationToken cancellationToken = default)
    {
        var fileError = ValidateUpload(file);
        if (fileError is not null)
            return BadRequest(fileError);

        try
        {
            await using var stream = file!.OpenReadStream();
            var result = await _importService.ValidateAsync(stream, mode, cancellationToken);
            return Ok(ApiResponse<ProductImportValidationResult>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message, instance: Request.Path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse product import file");
            return BadRequest(ApiError.Validation(
                "The file could not be read. Make sure it is a valid .xlsx workbook based on the template.",
                instance: Request.Path));
        }
    }

    /// <summary>
    /// Commit the confirmed rows in a single transaction. Rows without a SKU are created;
    /// rows with one update that part (create-and-update mode only). Any row that fails
    /// re-validation is skipped and reported back.
    /// </summary>
    [HttpPost("commit")]
    [HasPermission(Permissions.InventoryCreate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Commit([FromBody] ProductImportCommitRequest request, CancellationToken cancellationToken)
    {
        if (request?.Rows is null || request.Rows.Count == 0)
            return BadRequest(ApiError.Validation("No rows were supplied to import.", instance: Request.Path));

        // Creating parts needs inventory.create (the class-level gate); overwriting existing
        // ones is an edit, so update mode needs inventory.edit on top of it.
        if (request.Mode == ProductImportMode.CreateAndUpdate &&
            !await _permissionCheck.UserHasPermissionAsync(User, Permissions.InventoryEdit, cancellationToken))
        {
            _logger.LogWarning("User {User} attempted an update-mode import without {Permission}",
                User.Identity?.Name, Permissions.InventoryEdit);
            return StatusCode(StatusCodes.Status403Forbidden, ApiError.Forbidden(
                "Updating existing parts requires the inventory.edit permission.", instance: Request.Path));
        }

        var result = await _importService.CommitAsync(request.Rows, request.Mode, cancellationToken);
        return Ok(ApiResponse<ProductImportCommitResult>.Ok(result));
    }

    private ApiError? ValidateUpload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return ApiError.Validation("A file is required.", instance: Request.Path);

        if (file.Length > MaxUploadBytes)
            return ApiError.Validation("The file exceeds the 10 MB limit.", instance: Request.Path);

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ApiError.Validation("Only .xlsx files are supported. Use the downloaded template.", instance: Request.Path);

        return null;
    }
}
