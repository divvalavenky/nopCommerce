using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Registers inbound rate limiting. The limiter is added only when <see cref="CommonConfig.PermitLimit"/> is positive.
/// Config is read once per process.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the rate-limit log writer and, when <see cref="CommonConfig.PermitLimit"/> is positive, the inbound limiter.
    /// </summary>
    public static void AddNopRateLimiting(this IServiceCollection services, CommonConfig config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddSingleton<RateLimitEventLog>();

        if (InboundRateLimitPolicy.FromConfig(config) is not { } inbound)
            return;

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
}
