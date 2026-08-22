using System.Text.RegularExpressions;
using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Application.DTOs.PartDtos;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Manage a part's media gallery (images/videos). URLs come either from the
/// file upload endpoint (POST /api/v1/files) or from external sources (e.g. YouTube).
/// Deleting a media row also deletes the underlying uploaded blob when the URL is ours.
/// </summary>
[Route("api/v1/products/{partId:guid}/media")]
[ApiController]
[HasPermission(Permissions.InventoryView)]
[Produces("application/json")]
public class ProductMediaController(
    IProductMediaRepository _mediaRepository,
    IProductRepository _productRepository,
    IStoredFileRepository _fileRepository,
    IFileStorageService _storage,
    ILogger<ProductMediaController> _logger) : ControllerBase
{
    private static readonly Regex StoredFileUrlPattern =
        new(@"/api/v1/files/([0-9a-fA-F\-]{36})/content", RegexOptions.Compiled);

    /// <summary>All media of the part, ordered for display.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPart(Guid partId, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByPartAsync(partId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<ProductMediaDto>>.Ok(media.Select(MapToDto)));
    }

    [HttpPost]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid partId, [FromBody] SaveProductMediaRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return BadRequest(validationError);

        var part = await _productRepository.GetByIdAsync(partId, cancellationToken);
        if (part is null)
            return NotFound(ApiError.NotFound("Part not found.", instance: Request.Path));

        var variantError = await ValidateVariantAsync(partId, request.VariantId, cancellationToken);
        if (variantError is not null)
            return BadRequest(variantError);

        var existing = (await _mediaRepository.GetByPartAsync(partId, cancellationToken)).ToList();
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(x => x.SortOrder) + 1;
        // First media of a part becomes primary automatically so listings always have a thumbnail.
        var isPrimary = request.IsPrimary || existing.Count == 0;

        var media = ProductMedia.Create(
            partId,
            request.Url,
            request.MediaType,
            sortOrder,
            isPrimary,
            request.VariantId,
            request.AltText,
            request.FileName);

        if (isPrimary)
            await _mediaRepository.ClearPrimaryAsync(partId, exceptId: null, cancellationToken);

        await _mediaRepository.AddAsync(media, cancellationToken);

        return CreatedAtAction(nameof(GetByPart), new { partId }, ApiResponse<ProductMediaDto>.Ok(MapToDto(media)));
    }

    [HttpPut("{mediaId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid partId, Guid mediaId, [FromBody] SaveProductMediaRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return BadRequest(validationError);

        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media is null || media.PartId != partId)
            return NotFound(ApiError.NotFound("Media item not found.", instance: Request.Path));

        var variantError = await ValidateVariantAsync(partId, request.VariantId, cancellationToken);
        if (variantError is not null)
            return BadRequest(variantError);

        media.Update(request.Url, request.MediaType, media.SortOrder, request.IsPrimary, request.VariantId, request.AltText, request.FileName);

        if (request.IsPrimary)
            await _mediaRepository.ClearPrimaryAsync(partId, exceptId: mediaId, cancellationToken);

        await _mediaRepository.UpdateAsync(media, cancellationToken);

        return Ok(ApiResponse<ProductMediaDto>.Ok(MapToDto(media)));
    }

    /// <summary>Marks this item as the part's primary media (clears the flag elsewhere).</summary>
    [HttpPatch("{mediaId:guid}/primary")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimary(Guid partId, Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media is null || media.PartId != partId)
            return NotFound(ApiError.NotFound("Media item not found.", instance: Request.Path));

        await _mediaRepository.ClearPrimaryAsync(partId, exceptId: mediaId, cancellationToken);
        media.Update(media.Url, media.MediaType, media.SortOrder, isPrimary: true, media.VariantId, media.AltText, media.FileName);
        await _mediaRepository.UpdateAsync(media, cancellationToken);

        return Ok(ApiResponse<ProductMediaDto>.Ok(MapToDto(media)));
    }

    /// <summary>Reorder the gallery: SortOrder is assigned by position in orderedIds.</summary>
    [HttpPut("order")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reorder(Guid partId, [FromBody] ReorderProductMediaRequest request, CancellationToken cancellationToken)
    {
        if (request?.OrderedIds is null || request.OrderedIds.Count == 0)
            return BadRequest(ApiError.Validation("orderedIds is required.", instance: Request.Path));

        var media = (await _mediaRepository.GetByPartAsync(partId, cancellationToken)).ToList();

        // Resolve every id before mutating anything: a bad id used to return 400 with the
        // earlier items already written, leaving the gallery half-reordered.
        var ordered = new List<ProductMedia>(request.OrderedIds.Count);
        foreach (var id in request.OrderedIds)
        {
            var item = media.FirstOrDefault(x => x.Id == id);
            if (item is null)
                return BadRequest(ApiError.Validation($"Media item {id} does not belong to this part.", instance: Request.Path));

            ordered.Add(item);
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            item.Update(item.Url, item.MediaType, index, item.IsPrimary, item.VariantId, item.AltText, item.FileName);
        }

        await _mediaRepository.UpdateRangeAsync(ordered, cancellationToken);

        var reordered = await _mediaRepository.GetByPartAsync(partId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<ProductMediaDto>>.Ok(reordered.Select(MapToDto)));
    }

    /// <summary>Removes the media row; also deletes the uploaded blob when the URL points at our file store.</summary>
    [HttpDelete("{mediaId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid partId, Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media is null || media.PartId != partId)
            return NotFound(ApiError.NotFound("Media item not found.", instance: Request.Path));

        await _mediaRepository.DeleteAsync(mediaId, cancellationToken);

        // Add auto-promotes the first item, so "a part with media has a primary" is an
        // invariant — keep it true on the way out by promoting the next item in display order.
        if (media.IsPrimary)
        {
            var remaining = (await _mediaRepository.GetByPartAsync(partId, cancellationToken)).ToList();
            var successor = remaining.FirstOrDefault();
            if (successor is not null)
            {
                successor.Update(successor.Url, successor.MediaType, successor.SortOrder,
                    isPrimary: true, successor.VariantId, successor.AltText, successor.FileName);
                await _mediaRepository.UpdateAsync(successor, cancellationToken);
                _logger.LogInformation("Promoted media {MediaId} to primary for part {PartId} after the primary was deleted",
                    successor.Id, partId);
            }
        }

        if (TryParseStoredFileId(media.Url, out var fileId))
        {
            var storedFile = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
            if (storedFile is not null)
            {
                await _storage.DeleteAsync(storedFile.StorageKey, cancellationToken);
                await _fileRepository.DeleteAsync(fileId, cancellationToken);
                _logger.LogInformation("Deleted stored file {FileId} along with product media {MediaId}", fileId, mediaId);
            }
        }

        return NoContent();
    }

    private ApiError? Validate(SaveProductMediaRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
            return ApiError.Validation("Url is required.", instance: Request.Path);

        var mediaType = request.MediaType?.Trim().ToLowerInvariant();
        if (mediaType is not ("image" or "video"))
            return ApiError.Validation("MediaType must be 'image' or 'video'.", instance: Request.Path);

        return null;
    }

    /// <summary>
    /// Media may be scoped to one variant, but only a variant of this part — without the
    /// check a photo could be attached to another product's variant (the FK is NoAction,
    /// so the database does not catch it either).
    /// </summary>
    private async Task<ApiError?> ValidateVariantAsync(Guid partId, Guid? variantId, CancellationToken cancellationToken)
    {
        if (!variantId.HasValue || variantId.Value == Guid.Empty)
            return null;

        var belongs = await _productRepository.VariantBelongsToPartAsync(partId, variantId.Value, cancellationToken);
        return belongs
            ? null
            : ApiError.Validation("The variant does not belong to this part.", instance: Request.Path);
    }

    private static bool TryParseStoredFileId(string url, out Guid fileId)
    {
        var match = StoredFileUrlPattern.Match(url ?? string.Empty);
        return Guid.TryParse(match.Success ? match.Groups[1].Value : null, out fileId);
    }

    private static ProductMediaDto MapToDto(ProductMedia media) => new()
    {
        Id = media.Id,
        Url = media.Url,
        MediaType = media.MediaType,
        AltText = media.AltText,
        FileName = media.FileName,
        SortOrder = media.SortOrder,
        IsPrimary = media.IsPrimary,
        VariantId = media.VariantId
    };
}
