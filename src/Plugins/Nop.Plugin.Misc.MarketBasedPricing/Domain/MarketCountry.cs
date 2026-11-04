using Nop.Core;

namespace Nop.Plugin.Misc.MarketBasedPricing.Domain;

/// <summary>
/// Represents a country assigned to a market
/// </summary>
public class MarketCountry : BaseEntity
{
    /// <summary>
    /// Gets or sets the market identifier
    /// </summary>
    public int MarketId { get; set; }

    /// <summary>
    /// Gets or sets the country identifier
    /// </summary>
    public int CountryId { get; set; }
}
