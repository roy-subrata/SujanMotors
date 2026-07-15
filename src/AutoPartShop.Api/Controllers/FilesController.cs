using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.FileDtos;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Upload and serve binary files (product images/videos, employee photos, documents).
/// Blobs live behind IFileStorageService (local disk today, S3-compatible later);
/// this controller is the only URL surface, so storage can move without breaking links.
/// Images/videos are public (unguessable GUID URLs, usable in &lt;img&gt;/&lt;video&gt; tags
/// and the storefront); documents require authentication.
/// </summary>
[Route("api/v1/files")]
[ApiController]
[Produces("application/json")]
public class FilesController(
    IStoredFileRepository _fileRepository,
    IFileStorageService _storage,
    IConfiguration _configuration,
    ILogger<FilesController> _logger) : ControllerBase
{
    /// <summary>
    /// Upload a file (multipart/form-data). Allowed types are inferred from the extension:
    /// images (5 MB), videos (100 MB), documents (10 MB). Optionally tag the owning record
    /// via ownerType (e.g. PRODUCT, EMPLOYEE) + ownerId so attachments can be listed later.
    /// </summary>
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(UploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRules.MaxRequestBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? ownerType,
        [FromForm] Guid? ownerId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiError.Validation("A file is required.", instance: Request.Path));

        var rule = UploadRules.Resolve(file.FileName);
        if (rule is null)
            return BadRequest(ApiError.Validation(
                $"File type is not allowed. Allowed extensions: {UploadRules.AllowedExtensions}.",
                instance: Request.Path));

        if (file.Length > rule.MaxBytes)
            return BadRequest(ApiError.Validation(
                $"The file exceeds the {rule.MaxBytes / (1024 * 1024)} MB limit for {rule.Kind.ToLowerInvariant()} files.",
                instance: Request.Path));

        await using (var probe = file.OpenReadStream())
        {
            if (!await UploadRules.MatchesSignatureAsync(rule, probe, cancellationToken))
                return BadRequest(ApiError.Validation(
                    "The file content does not match its extension.", instance: Request.Path));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = SanitizeFolder(ownerType);
        var storageKey = $"{folder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        await using (var content = file.OpenReadStream())
        {
            await _storage.SaveAsync(content, storageKey, rule.ContentType, cancellationToken);
        }

        try
        {
            var storedFile = StoredFile.Create(
                storageKey,
                Path.GetFileName(file.FileName),
                rule.ContentType,
                file.Length,
                rule.Kind,
                isPublic: rule.Kind != "DOCUMENT",
                ownerType: ownerType ?? string.Empty,
                ownerId: ownerId);

            await _fileRepository.AddAsync(storedFile, cancellationToken);

            _logger.LogInformation("Uploaded {Kind} file {FileName} ({SizeBytes} bytes) as {StorageKey}",
                rule.Kind, storedFile.OriginalFileName, storedFile.SizeBytes, storageKey);

            return Ok(ApiResponse<StoredFileDto>.Ok(MapToDto(storedFile)));
        }
        catch
        {
            // DB record failed — remove the orphaned blob so storage stays consistent.
            await _storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Stream the file content. Public files (images/videos) need no authentication —
    /// their URLs work directly in img/video tags and the storefront. Documents
    /// require a logged-in user (download via HttpClient as a blob).
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var storedFile = await _fileRepository.GetByIdAsync(id, cancellationToken);
        if (storedFile is null)
            return NotFound(ApiError.NotFound("File not found.", instance: Request.Path));

        if (!storedFile.IsPublic && User.Identity?.IsAuthenticated != true)
            return Unauthorized(ApiError.Unauthorized("Authentication is required for this file.", instance: Request.Path));

        var content = await _storage.OpenReadAsync(storedFile.StorageKey, cancellationToken);
        if (content is null)
        {
            _logger.LogWarning("Blob missing for stored file {FileId} (key {StorageKey})", id, storedFile.StorageKey);
            return NotFound(ApiError.NotFound("File content is no longer available.", instance: Request.Path));
        }

        if (storedFile.IsPublic)
        {
            // Keys are never reused, so public content is immutable and safe to cache hard.
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }

        return storedFile.Kind == "DOCUMENT"
            ? File(content, storedFile.ContentType, storedFile.OriginalFileName)
            : File(content, storedFile.ContentType, enableRangeProcessing: true); // ranged: lets <video> seek
    }

    /// <summary>List files attached to a record (e.g. ownerType=EMPLOYEE, ownerId=...).</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOwner(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerType) || ownerId == Guid.Empty)
            return BadRequest(ApiError.Validation("ownerType and ownerId are required.", instance: Request.Path));

        var files = await _fileRepository.GetByOwnerAsync(ownerType, ownerId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<StoredFileDto>>.Ok(files.Select(MapToDto)));
    }

    /// <summary>Delete a file (record + blob).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var storedFile = await _fileRepository.GetByIdAsync(id, cancellationToken);
        if (storedFile is null)
            return NotFound(ApiError.NotFound("File not found.", instance: Request.Path));

        await _storage.DeleteAsync(storedFile.StorageKey, cancellationToken);
        await _fileRepository.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    private StoredFileDto MapToDto(StoredFile file) => new()
    {
        Id = file.Id,
        Url = BuildUrl(file.Id),
        FileName = file.OriginalFileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        Kind = file.Kind,
        IsPublic = file.IsPublic,
        OwnerType = file.OwnerType,
        OwnerId = file.OwnerId,
        CreatedAt = file.CreatedDate
    };

    /// <summary>
    /// Relative by default so links survive domain changes; set FileStorage:PublicBaseUrl
    /// when absolute URLs are needed (e.g. mobile app consuming stored ProductMedia URLs).
    /// </summary>
    private string BuildUrl(Guid id)
    {
        var baseUrl = _configuration["FileStorage:PublicBaseUrl"]?.TrimEnd('/');
        return $"{baseUrl}/api/v1/files/{id}/content";
    }

    private static string SanitizeFolder(string? ownerType)
    {
        var folder = new string((ownerType ?? string.Empty)
            .Trim().ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return string.IsNullOrEmpty(folder) ? "general" : folder;
    }
}
