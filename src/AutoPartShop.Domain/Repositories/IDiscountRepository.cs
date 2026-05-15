using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IDiscountRepository : IBaseRepository<Discount>
{
    Task<Discount?> GetByPromoCodeAsync(string promoCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns active variant-level discount for a specific variant.</summary>
    Task<Discount?> GetVariantDiscountAsync(Guid partId, Guid productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Returns active product-level discount (VariantId = null) for a part.</summary>
    Task<Discount?> GetProductDiscountAsync(Guid partId, CancellationToken cancellationToken = default);

    /// <summary>Returns all discounts assigned to a part (product + variant level).</summary>
    Task<IEnumerable<Discount>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
}
