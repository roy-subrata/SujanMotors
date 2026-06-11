using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


/// <summary>
/// Repository interface for Unit entity
/// </summary>
public interface IUnitRepository : IBaseRepository<Unit>
{
    /// <summary>
    /// Get all active units
    /// </summary>
    Task<IEnumerable<Unit>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get units with pagination
    /// </summary>
    Task<(IEnumerable<Unit> Units, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search units with pagination
    /// </summary>
    Task<(IEnumerable<Unit> Units, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if unit name already exists (excluding a specific unit ID for updates)
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeUnitId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unit by name
    /// </summary>
    Task<Unit?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for UnitConversion entity
/// </summary>
public interface IUnitConversionRepository : IBaseRepository<UnitConversion>
{
    /// <summary>
    /// Get all active conversions
    /// </summary>
    Task<IEnumerable<UnitConversion>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversions with pagination
    /// </summary>
    Task<(IEnumerable<UnitConversion> Conversions, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversions from a specific unit
    /// </summary>
    Task<IEnumerable<UnitConversion>> GetConversionsFromUnitAsync(Guid fromUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversions to a specific unit
    /// </summary>
    Task<IEnumerable<UnitConversion>> GetConversionsToUnitAsync(Guid toUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversion between two specific units
    /// </summary>
    Task<UnitConversion?> GetConversionAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if conversion already exists between two units
    /// </summary>
    Task<bool> ConversionExistsAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conversions for a unit (both from and to)
    /// </summary>
    Task<IEnumerable<UnitConversion>> GetAllConversionsForUnitAsync(Guid unitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search conversions
    /// </summary>
    Task<(IEnumerable<UnitConversion> Conversions, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
