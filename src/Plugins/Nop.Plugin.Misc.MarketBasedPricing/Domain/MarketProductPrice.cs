using Nop.Core;

namespace Nop.Plugin.Misc.MarketBasedPricing.Domain;

/// <summary>
/// Represents a product price for a market
/// </summary>
public class MarketProductPrice : BaseEntity
{
    /// <summary>
    /// Gets or sets the market identifier
    /// </summary>
    public int MarketId { get; set; }

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the price (primary store currency)
    /// </summary>
    public decimal Price { get; set; }
}
