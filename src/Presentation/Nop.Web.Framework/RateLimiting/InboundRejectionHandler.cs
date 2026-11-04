using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Nop.Web.Framework.RateLimiting;

public sealed record RateLimitRejection(
    string Partition,
    string Method,
    string Path,
    int StatusCode,
    TimeSpan? RetryAfter,
    int PermitLimit,
    TimeSpan Window)
{
    /// <summary>
    /// Reads the rejection the middleware just produced. The path is PathBase + Path and omits the query string.
    /// </summary>
    public static RateLimitRejection From(OnRejectedContext context, InboundRateLimitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);

        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        TimeSpan? retryAfter = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            retryAfter = retry;

        return new RateLimitRejection(
            InboundRateLimitPolicy.PartitionKeyOf(httpContext),
            request.Method,
            (request.PathBase + request.Path).ToString(),
            policy.RejectionStatusCode,
            retryAfter,
            policy.PermitLimit,
            policy.Window);
    }
}

/// <summary>
/// Owns the per-window log dedupe state for inbound rejections.
/// </summary>
public partial class InboundRejectionHandler
{
    protected const int MaxLoggedPartitionsPerWindow = 100;

    protected readonly InboundRateLimitPolicy _policy;
    protected readonly RateLimitEventLog _eventLog;
    protected readonly TimeProvider _timeProvider;
    private readonly RejectionLogGate _gate;

    public InboundRejectionHandler(InboundRateLimitPolicy policy, RateLimitEventLog eventLog, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _policy = policy;
        _eventLog = eventLog;
        _timeProvider = timeProvider;
        _gate = new RejectionLogGate(policy.Window, MaxLoggedPartitionsPerWindow);
    }

    /// <summary>
    /// Sets Retry-After and writes at most one warning for this partition in the current window.
    /// The middleware has already set the status code. This method does not write a body.
    /// </summary>
    public virtual async Task HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var rejection = RateLimitRejection.From(context, _policy);
        if (rejection.RetryAfter is TimeSpan retryAfter)
            context.HttpContext.Response.Headers.RetryAfter = RetryAfterSeconds(retryAfter);

        if (_gate.TryAdmit(rejection.Partition, _timeProvider.GetUtcNow()))
            await _eventLog.WriteAsync(new RateLimitEvent.InboundRejected(rejection));
    }

    /// <summary>
    /// Whole seconds, rounded up, with a floor of 1.
    /// </summary>
    public static string RetryAfterSeconds(TimeSpan retryAfter)
    {
        var seconds = (long)Math.Ceiling(retryAfter.TotalSeconds);
        if (seconds < 1)
            seconds = 1;

        return seconds.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class RejectionLogGate(TimeSpan window, int maxPartitionsPerWindow)
    {
        private readonly Lock _lock = new();
        private readonly HashSet<string> _logged = new(StringComparer.Ordinal);
        private long _windowIndex = -1;

        public bool TryAdmit(string partition, DateTimeOffset now)
        {
            var index = now.UtcTicks / window.Ticks;
            lock (_lock)
            {
                if (index != _windowIndex)
                {
                    _windowIndex = index;
                    _logged.Clear();
                }

                return _logged.Count < maxPartitionsPerWindow && _logged.Add(partition);
            }
        }
    }
}
