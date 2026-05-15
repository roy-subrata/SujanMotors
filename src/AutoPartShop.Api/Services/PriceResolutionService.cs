using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

public class PriceResolutionService : IPriceResolutionService
{
    private readonly IProductVariantPriceHistoryRepository _priceHistoryRepository;

    public PriceResolutionService(IProductVariantPriceHistoryRepository priceHistoryRepository)
    {
        _priceHistoryRepository = priceHistoryRepository;
    }

    public async Task<PriceResolutionResult?> ResolveAsync(
        Guid partId,
        Guid? productVariantId,
        CancellationToken cancellationToken = default)
    {
        var history = await _priceHistoryRepository.ResolveActivePriceAsync(
            partId, productVariantId, cancellationToken);

        if (history == null) return null;

        var source = history.ProductVariantId.HasValue ? "VARIANT_HISTORY" : "PRODUCT_HISTORY";
        return new PriceResolutionResult(history.SellingPrice, history.Currency, source, history.Id);
    }
}
