using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoPartShop.Web.Services;

/// <summary>
/// Category service implementation for Blazor Web
/// Communicates with the Categories API
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CategoryService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoryService(HttpClient httpClient, ILogger<CategoryService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON options to handle API response format (camelCase)
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // Allow case-insensitive property matching
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Expect camelCase from API
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore null values
            WriteIndented = false
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all categories");
            var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("api/categories", _jsonOptions, cancellationToken);
            return response ?? new List<CategoryDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching categories from API");
            throw new ServiceException("Failed to fetch categories", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching categories");
            throw new ServiceException("An unexpected error occurred while fetching categories", ex);
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching active categories");
            var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("api/categories/active", _jsonOptions, cancellationToken);
            return response ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active categories");
            throw new ServiceException("Failed to fetch active categories", ex);
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching top-level categories");
            var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("api/categories/top-level", _jsonOptions, cancellationToken);
            return response ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top-level categories");
            throw new ServiceException("Failed to fetch top-level categories", ex);
        }
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching category with ID: {CategoryId}", id);
            var response = await _httpClient.GetFromJsonAsync<CategoryDto>($"api/categories/{id}", _jsonOptions, cancellationToken);
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Category not found: {CategoryId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category: {CategoryId}", id);
            throw new ServiceException("Failed to fetch category", ex);
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching subcategories for parent: {ParentCategoryId}", parentCategoryId);
            var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>($"api/categories/{parentCategoryId}/subcategories", _jsonOptions, cancellationToken);
            return response ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subcategories for parent: {ParentCategoryId}", parentCategoryId);
            throw new ServiceException("Failed to fetch subcategories", ex);
        }
    }

    public async Task<IEnumerable<CategoryDto>> SearchCategoriesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

            _logger.LogInformation("Searching categories with term: {SearchTerm}", searchTerm);
            var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>($"api/categories/search/{Uri.EscapeDataString(searchTerm)}", _jsonOptions, cancellationToken);
            return response ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching categories");
            throw new ServiceException("Failed to search categories", ex);
        }
    }

    public async Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching categories - Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);
            var response = await _httpClient.GetFromJsonAsync<PaginatedResult<CategoryDto>>(
                $"api/categories/paged?pageNumber={pageNumber}&pageSize={pageSize}",
                _jsonOptions,
                cancellationToken
            );
            return response ?? new PaginatedResult<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated categories");
            throw new ServiceException("Failed to fetch paginated categories", ex);
        }
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating category: {CategoryName}", request.Name);

            var response = await _httpClient.PostAsJsonAsync("api/categories", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create category: {StatusCode} - {Content}", response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    throw new ServiceException("A category with this code already exists");

                throw new ServiceException($"Failed to create category: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Successfully created category: {CategoryId}", result?.Id);

            return result ?? throw new ServiceException("Failed to deserialize created category");
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw new ServiceException("Failed to create category", ex);
        }
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating category: {CategoryId}", id);

            var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update category: {StatusCode}", response.StatusCode);
                throw new ServiceException($"Failed to update category: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Successfully updated category: {CategoryId}", id);

            return result ?? throw new ServiceException("Failed to deserialize updated category");
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", id);
            throw new ServiceException("Failed to update category", ex);
        }
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting category: {CategoryId}", id);

            var response = await _httpClient.DeleteAsync($"api/categories/{id}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to delete category: {StatusCode}", response.StatusCode);
                throw new ServiceException($"Failed to delete category: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully deleted category: {CategoryId}", id);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            throw new ServiceException("Failed to delete category", ex);
        }
    }

    public async Task<CategoryDto> ActivateCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Activating category: {CategoryId}", id);

            var response = await _httpClient.PatchAsync($"api/categories/{id}/activate", null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to activate category: {StatusCode}", response.StatusCode);
                throw new ServiceException($"Failed to activate category: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Successfully activated category: {CategoryId}", id);

            return result ?? throw new ServiceException("Failed to deserialize activated category");
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating category: {CategoryId}", id);
            throw new ServiceException("Failed to activate category", ex);
        }
    }

    public async Task<CategoryDto> DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deactivating category: {CategoryId}", id);

            var response = await _httpClient.PatchAsync($"api/categories/{id}/deactivate", null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to deactivate category: {StatusCode}", response.StatusCode);
                throw new ServiceException($"Failed to deactivate category: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Successfully deactivated category: {CategoryId}", id);

            return result ?? throw new ServiceException("Failed to deserialize deactivated category");
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating category: {CategoryId}", id);
            throw new ServiceException("Failed to deactivate category", ex);
        }
    }
}

/// <summary>
/// Custom exception for service errors
/// </summary>
public class ServiceException : Exception
{
    public ServiceException(string message) : base(message) { }
    public ServiceException(string message, Exception innerException) : base(message, innerException) { }
}
