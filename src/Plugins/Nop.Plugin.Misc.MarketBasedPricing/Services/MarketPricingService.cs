using LinqToDB;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;
using Nop.Services.Customers;

namespace Nop.Plugin.Misc.MarketBasedPricing.Services;

/// <summary>
/// Market pricing service
/// </summary>
public class MarketPricingService : IMarketPricingService
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly IRepository<Market> _marketRepository;
    protected readonly IRepository<MarketCountry> _marketCountryRepository;
    protected readonly IRepository<MarketProductPrice> _marketProductPriceRepository;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly MarketBasedPricingSettings _settings;

    #endregion

    #region Ctor

    public MarketPricingService(ICustomerService customerService,
        IRepository<Market> marketRepository,
        IRepository<MarketCountry> marketCountryRepository,
        IRepository<MarketProductPrice> marketProductPriceRepository,
        IStaticCacheManager staticCacheManager,
        MarketBasedPricingSettings settings)
    {
        _customerService = customerService;
        _marketRepository = marketRepository;
        _marketCountryRepository = marketCountryRepository;
        _marketProductPriceRepository = marketProductPriceRepository;
        _staticCacheManager = staticCacheManager;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task ClearMarketCacheAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(NopMarketBasedPricingDefaults.MarketCachePrefix);
    }

    #endregion

    #region Methods

    public virtual async Task<IPagedList<Market>> GetAllMarketsAsync(bool? active = null, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _marketRepository.GetAllPagedAsync(query =>
        {
            if (active.HasValue)
                query = query.Where(m => m.Active == active.Value);

            if (storeId > 0)
                query = query.Where(m => m.StoreId == 0 || m.StoreId == storeId);

            query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);

            return query;
        }, pageIndex, pageSize);
    }

    public virtual async Task<Market> GetMarketByIdAsync(int marketId)
    {
        return await _marketRepository.GetByIdAsync(marketId);
    }

    public virtual async Task InsertMarketAsync(Market market)
    {
        await _marketRepository.InsertAsync(market);
        await ClearMarketCacheAsync();
    }

    public virtual async Task UpdateMarketAsync(Market market)
    {
        await _marketRepository.UpdateAsync(market);
        await ClearMarketCacheAsync();
    }

    public virtual async Task DeleteMarketAsync(Market market)
    {
        var countries = await GetMarketCountriesByMarketIdAsync(market.Id);
        await _marketCountryRepository.DeleteAsync(countries);

        var prices = await _marketProductPriceRepository.GetAllAsync(query =>
            query.Where(p => p.MarketId == market.Id));
        await _marketProductPriceRepository.DeleteAsync(prices);

        await _marketRepository.DeleteAsync(market);
        await ClearMarketCacheAsync();
    }

    public virtual async Task<IList<MarketCountry>> GetMarketCountriesByMarketIdAsync(int marketId)
    {
        return await _marketCountryRepository.GetAllAsync(query =>
            query.Where(c => c.MarketId == marketId).OrderBy(c => c.CountryId));
    }

    public virtual async Task SaveMarketCountriesAsync(int marketId, IList<int> countryIds)
    {
        countryIds ??= new List<int>();
        var existing = await GetMarketCountriesByMarketIdAsync(marketId);
        var toDelete = existing.Where(c => !countryIds.Contains(c.CountryId)).ToList();
        await _marketCountryRepository.DeleteAsync(toDelete);

        var existingIds = existing.Select(c => c.CountryId).ToHashSet();
        foreach (var countryId in countryIds.Distinct().Where(id => id > 0 && !existingIds.Contains(id)))
        {
            await _marketCountryRepository.InsertAsync(new MarketCountry
            {
                MarketId = marketId,
                CountryId = countryId
            });
        }

        await ClearMarketCacheAsync();
    }

    public virtual async Task<Market> FindMarketByCountryIdAsync(int countryId, int storeId)
    {
        if (countryId <= 0)
            return null;

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            NopMarketBasedPricingDefaults.MarketByCountryCacheKey, countryId, storeId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            var query = from mc in _marketCountryRepository.Table
                join m in _marketRepository.Table on mc.MarketId equals m.Id
                where mc.CountryId == countryId && m.Active
                      && (m.StoreId == 0 || m.StoreId == storeId)
                orderby m.DisplayOrder, m.Id
                select m;

            return await query.FirstOrDefaultAsync();
        });
    }

    public virtual async Task<IPagedList<MarketProductPrice>> GetMarketProductPricesAsync(int marketId, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _marketProductPriceRepository.GetAllPagedAsync(query =>
        {
            query = query.Where(p => p.MarketId == marketId);
            query = query.OrderBy(p => p.ProductId);
            return query;
        }, pageIndex, pageSize);
    }

    public virtual async Task<MarketProductPrice> GetMarketProductPriceByIdAsync(int id)
    {
        return await _marketProductPriceRepository.GetByIdAsync(id);
    }

    public virtual async Task<MarketProductPrice> GetMarketProductPriceAsync(int marketId, int productId)
    {
        if (marketId <= 0 || productId <= 0)
            return null;

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            NopMarketBasedPricingDefaults.MarketProductPriceCacheKey, marketId, productId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
            await _marketProductPriceRepository.Table
                .FirstOrDefaultAsync(p => p.MarketId == marketId && p.ProductId == productId));
    }

    public virtual async Task InsertMarketProductPriceAsync(MarketProductPrice marketProductPrice)
    {
        await _marketProductPriceRepository.InsertAsync(marketProductPrice);
        await ClearMarketCacheAsync();
    }

    public virtual async Task UpdateMarketProductPriceAsync(MarketProductPrice marketProductPrice)
    {
        await _marketProductPriceRepository.UpdateAsync(marketProductPrice);
        await ClearMarketCacheAsync();
    }

    public virtual async Task DeleteMarketProductPriceAsync(MarketProductPrice marketProductPrice)
    {
        await _marketProductPriceRepository.DeleteAsync(marketProductPrice);
        await ClearMarketCacheAsync();
    }

    public virtual async Task<int?> ResolveCountryIdAsync(Customer customer)
    {
        if (customer == null)
            return null;

        var shipping = await _customerService.GetCustomerShippingAddressAsync(customer);
        var billing = await _customerService.GetCustomerBillingAddressAsync(customer);

        return MarketCountryResolver.Resolve(_settings.PreferShippingAddress, shipping?.CountryId, billing?.CountryId);
    }

    public virtual async Task<decimal?> GetMarketPriceAsync(Product product, Customer customer, Store store)
    {
        if (!_settings.Enabled || product == null || customer == null || store == null)
            return null;

        var countryId = await ResolveCountryIdAsync(customer);
        if (!countryId.HasValue)
            return null;

        var market = await FindMarketByCountryIdAsync(countryId.Value, store.Id);
        if (market == null)
            return null;

        var marketPrice = await GetMarketProductPriceAsync(market.Id, product.Id);
        return marketPrice?.Price;
    }

    #endregion
}

/// <summary>
/// Cache keys for market-based pricing
/// </summary>
public static class NopMarketBasedPricingDefaults
{
    public static string MarketCachePrefix => "Nop.plugins.marketbasedpricing.";

    public static CacheKey MarketByCountryCacheKey => new("Nop.plugins.marketbasedpricing.marketbycountry.{0}-{1}");

    public static CacheKey MarketProductPriceCacheKey => new("Nop.plugins.marketbasedpricing.productprice.{0}-{1}");
}
