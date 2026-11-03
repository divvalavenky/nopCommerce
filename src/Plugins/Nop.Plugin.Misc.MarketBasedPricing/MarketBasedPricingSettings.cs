using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.MarketBasedPricing;

/// <summary>
/// Represents market-based pricing settings
/// </summary>
public class MarketBasedPricingSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether market-based pricing is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefer the shipping address country over billing
    /// </summary>
    public bool PreferShippingAddress { get; set; } = true;
}
