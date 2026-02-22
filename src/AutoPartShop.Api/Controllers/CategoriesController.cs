
using AutoPartShop.Application.Categories.Dtos;
using AutoPartShop.Application.Catgories;
using AutoPartShop.Application.Common;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// API Controller for managing product categories
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController(
    ILogger<CategoriesController> _logger,
    ICategoryRepository _categoryRepository,
    ICategoryReadRepository _categoryReadRepository,
    ICodeGenerateService _codeGenerateService) : ControllerBase
{


    /// <summary>
    /// Get all categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAllCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all categories");
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var response = categories.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Successfully fetched {Count} categories", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all categories");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching categories" });
        }
    }

    /// <summary>
    /// Get all active categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active categories</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetActiveCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching active categories");
            var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);
            var response = categories.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Successfully fetched {Count} active categories", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active categories");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching active categories" });
        }
    }

    /// <summary>
    /// Get top-level categories (without parents)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top-level categories</returns>
    [HttpGet("top-level")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetTopLevelCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching top-level categories");
            var categories = await _categoryRepository.GetTopLevelCategoriesAsync(cancellationToken);
            var response = categories.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Successfully fetched {Count} top-level categories", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top-level categories");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching top-level categories" });
        }
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponse>> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching category with ID: {CategoryId}", id);
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

            if (category == null)
            {
                _logger.LogWarning("Category not found with ID: {CategoryId}", id);
                return NotFound(new { message = "Category not found" });
            }

            var response = MapToResponse(category);
            _logger.LogInformation("Successfully fetched category with ID: {CategoryId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category with ID: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching the category" });
        }
    }

    /// <summary>
    /// Get subcategories of a parent category
    /// </summary>
    /// <param name="parentCategoryId">Parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subcategories</returns>
    [HttpGet("{parentCategoryId:guid}/subcategories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetSubcategories(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching subcategories for parent ID: {ParentCategoryId}", parentCategoryId);

            // Check if parent exists
            var parentExists = await _categoryRepository.ExistsAsync(parentCategoryId, cancellationToken);
            if (!parentExists)
            {
                _logger.LogWarning("Parent category not found with ID: {ParentCategoryId}", parentCategoryId);
                return NotFound(new { message = "Parent category not found" });
            }

            var subcategories = await _categoryRepository.GetSubcategoriesAsync(parentCategoryId, cancellationToken);
            var response = subcategories.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Successfully fetched {Count} subcategories for parent ID: {ParentCategoryId}", response.Count, parentCategoryId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subcategories for parent ID: {ParentCategoryId}", parentCategoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching subcategories" });
        }
    }

    [HttpPost("list")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetCategoriesPaged(CategoryQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching categories with pagination: pageNumber={PageNumber}, pageSize={PageSize}, searchTerm={SearchTerm}", request.PageNumber, request.PageSize, request.Search ?? "none");

            var (response, total) = await _categoryReadRepository.FindAllyAsync(request, cancellationToken);

            return Ok(PagedResult<CategoryResponse>.Create(response, total, request));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated categories with searchTerm={SearchTerm}", request.Search ?? "none");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching paginated categories" });
        }
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="request">Category creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CreateCategory request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            {
                _logger.LogWarning("Invalid category creation request: Name or Code is empty");
                return BadRequest(new { message = "Name and Code are required" });
            }

            // Check if code already exists
            var codeExists = await _categoryRepository.CodeExistsAsync(request.Code, null, cancellationToken);
            if (codeExists)
            {
                _logger.LogWarning("Category code already exists: {Code}", request.Code);
                return Conflict(new { message = $"Category with code '{request.Code}' already exists" });
            }

            _logger.LogInformation("Creating new category: {Name} ({Code})", request.Name, request.Code);

            // Create category
            var category = Category.Create(
                request.Name,
                request.Description,
                request.Code,
                request.DisplayOrder,
                request.ParentCategoryId
            );

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _codeGenerateService.SaveGenerateCodeAsync("CAT", cancellationToken);

            var response = MapToResponse(category);
            _logger.LogInformation("Successfully created category with ID: {CategoryId}", category.Id);

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating category");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the category" });
        }
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Category update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, UpdateCategory request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != request.Id)
            {
                _logger.LogWarning("ID mismatch in update request: URL ID={UrlId}, Request ID={RequestId}", id, request.Id);
                return BadRequest(new { message = "ID mismatch" });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Invalid category update request: Name is empty");
                return BadRequest(new { message = "Name is required" });
            }

            var existingCategory = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (existingCategory == null)
            {
                _logger.LogWarning("Category not found for update: {CategoryId}", id);
                return NotFound(new { message = "Category not found" });
            }

            _logger.LogInformation("Updating category: {CategoryId}", id);

            existingCategory.Update(request.Name, request.Description, request.DisplayOrder, request.IsActive);
            await _categoryRepository.UpdateAsync(existingCategory, cancellationToken);

            var response = MapToResponse(existingCategory);
            _logger.LogInformation("Successfully updated category: {CategoryId}", id);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating category: {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the category" });
        }
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCategory = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (existingCategory == null)
            {
                _logger.LogWarning("Category not found for deletion: {CategoryId}", id);
                return NotFound(new { message = "Category not found" });
            }

            _logger.LogInformation("Deleting category: {CategoryId}", id);
            await _categoryRepository.DeleteAsync(id, cancellationToken);

            _logger.LogInformation("Successfully deleted category: {CategoryId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting the category" });
        }
    }

    /// <summary>
    /// Activate a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category details</returns>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponse>> ActivateCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for activation: {CategoryId}", id);
                return NotFound(new { message = "Category not found" });
            }

            _logger.LogInformation("Activating category: {CategoryId}", id);
            category.Activate();
            await _categoryRepository.UpdateAsync(category, cancellationToken);

            var response = MapToResponse(category);
            _logger.LogInformation("Successfully activated category: {CategoryId}", id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating category: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while activating the category" });
        }
    }

    /// <summary>
    /// Deactivate a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category details</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponse>> DeactivateCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for deactivation: {CategoryId}", id);
                return NotFound(new { message = "Category not found" });
            }

            _logger.LogInformation("Deactivating category: {CategoryId}", id);
            category.Deactivate();
            await _categoryRepository.UpdateAsync(category, cancellationToken);

            var response = MapToResponse(category);
            _logger.LogInformation("Successfully deactivated category: {CategoryId}", id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating category: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deactivating the category" });
        }
    }

    /// <summary>
    /// Get breadcrumb path for a category (e.g., "Engines > Diesel > Small")
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Breadcrumb path string</returns>
    [HttpGet("{categoryId:guid}/breadcrumb")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetCategoryBreadcrumb(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for breadcrumb: {CategoryId}", categoryId);
                return NotFound(new { message = "Category not found" });
            }

            var breadcrumb = await _categoryRepository.GetBreadcrumbPathAsync(categoryId, cancellationToken);
            _logger.LogInformation("Retrieved breadcrumb for category: {CategoryId}", categoryId);

            return Ok(new { categoryId, breadcrumbPath = breadcrumb });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting breadcrumb for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving breadcrumb" });
        }
    }

    /// <summary>
    /// Get all ancestors of a category (path to root)
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ancestor categories</returns>
    [HttpGet("{categoryId:guid}/ancestors")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategoryAncestors(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for ancestors: {CategoryId}", categoryId);
                return NotFound(new { message = "Category not found" });
            }

            var ancestors = await _categoryRepository.GetAncestorsAsync(categoryId, cancellationToken);
            var response = ancestors.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Retrieved {Count} ancestors for category: {CategoryId}", response.Count, categoryId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ancestors for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving ancestors" });
        }
    }

    /// <summary>
    /// Get all descendants of a category at all levels
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all descendant categories</returns>
    [HttpGet("{categoryId:guid}/descendants")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategoryDescendants(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for descendants: {CategoryId}", categoryId);
                return NotFound(new { message = "Category not found" });
            }

            var descendants = await _categoryRepository.GetAllDescendantsAsync(categoryId, cancellationToken);
            var response = descendants.Select(c => MapToResponse(c)).ToList();

            _logger.LogInformation("Retrieved {Count} descendants for category: {CategoryId}", response.Count, categoryId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting descendants for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving descendants" });
        }
    }

    /// <summary>
    /// Get the depth level of a category in the hierarchy
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Depth level (0 = root)</returns>
    [HttpGet("{categoryId:guid}/depth")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetCategoryDepth(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for depth calculation: {CategoryId}", categoryId);
                return NotFound(new { message = "Category not found" });
            }

            var depth = await _categoryRepository.GetDepthAsync(categoryId, cancellationToken);
            _logger.LogInformation("Retrieved depth {Depth} for category: {CategoryId}", depth, categoryId);

            return Ok(new { categoryId, depthLevel = depth, maxDepth = Category.MaxCategoryDepth });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depth for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while calculating depth" });
        }
    }

    /// <summary>
    /// Check if moving a category to a new parent would create a circular reference
    /// </summary>
    /// <param name="categoryId">Category ID to move</param>
    /// <param name="newParentId">Proposed new parent ID (null for root)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the move would create a circular reference</returns>
    [HttpPost("{categoryId:guid}/check-circular-reference")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CheckCircularReference(Guid categoryId, [FromQuery] Guid? newParentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category not found for circular reference check: {CategoryId}", categoryId);
                return NotFound(new { message = "Category not found" });
            }

            if (newParentId.HasValue)
            {
                var parentExists = await _categoryRepository.ExistsAsync(newParentId.Value, cancellationToken);
                if (!parentExists)
                {
                    _logger.LogWarning("Proposed parent category not found: {ParentId}", newParentId);
                    return BadRequest(new { message = "Proposed parent category not found" });
                }
            }

            var wouldCreateCircle = await _categoryRepository.WouldCreateCircularReferenceAsync(categoryId, newParentId, cancellationToken);
            _logger.LogInformation("Circular reference check for category {CategoryId} with parent {ParentId}: {Result}", categoryId, newParentId, wouldCreateCircle);

            return Ok(new
            {
                categoryId,
                newParentId,
                wouldCreateCircularReference = wouldCreateCircle,
                message = wouldCreateCircle ? "Moving this category to the proposed parent would create a circular reference" : "Move is allowed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking circular reference for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while checking circular reference" });
        }
    }

    /// <summary>
    /// Map Category entity to CategoryResponse DTO
    /// </summary>
    private static CategoryResponse MapToResponse(Category category, int depth = 0, int maxDepth = 3)
    {
        // Prevent deep recursion to avoid serialization issues
        var subcategories = (depth < maxDepth && category.SubCategories != null)
            ? category.SubCategories.Select(sc => MapToResponse(sc, depth + 1, maxDepth)).ToList()
            : new List<CategoryResponse>();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Code = category.Code,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            CreatedBy = category.CreatedBy,
            ModifiedBy = category.ModifiedBy,
            BreadcrumbPath = category.BreadcrumbPath,
            DepthLevel = category.DepthLevel,
            ChildCount = category.ChildCount,
            SubCategories = subcategories
        };
    }
}
