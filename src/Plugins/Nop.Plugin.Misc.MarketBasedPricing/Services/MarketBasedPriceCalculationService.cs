using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.MarketBasedPricing.Services;

/// <summary>
/// Decorates core price calculation to apply market base prices when enabled
/// </summary>
public class MarketBasedPriceCalculationService : IPriceCalculationService
{
    #region Fields

    protected readonly IPriceCalculationService _inner;
    protected readonly IMarketPricingService _marketPricingService;
    protected readonly IProductService _productService;

    #endregion

    #region Ctor

    public MarketBasedPriceCalculationService(IPriceCalculationService inner,
        IMarketPricingService marketPricingService,
        IProductService productService)
    {
        _inner = inner;
        _marketPricingService = marketPricingService;
        _productService = productService;
    }

    #endregion

    #region Methods

    public virtual async Task<(decimal priceWithoutDiscounts, decimal finalPrice, decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(
        Product product, Customer customer, Store store, decimal additionalCharge = 0, bool includeDiscounts = true, int quantity = 1)
    {
        return await GetFinalPriceAsync(product, customer, store, additionalCharge, includeDiscounts, quantity, null, null);
    }

    public virtual async Task<(decimal priceWithoutDiscounts, decimal finalPrice, decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(
        Product product, Customer customer, Store store, decimal additionalCharge, bool includeDiscounts, int quantity,
        DateTime? rentalStartDate, DateTime? rentalEndDate)
    {
        return await GetFinalPriceAsync(product, customer, store, null, additionalCharge, includeDiscounts, quantity, rentalStartDate, rentalEndDate);
    }

    public virtual async Task<(decimal priceWithoutDiscounts, decimal finalPrice, decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(
        Product product, Customer customer, Store store, decimal? overriddenProductPrice, decimal additionalCharge,
        bool includeDiscounts, int quantity, DateTime? rentalStartDate, DateTime? rentalEndDate)
    {
        if (!overriddenProductPrice.HasValue)
        {
            var marketPrice = await _marketPricingService.GetMarketPriceAsync(product, customer, store);
            if (marketPrice.HasValue)
                overriddenProductPrice = marketPrice;
        }

        return await _inner.GetFinalPriceAsync(product, customer, store, overriddenProductPrice, additionalCharge,
            includeDiscounts, quantity, rentalStartDate, rentalEndDate);
    }

    public virtual Task<decimal> GetProductCostAsync(Product product, string attributesXml)
        => _inner.GetProductCostAsync(product, attributesXml);

    public virtual async Task<decimal> GetProductAttributeValuePriceAdjustmentAsync(Product product, ProductAttributeValue value,
        Customer customer, Store store, decimal? productPrice = null, int quantity = 1)
    {
        // Percentage and associated-product adjustments call GetFinalPriceAsync.
        // Doing that on the inner service skips the market base price, so resolve it here.
        if (value != null && value.AttributeValueType == AttributeValueType.AssociatedToProduct)
        {
            var associatedProduct = await _productService.GetProductByIdAsync(value.AssociatedProductId);
            if (associatedProduct == null)
                return decimal.Zero;

            var (_, finalPrice, _, _) = await GetFinalPriceAsync(associatedProduct, customer, store);
            return finalPrice * value.Quantity;
        }

        if (value != null
            && value.AttributeValueType == AttributeValueType.Simple
            && value.PriceAdjustmentUsePercentage
            && !productPrice.HasValue)
        {
            var (_, finalPrice, _, _) = await GetFinalPriceAsync(product, customer, store, quantity: quantity);
            productPrice = finalPrice;
        }

        return await _inner.GetProductAttributeValuePriceAdjustmentAsync(product, value, customer, store, productPrice, quantity);
    }

    public virtual Task<decimal> RoundPriceAsync(decimal value, Currency currency = null)
        => _inner.RoundPriceAsync(value, currency);

    public virtual decimal Round(decimal value, RoundingType roundingType)
        => _inner.Round(value, roundingType);

    #endregion
}
