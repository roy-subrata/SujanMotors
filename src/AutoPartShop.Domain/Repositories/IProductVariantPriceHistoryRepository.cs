using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IProductVariantPriceHistoryRepository : IBaseRepository<ProductVariantPriceHistory>
{
    /// <summary>Active variant-specific price (PartId + VariantId match).</summary>
    Task<ProductVariantPriceHistory?> GetActiveVariantPriceAsync(Guid partId, Guid productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Active base product price (PartId match, VariantId = null).</summary>
    Task<ProductVariantPriceHistory?> GetActiveProductPriceAsync(Guid partId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves selling price for a part/variant on a given date.
    /// Order: variant-specific → base product.
    /// </summary>
    Task<ProductVariantPriceHistory?> ResolveActivePriceAsync(Guid partId, Guid? productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Price active on a specific date (for order auditing).</summary>
    Task<ProductVariantPriceHistory?> GetPriceOnDateAsync(Guid partId, Guid? productVariantId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>Full price history for a part (all variants included).</summary>
    Task<IEnumerable<ProductVariantPriceHistory>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a new price atomically: closes current active price, inserts new.
    /// </summary>
    Task SetNewPriceAsync(ProductVariantPriceHistory newPrice, CancellationToken cancellationToken = default);
}
