using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IDiscountRepository : IBaseRepository<Discount>
{
    Task<Discount?> GetByPromoCodeAsync(string promoCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Discount>> GetActiveDiscountsAsync(DateTime today, CancellationToken cancellationToken = default);

    /// <summary>Returns active variant-level discounts for a specific variant.</summary>
    Task<IEnumerable<Discount>> GetVariantDiscountsAsync(Guid partId, Guid productVariantId, DateTime today, CancellationToken cancellationToken = default);

    /// <summary>Returns active product-level discounts (VariantId = null) for a part.</summary>
    Task<IEnumerable<Discount>> GetProductDiscountsAsync(Guid partId, DateTime today, CancellationToken cancellationToken = default);

    /// <summary>Returns all discounts assigned to a part (product + variant level).</summary>
    Task<IEnumerable<Discount>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
}
