using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Registers inbound rate limiting and outbound 429 retry. Each half is registered only when its policy parses as enabled.
/// Config is read once per process.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the rate-limit log writer, the inbound limiter, and the outbound retry handler.
    /// </summary>
    public static void AddNopRateLimiting(this IServiceCollection services, CommonConfig config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddSingleton<RateLimitEventLog>();

        if (InboundRateLimitPolicy.FromConfig(config) is { } inbound)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton(inbound);
            services.AddSingleton<InboundRejectionHandler>();
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = inbound.CreateGlobalLimiter();
                options.RejectionStatusCode = inbound.RejectionStatusCode;
                options.OnRejected = (context, token) => new ValueTask(context.HttpContext.RequestServices
                    .GetRequiredService<InboundRejectionHandler>()
                    .HandleAsync(context, token));
            });
        }

        if (OutboundRetryPolicy.FromConfig(config) is { } outbound)
        {
            services.AddSingleton(outbound);
            services.AddTransient<RateLimitRetryHandler>();
            services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler<RateLimitRetryHandler>());
        }
    }
}
