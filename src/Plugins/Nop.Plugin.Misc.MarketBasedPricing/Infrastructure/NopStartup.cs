using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.MarketBasedPricing.Services;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.MarketBasedPricing.Infrastructure;

/// <summary>
/// Represents object for the configuring services on application startup
/// </summary>
public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMarketPricingService, MarketPricingService>();

        // Decorate core price calculation: keep concrete implementation, replace interface registration
        services.TryAddScoped<PriceCalculationService>();
        var descriptors = services.Where(d => d.ServiceType == typeof(IPriceCalculationService)).ToList();
        foreach (var descriptor in descriptors)
            services.Remove(descriptor);

        services.AddScoped<IPriceCalculationService>(sp =>
        {
            var inner = sp.GetRequiredService<PriceCalculationService>();
            var marketPricing = sp.GetRequiredService<IMarketPricingService>();
            var productService = sp.GetRequiredService<IProductService>();
            return new MarketBasedPriceCalculationService(inner, marketPricing, productService);
        });
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 3000;
}
