using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.MarketBasedPricing.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(name: MarketBasedPricingDefaults.ConfigurationRouteName,
            pattern: "Admin/MarketBasedPricing/Configure",
            defaults: new { controller = "MarketBasedPricing", action = "Configure", area = AreaNames.ADMIN });
    }

    public int Priority => 0;
}
