namespace AutoPartShop.Infrastructure.Repositories;

using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for ApplicationSettings entity
/// </summary>
public class ApplicationSettingsRepository(AutoPartDbContext context) : IApplicationSettingsRepository
{
    private readonly AutoPartDbContext _context = context;

    /// <inheritdoc/>
    public async Task<IEnumerable<ApplicationSettings>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationSettings
            .Where(s => !s.Isdeleted)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApplicationSettings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Id == id && !s.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(ApplicationSettings entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _context.ApplicationSettings.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ApplicationSettings entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.ApplicationSettings.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setting = await GetByIdAsync(id, cancellationToken);
        if (setting != null)
        {
            setting.Delete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationSettings
            .AnyAsync(s => s.Id == id && !s.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var setting = await GetByKeyAsync(key, cancellationToken);
        return setting?.Value;
    }

    /// <inheritdoc/>
    public async Task<ApplicationSettings?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalizedKey = key.Trim().ToUpper().Replace(" ", "_");
        return await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == normalizedKey && !s.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SetValueAsync(
        string key,
        string value,
        string dataType = "STRING",
        string category = "GENERAL",
        string description = "",
        bool isSystemSetting = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var normalizedKey = key.Trim().ToUpper().Replace(" ", "_");
        var existingSetting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == normalizedKey && !s.Isdeleted, cancellationToken);

        if (existingSetting != null)
        {
            // Update existing setting
            existingSetting.Update(value, dataType, category, description);
        }
        else
        {
            // Create new setting
            var newSetting = ApplicationSettings.Create(
                normalizedKey,
                value,
                dataType,
                category,
                description,
                isSystemSetting);
            await _context.ApplicationSettings.AddAsync(newSetting, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationSettings>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<ApplicationSettings>();

        var normalizedCategory = category.Trim().ToUpper();
        return await _context.ApplicationSettings
            .Where(s => s.Category == normalizedCategory && !s.Isdeleted)
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var normalizedKey = key.Trim().ToUpper().Replace(" ", "_");
        return await _context.ApplicationSettings
            .AnyAsync(s => s.Key == normalizedKey && !s.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationSettings
            .Where(s => !s.Isdeleted)
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be empty", nameof(key));

        var normalizedKey = key.Trim().ToUpper().Replace(" ", "_");
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == normalizedKey && !s.Isdeleted, cancellationToken);

        if (setting != null)
        {
            setting.Delete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
