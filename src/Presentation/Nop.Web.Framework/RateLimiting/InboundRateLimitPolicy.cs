using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Inbound limiter options parsed from <see cref="CommonConfig"/>. A null parse means the limiter is off.
/// </summary>
public sealed record InboundRateLimitPolicy
{
    public int PermitLimit { get; }

    /// <summary>
    /// Queue length. Zero means rejected requests are not queued.
    /// </summary>
    public int QueueLimit { get; }

    public TimeSpan Window { get; }

    public int RejectionStatusCode { get; }

    private InboundRateLimitPolicy(int permitLimit, int queueLimit, TimeSpan window, int rejectionStatusCode)
    {
        if (permitLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(permitLimit));
        if (queueLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(queueLimit));
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));

        PermitLimit = permitLimit;
        QueueLimit = queueLimit;
        Window = window;
        RejectionStatusCode = rejectionStatusCode;
    }

    /// <summary>
    /// Parses inbound limiter options.
    /// </summary>
    /// <returns>Null when <see cref="CommonConfig.PermitLimit"/> is not positive.</returns>
    public static InboundRateLimitPolicy FromConfig(CommonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.PermitLimit <= 0)
            return null;

        return new InboundRateLimitPolicy(
            config.PermitLimit,
            Math.Max(0, config.QueueCount),
            TimeSpan.FromMinutes(1),
            NormalizeRejectionStatusCode(config.RejectionStatusCode));
    }

    /// <summary>
    /// Allowed rejection statuses are HTTP 401–403 and 405–599. Anything else becomes 429.
    /// </summary>
    public static int NormalizeRejectionStatusCode(int statusCode)
    {
        return statusCode is (>= 401 and <= 403) or (>= 405 and <= 599)
            ? statusCode
            : StatusCodes.Status429TooManyRequests;
    }

    /// <summary>
    /// Partition key shared by the limiter and the rejection log. Uses the Host header because the limiter runs before authentication.
    /// </summary>
    public static string PartitionKeyOf(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.Request.Headers.Host.ToString();
    }

    public PartitionedRateLimiter<HttpContext> CreateGlobalLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                PartitionKeyOf(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = PermitLimit,
                    QueueLimit = QueueLimit,
                    Window = Window
                }));
    }
}
