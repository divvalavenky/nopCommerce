using Nop.Core;

namespace Nop.Plugin.Misc.MarketBasedPricing.Domain;

/// <summary>
/// Represents a geographic pricing market
/// </summary>
public class Market : BaseEntity
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the market is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the display order (lower wins when a country maps to multiple markets)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the store identifier (0 = all stores)
    /// </summary>
    public int StoreId { get; set; }
}
