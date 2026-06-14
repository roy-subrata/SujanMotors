namespace AutoPartShop.Api.Common;

/// <summary>
/// Single source of truth for resolving a part/variant's catalog selling price.
///
/// The business uses one selling price per part (and per variant) — batches differ only in
/// cost, not sell price. The denormalized <c>Product.SellingPrice</c> / <c>ProductVariant.SellingPrice</c>
/// columns are the canonical read value; <c>ProductVariantPriceHistory</c> + the scheduled
/// price-sync background service keep them equal to the currently-active scheduled price.
///
/// Every read path (POS search, barcode scan, ecommerce checkout) must use this rule so the
/// same part can never ring up at two different prices.
/// </summary>
public static class CatalogPrice
{
    /// <summary>
    /// Returns the variant's selling price when it has one (&gt; 0), otherwise the base
    /// product's selling price.
    /// </summary>
    public static decimal Resolve(decimal partSellingPrice, decimal? variantSellingPrice)
        => variantSellingPrice is > 0 ? variantSellingPrice.Value : partSellingPrice;
}
