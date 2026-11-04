using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.MarketBasedPricing.Models;

/// <summary>
/// Represents plugin configuration model
/// </summary>
public record ConfigurationModel : BaseSearchModel
{
    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.PreferShippingAddress")]
    public bool PreferShippingAddress { get; set; }

    public MarketSearchModel MarketSearchModel { get; set; } = new();
}
