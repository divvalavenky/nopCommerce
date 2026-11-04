namespace Nop.Plugin.Misc.MarketBasedPricing.Services;

/// <summary>
/// Pure country selection for market pricing (unit-testable)
/// </summary>
public static class MarketCountryResolver
{
    /// <summary>
    /// Resolves which country id should drive market selection
    /// </summary>
    public static int? Resolve(bool preferShippingAddress, int? shippingCountryId, int? billingCountryId)
    {
        if (preferShippingAddress)
        {
            if (shippingCountryId is > 0)
                return shippingCountryId;
            if (billingCountryId is > 0)
                return billingCountryId;
            return null;
        }

        if (billingCountryId is > 0)
            return billingCountryId;
        if (shippingCountryId is > 0)
            return shippingCountryId;
        return null;
    }
}
