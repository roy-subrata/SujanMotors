namespace AutoPartShop.Domain.Repositories;

using AutoPartShop.Domain.Entities;

/// <summary>
/// Repository interface for Currency entity
/// </summary>
public interface ICurrencyRepository : IBaseRepository<Currency>
{
    /// <summary>
    /// Get currency by its ISO code (e.g., "BDT", "USD")
    /// </summary>
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the base currency for the system
    /// </summary>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active currencies
    /// </summary>
    Task<List<Currency>> GetActiveCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currencies ordered by display order
    /// </summary>
    Task<List<Currency>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a currency code exists
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
}
