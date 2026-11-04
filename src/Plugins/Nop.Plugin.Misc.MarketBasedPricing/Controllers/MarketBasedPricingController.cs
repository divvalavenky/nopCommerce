using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;
using Nop.Plugin.Misc.MarketBasedPricing.Models;
using Nop.Plugin.Misc.MarketBasedPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.MarketBasedPricing.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class MarketBasedPricingController : BasePluginController
{
    #region Fields

    protected readonly ICountryService _countryService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IMarketPricingService _marketPricingService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IProductService _productService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreService _storeService;
    protected readonly MarketBasedPricingSettings _settings;

    #endregion

    #region Ctor

    public MarketBasedPricingController(ICountryService countryService,
        ILocalizationService localizationService,
        IMarketPricingService marketPricingService,
        INotificationService notificationService,
        IPermissionService permissionService,
        IProductService productService,
        ISettingService settingService,
        IStoreService storeService,
        MarketBasedPricingSettings settings)
    {
        _countryService = countryService;
        _localizationService = localizationService;
        _marketPricingService = marketPricingService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _productService = productService;
        _settingService = settingService;
        _storeService = storeService;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task PrepareStoreAndCountryLookupsAsync(MarketModel model)
    {
        model.AvailableStores.Clear();
        model.AvailableStores.Add(new SelectListItem
        {
            Text = await _localizationService.GetResourceAsync("Admin.Common.All"),
            Value = "0"
        });
        foreach (var store in await _storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString(), Selected = store.Id == model.StoreId });

        model.AvailableCountries.Clear();
        foreach (var country in await _countryService.GetAllCountriesAsync(showHidden: true))
        {
            model.AvailableCountries.Add(new SelectListItem
            {
                Text = country.Name,
                Value = country.Id.ToString(),
                Selected = model.SelectedCountryIds.Contains(country.Id)
            });
        }
    }

    #endregion

    #region Configure

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure()
    {
        var model = new ConfigurationModel
        {
            Enabled = _settings.Enabled,
            PreferShippingAddress = _settings.PreferShippingAddress
        };
        model.SetGridPageSize();
        model.MarketSearchModel.SetGridPageSize();

        return View("~/Plugins/Misc.MarketBasedPricing/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        _settings.Enabled = model.Enabled;
        _settings.PreferShippingAddress = model.PreferShippingAddress;
        await _settingService.SaveSettingAsync(_settings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion

    #region Markets

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> MarketList(MarketSearchModel searchModel)
    {
        var markets = await _marketPricingService.GetAllMarketsAsync(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);
        var model = await new MarketListModel().PrepareToGridAsync(searchModel, markets, () =>
        {
            return markets.SelectAwait(async market =>
            {
                var storeName = market.StoreId == 0
                    ? await _localizationService.GetResourceAsync("Admin.Common.All")
                    : (await _storeService.GetStoreByIdAsync(market.StoreId))?.Name ?? market.StoreId.ToString();

                return new MarketModel
                {
                    Id = market.Id,
                    Name = market.Name,
                    Active = market.Active,
                    DisplayOrder = market.DisplayOrder,
                    StoreId = market.StoreId,
                    StoreName = storeName
                };
            });
        });

        return Json(model);
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> CreateMarket()
    {
        var model = new MarketModel { Active = true };
        await PrepareStoreAndCountryLookupsAsync(model);
        return View("~/Plugins/Misc.MarketBasedPricing/Views/CreateMarket.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> CreateMarket(MarketModel model)
    {
        if (ModelState.IsValid)
        {
            var market = new Market
            {
                Name = model.Name,
                Active = model.Active,
                DisplayOrder = model.DisplayOrder,
                StoreId = model.StoreId
            };
            await _marketPricingService.InsertMarketAsync(market);
            await _marketPricingService.SaveMarketCountriesAsync(market.Id, model.SelectedCountryIds);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.MarketBasedPricing.Market.Added"));

            return RedirectToAction(nameof(EditMarket), new { id = market.Id });
        }

        await PrepareStoreAndCountryLookupsAsync(model);
        return View("~/Plugins/Misc.MarketBasedPricing/Views/CreateMarket.cshtml", model);
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> EditMarket(int id)
    {
        var market = await _marketPricingService.GetMarketByIdAsync(id);
        if (market == null)
            return RedirectToAction(nameof(Configure));

        var countries = await _marketPricingService.GetMarketCountriesByMarketIdAsync(market.Id);
        var model = new MarketModel
        {
            Id = market.Id,
            Name = market.Name,
            Active = market.Active,
            DisplayOrder = market.DisplayOrder,
            StoreId = market.StoreId,
            SelectedCountryIds = countries.Select(c => c.CountryId).ToList()
        };
        model.MarketProductPriceSearchModel.MarketId = market.Id;
        model.MarketProductPriceSearchModel.SetGridPageSize();
        await PrepareStoreAndCountryLookupsAsync(model);

        return View("~/Plugins/Misc.MarketBasedPricing/Views/EditMarket.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> EditMarket(MarketModel model)
    {
        var market = await _marketPricingService.GetMarketByIdAsync(model.Id);
        if (market == null)
            return RedirectToAction(nameof(Configure));

        if (ModelState.IsValid)
        {
            market.Name = model.Name;
            market.Active = model.Active;
            market.DisplayOrder = model.DisplayOrder;
            market.StoreId = model.StoreId;
            await _marketPricingService.UpdateMarketAsync(market);
            await _marketPricingService.SaveMarketCountriesAsync(market.Id, model.SelectedCountryIds);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.MarketBasedPricing.Market.Updated"));

            return RedirectToAction(nameof(EditMarket), new { id = market.Id });
        }

        model.MarketProductPriceSearchModel.MarketId = market.Id;
        model.MarketProductPriceSearchModel.SetGridPageSize();
        await PrepareStoreAndCountryLookupsAsync(model);
        return View("~/Plugins/Misc.MarketBasedPricing/Views/EditMarket.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> DeleteMarket(int id)
    {
        var market = await _marketPricingService.GetMarketByIdAsync(id);
        if (market != null)
            await _marketPricingService.DeleteMarketAsync(market);

        return new NullJsonResult();
    }

    #endregion

    #region Market product prices

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> MarketProductPriceList(MarketProductPriceSearchModel searchModel)
    {
        var prices = await _marketPricingService.GetMarketProductPricesAsync(searchModel.MarketId, searchModel.Page - 1, searchModel.PageSize);
        var model = await new MarketProductPriceListModel().PrepareToGridAsync(searchModel, prices, () =>
        {
            return prices.SelectAwait(async price =>
            {
                var product = await _productService.GetProductByIdAsync(price.ProductId);
                return new MarketProductPriceModel
                {
                    Id = price.Id,
                    MarketId = price.MarketId,
                    ProductId = price.ProductId,
                    ProductName = product?.Name ?? price.ProductId.ToString(),
                    Price = price.Price
                };
            });
        });

        return Json(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> MarketProductPriceAdd(MarketProductPriceModel model)
    {
        if (model.MarketId <= 0 || model.ProductId <= 0 || model.Price < decimal.Zero)
            return ErrorJson(await _localizationService.GetResourceAsync("Plugins.Misc.MarketBasedPricing.ProductPrice.Invalid"));

        var product = await _productService.GetProductByIdAsync(model.ProductId);
        if (product == null)
            return ErrorJson(await _localizationService.GetResourceAsync("Plugins.Misc.MarketBasedPricing.ProductPrice.ProductNotFound"));

        var existing = await _marketPricingService.GetMarketProductPriceAsync(model.MarketId, model.ProductId);
        if (existing != null)
        {
            existing.Price = model.Price;
            await _marketPricingService.UpdateMarketProductPriceAsync(existing);
        }
        else
        {
            await _marketPricingService.InsertMarketProductPriceAsync(new MarketProductPrice
            {
                MarketId = model.MarketId,
                ProductId = model.ProductId,
                Price = model.Price
            });
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> MarketProductPriceUpdate(MarketProductPriceModel model)
    {
        if (model.Price < decimal.Zero)
            return ErrorJson(await _localizationService.GetResourceAsync("Plugins.Misc.MarketBasedPricing.ProductPrice.Invalid"));

        var entity = await _marketPricingService.GetMarketProductPriceByIdAsync(model.Id);
        if (entity == null)
            return new NullJsonResult();

        entity.Price = model.Price;
        await _marketPricingService.UpdateMarketProductPriceAsync(entity);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> MarketProductPriceDelete(int id)
    {
        var entity = await _marketPricingService.GetMarketProductPriceByIdAsync(id);
        if (entity != null)
            await _marketPricingService.DeleteMarketProductPriceAsync(entity);

        return new NullJsonResult();
    }

    #endregion
}
