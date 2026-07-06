using AutoPartShop.Api.Common;
using AutoPartShop.Application.Categories.Dtos;
using AutoPartShop.Application.Catgories;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
[Produces("application/json")]
public class CategoriesController(
    ILogger<CategoriesController> _logger,
    ICategoryRepository _categoryRepository,
    ICategoryReadRepository _categoryReadRepository) : ControllerBase
{
    // ── List ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// List categories with optional filtering and pagination.
    /// Results ordered by displayOrder ASC, name ASC.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        else if (pageSize > 100) pageSize = 100;

        var query = new CategoryQuery
        {
            Search = search ?? string.Empty,
            PageNumber = page,
            PageSize = pageSize,
            IsActive = isActive
        };

        var (items, total) = await _categoryReadRepository.FindAllyAsync(query, cancellationToken);
        return Ok(PagedApiResponse<CategoryResponse>.Create(items, total, page, pageSize));
    }

    // ── Single ────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        return Ok(ApiResponse<CategoryResponse>.Ok(MapToResponse(category)));
    }

    // ── Tree navigation (sub-resources) ──────────────────────────────────────

    /// <summary>Direct children of a category.</summary>
    [HttpGet("{id:guid}/subcategories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubcategories(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _categoryRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        var items = await _categoryRepository.GetSubcategoriesAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(items.Select(c => MapToResponse(c))));
    }

    /// <summary>All ancestor categories from immediate parent to root.</summary>
    [HttpGet("{id:guid}/ancestors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAncestors(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _categoryRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        var items = await _categoryRepository.GetAncestorsAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(items.Select(c => MapToResponse(c))));
    }

    /// <summary>All descendant categories at every level below.</summary>
    [HttpGet("{id:guid}/descendants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDescendants(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _categoryRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        var items = await _categoryRepository.GetAllDescendantsAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(items.Select(c => MapToResponse(c))));
    }

    /// <summary>
    /// Check whether moving a category to a new parent would create a circular reference.
    /// Pass newParentId=null to check moving to root.
    /// </summary>
    [HttpGet("{id:guid}/check-circular-reference")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCircularReference(
        Guid id, [FromQuery] Guid? newParentId, CancellationToken cancellationToken = default)
    {
        if (!await _categoryRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        if (newParentId.HasValue && !await _categoryRepository.ExistsAsync(newParentId.Value, cancellationToken))
            return BadRequest(ApiError.Validation("Proposed parent category not found", instance: Request.Path));

        var wouldCreateCircle = await _categoryRepository.WouldCreateCircularReferenceAsync(id, newParentId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new
        {
            categoryId = id,
            newParentId,
            wouldCreateCircularReference = wouldCreateCircle,
            message = wouldCreateCircle
                ? "Moving this category to the proposed parent would create a circular reference"
                : "Move is allowed"
        }));
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategory request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        if (await _categoryRepository.NameExistsAsync(request.Name.Trim(), null, cancellationToken))
            return Conflict(ApiError.Conflict($"Category name '{request.Name}' already exists", Request.Path));

        if (request.ParentCategoryId.HasValue &&
            !await _categoryRepository.ExistsAsync(request.ParentCategoryId.Value, cancellationToken))
            return BadRequest(ApiError.Validation("Parent category not found", instance: Request.Path));

        var category = Category.Create(
            request.Name.Trim(), request.Description ?? string.Empty,
            request.DisplayOrder, request.ParentCategoryId);

        await _categoryRepository.AddAsync(category, cancellationToken);

        _logger.LogInformation("Created category {Name} id={Id}", category.Name, category.Id);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            ApiResponse<CategoryResponse>.Ok(MapToResponse(category)));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategory request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiError.Validation("Name is required", instance: Request.Path));

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        if (await _categoryRepository.NameExistsAsync(request.Name.Trim(), id, cancellationToken))
            return Conflict(ApiError.Conflict($"Category name '{request.Name}' already exists", Request.Path));

        category.Update(request.Name.Trim(), request.Description ?? string.Empty, request.DisplayOrder, request.IsActive);
        await _categoryRepository.UpdateAsync(category, cancellationToken);

        return Ok(ApiResponse<CategoryResponse>.Ok(MapToResponse(category)));
    }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Activate or deactivate a category. Body: { "isActive": true|false }</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        if (request.IsActive) category.Activate(); else category.Deactivate();
        await _categoryRepository.UpdateAsync(category, cancellationToken);

        return Ok(ApiResponse<CategoryResponse>.Ok(MapToResponse(category)));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(ApiError.NotFound($"Category '{id}' not found", Request.Path));

        try
        {
            await _categoryRepository.DeleteAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiError.BusinessRule(ex.Message, Request.Path));
        }

        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static CategoryResponse MapToResponse(Category category, int depth = 0, int maxDepth = 2)
    {
        var subcategories = (depth < maxDepth && category.SubCategories != null)
            ? category.SubCategories.Select(sc => MapToResponse(sc, depth + 1, maxDepth)).ToList()
            : new List<CategoryResponse>();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            BreadcrumbPath = category.BreadcrumbPath,
            DepthLevel = category.DepthLevel,
            ChildCount = category.ChildCount,
            SubCategories = subcategories
        };
    }
}

public class SetCategoryStatusRequest
{
    public bool IsActive { get; set; }
}
