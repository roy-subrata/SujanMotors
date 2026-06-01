using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.PartDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Bulk import of products (parts) from an Excel workbook.
/// Three-step flow: download a template, validate the filled file (dry-run),
/// then commit the confirmed rows. Category, Brand, and Unit are referenced by name
/// and must already exist.
/// </summary>
[Route("api/v1/products/import")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class ProductImportController(
    IProductImportService _importService,
    ILogger<ProductImportController> _logger) : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Download a ready-to-fill .xlsx template with column headers and an example row.</summary>
    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateTemplate();
        return File(bytes, XlsxContentType, "product-import-template.xlsx");
    }

    /// <summary>
    /// Upload a filled workbook and receive a per-row validation report.
    /// Nothing is written to the database.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Validate(IFormFile? file, CancellationToken cancellationToken)
    {
        var fileError = ValidateUpload(file);
        if (fileError is not null)
            return BadRequest(fileError);

        try
        {
            await using var stream = file!.OpenReadStream();
            var result = await _importService.ValidateAsync(stream, cancellationToken);
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
    /// Commit the confirmed rows. Valid rows are created as parts; any rows that fail
    /// re-validation are skipped and reported back.
    /// </summary>
    [HttpPost("commit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Commit([FromBody] ProductImportCommitRequest request, CancellationToken cancellationToken)
    {
        if (request?.Rows is null || request.Rows.Count == 0)
            return BadRequest(ApiError.Validation("No rows were supplied to import.", instance: Request.Path));

        var result = await _importService.CommitAsync(request.Rows, cancellationToken);
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
