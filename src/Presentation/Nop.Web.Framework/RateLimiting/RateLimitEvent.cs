using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Services.Logging;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Rate-limit rows that reach System → Log. The prefix is the filter key and the IgnoreLogWordlist switch.
/// </summary>
public abstract record RateLimitEvent
{
    /// <summary>
    /// Stable prefix. System → Log filters on it, and IgnoreLogWordlist turns the events off when it contains this text.
    /// </summary>
    public const string ShortMessagePrefix = "Rate limit exceeded:";

    private RateLimitEvent()
    {
    }

    public string ShortMessage => $"{ShortMessagePrefix} {Summary}";

    protected abstract string Summary { get; }

    public abstract string FullMessage { get; }

    /// <summary>
    /// An inbound request was rejected.
    /// </summary>
    public sealed record InboundRejected(RateLimitRejection Rejection) : RateLimitEvent
    {
        protected override string Summary =>
            $"inbound {Rejection.Method} {Rejection.Path} rejected with {Rejection.StatusCode} for partition \"{Rejection.Partition}\"; retry after {RetryAfterLabel(Rejection.RetryAfter)}";

        public override string FullMessage =>
            string.Join('\n',
                "Direction: inbound",
                "Partition: " + Rejection.Partition,
                "Request: " + Rejection.Method + " " + Rejection.Path,
                "Status: " + Rejection.StatusCode.ToString(CultureInfo.InvariantCulture),
                "Limit: " + Rejection.PermitLimit.ToString(CultureInfo.InvariantCulture) + " requests per " + Rejection.Window,
                "Retry-After: " + RetryAfterLabel(Rejection.RetryAfter),
                "Note: logged at most once per partition per window; further rejections in this window are not logged");

        private static string RetryAfterLabel(TimeSpan? retryAfter)
        {
            return retryAfter is TimeSpan value
                ? InboundRejectionHandler.RetryAfterSeconds(value) + "s"
                : "none";
        }
    }
}

/// <summary>
/// The only writer of rate-limit rows. Opens its own scope because HTTP handlers outlive the request scope.
/// </summary>
public partial class RateLimitEventLog
{
    protected readonly IServiceScopeFactory _serviceScopeFactory;

    public RateLimitEventLog(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Writes one warning. A logging failure is swallowed so a 429 cannot become a 500 and an outbound call cannot fail because the log is down.
    /// </summary>
    public virtual async Task WriteAsync(RateLimitEvent rateLimitEvent)
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
            await logger.InsertLogAsync(LogLevel.Warning, rateLimitEvent.ShortMessage, rateLimitEvent.FullMessage, customer: null);
        }
        catch
        {
            // A logging failure must not turn a 429 into a 500 or fail an outbound call.
        }
    }
}
