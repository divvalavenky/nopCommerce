using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;

namespace Nop.Plugin.Misc.MarketBasedPricing.Services;

/// <summary>
/// Market pricing service
/// </summary>
public interface IMarketPricingService
{
    #region Markets

    Task<IPagedList<Market>> GetAllMarketsAsync(bool? active = null, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<Market> GetMarketByIdAsync(int marketId);

    Task InsertMarketAsync(Market market);

    Task UpdateMarketAsync(Market market);

    Task DeleteMarketAsync(Market market);

    #endregion

    #region Countries

    Task<IList<MarketCountry>> GetMarketCountriesByMarketIdAsync(int marketId);

    Task SaveMarketCountriesAsync(int marketId, IList<int> countryIds);

    Task<Market> FindMarketByCountryIdAsync(int countryId, int storeId);

    #endregion

    #region Prices

    Task<IPagedList<MarketProductPrice>> GetMarketProductPricesAsync(int marketId, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<MarketProductPrice> GetMarketProductPriceByIdAsync(int id);

    Task<MarketProductPrice> GetMarketProductPriceAsync(int marketId, int productId);

    Task InsertMarketProductPriceAsync(MarketProductPrice marketProductPrice);

    Task UpdateMarketProductPriceAsync(MarketProductPrice marketProductPrice);

    Task DeleteMarketProductPriceAsync(MarketProductPrice marketProductPrice);

    /// <summary>
    /// Gets the market base price for a product for the given customer/store, if any
    /// </summary>
    Task<decimal?> GetMarketPriceAsync(Product product, Customer customer, Store store);

    /// <summary>
    /// Resolves the country id used for market selection (testable helper)
    /// </summary>
    Task<int?> ResolveCountryIdAsync(Customer customer);

    #endregion
}
