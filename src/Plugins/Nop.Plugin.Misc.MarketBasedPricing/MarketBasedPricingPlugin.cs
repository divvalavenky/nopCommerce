using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.MarketBasedPricing;

/// <summary>
/// Represents the market-based pricing plugin
/// </summary>
public class MarketBasedPricingPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public MarketBasedPricingPlugin(ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        ISettingService settingService)
    {
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(MarketBasedPricingDefaults.ConfigurationRouteName);
    }

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new MarketBasedPricingSettings
        {
            Enabled = false,
            PreferShippingAddress = true
        });

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.MarketBasedPricing.Fields.Enabled"] = "Enabled",
            ["Plugins.Misc.MarketBasedPricing.Fields.Enabled.Hint"] = "Check to apply market prices on the storefront and in the cart.",
            ["Plugins.Misc.MarketBasedPricing.Fields.PreferShippingAddress"] = "Prefer shipping address country",
            ["Plugins.Misc.MarketBasedPricing.Fields.PreferShippingAddress.Hint"] = "When checked, use the shipping address country to select a market; otherwise prefer billing, then shipping.",
            ["Plugins.Misc.MarketBasedPricing.Fields.Name"] = "Name",
            ["Plugins.Misc.MarketBasedPricing.Fields.Active"] = "Active",
            ["Plugins.Misc.MarketBasedPricing.Fields.DisplayOrder"] = "Display order",
            ["Plugins.Misc.MarketBasedPricing.Fields.Store"] = "Store",
            ["Plugins.Misc.MarketBasedPricing.Fields.Countries"] = "Countries",
            ["Plugins.Misc.MarketBasedPricing.Fields.ProductId"] = "Product ID",
            ["Plugins.Misc.MarketBasedPricing.Fields.Price"] = "Price",
            ["Plugins.Misc.MarketBasedPricing.Markets"] = "Markets",
            ["Plugins.Misc.MarketBasedPricing.Markets.AddNew"] = "Add market",
            ["Plugins.Misc.MarketBasedPricing.Markets.BackToList"] = "back to configuration",
            ["Plugins.Misc.MarketBasedPricing.Market.Added"] = "The market has been added successfully.",
            ["Plugins.Misc.MarketBasedPricing.Market.Updated"] = "The market has been updated successfully.",
            ["Plugins.Misc.MarketBasedPricing.ProductPrices"] = "Product prices",
            ["Plugins.Misc.MarketBasedPricing.ProductPrices.AddNew"] = "Add / update product price",
            ["Plugins.Misc.MarketBasedPricing.ProductPrice.Invalid"] = "Enter a valid product ID and a non-negative price.",
            ["Plugins.Misc.MarketBasedPricing.ProductPrice.ProductNotFound"] = "Product was not found.",
            ["Plugins.Misc.MarketBasedPricing.ProductName"] = "Product"
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<MarketBasedPricingSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.MarketBasedPricing");
        await base.UninstallAsync();
    }

    #endregion
}
