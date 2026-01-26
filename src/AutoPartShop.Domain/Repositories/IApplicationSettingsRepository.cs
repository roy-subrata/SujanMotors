namespace AutoPartShop.Domain.Repositories;

using AutoPartShop.Domain.Entities;

/// <summary>
/// Repository interface for ApplicationSettings entity
/// </summary>
public interface IApplicationSettingsRepository : IBaseRepository<ApplicationSettings>
{
    /// <summary>
    /// Get setting value by key
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get setting entity by key
    /// </summary>
    Task<ApplicationSettings?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set or update a setting value
    /// </summary>
    Task SetValueAsync(
        string key,
        string value,
        string dataType = "STRING",
        string category = "GENERAL",
        string description = "",
        bool isSystemSetting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all settings in a category
    /// </summary>
    Task<List<ApplicationSettings>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a setting key exists
    /// </summary>
    Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all categories
    /// </summary>
    Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete setting by key
    /// </summary>
    Task DeleteByKeyAsync(string key, CancellationToken cancellationToken = default);
}
