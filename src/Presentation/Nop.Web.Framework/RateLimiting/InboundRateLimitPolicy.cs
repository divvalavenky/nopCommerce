using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Inbound limiter options parsed from <see cref="CommonConfig"/>. A null parse means the limiter is off.
/// </summary>
public sealed record InboundRateLimitPolicy
{
    /// <summary>
    /// Gets the permit count for one partition in <see cref="Window"/>.
    /// </summary>
    public int PermitLimit { get; }

    /// <summary>
    /// Gets the queue length. Zero means rejected requests are not queued.
    /// </summary>
    public int QueueLimit { get; }

    /// <summary>
    /// Gets the fixed window. This stays one minute, matching the limiter nop already shipped.
    /// </summary>
    public TimeSpan Window { get; }

    /// <summary>
    /// Gets the HTTP status written for a rejected request.
    /// </summary>
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

        // 400 and 404 are claimed by nop status pages, so a limiter that emits them never reaches the client.
        // Values outside 400-599 are not rejection statuses. Configured 503 is left alone.
        var status = config.RejectionStatusCode is >= 400 and <= 599 and not 400 and not 404
            ? config.RejectionStatusCode
            : StatusCodes.Status429TooManyRequests;

        return new InboundRateLimitPolicy(
            config.PermitLimit,
            Math.Max(0, config.QueueCount),
            TimeSpan.FromMinutes(1),
            status);
    }

    /// <summary>
    /// Partition key shared by the limiter and the rejection log.
    /// Authentication has not run yet, so this is the Host header unless a name is already present.
    /// </summary>
    public static string PartitionKeyOf(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString();
    }

    /// <summary>
    /// Builds the global fixed-window limiter described by this policy.
    /// </summary>
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
