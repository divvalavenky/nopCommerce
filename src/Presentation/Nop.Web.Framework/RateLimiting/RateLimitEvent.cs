using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Services.Logging;

namespace Nop.Web.Framework.RateLimiting;

public abstract record RateLimitEvent
{
    /// <summary>
    /// Stable prefix. DefaultLogger drops a row when IgnoreLogWordlist is a substring of the short or full message.
    /// </summary>
    public const string ShortMessagePrefix = "Rate limit exceeded:";

    private RateLimitEvent()
    {
    }

    public string ShortMessage => $"{ShortMessagePrefix} {Summary}";

    protected abstract string Summary { get; }

    public abstract string FullMessage { get; }

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

    /// <summary>
    /// An outbound call gave up after a throttled response.
    /// The target is scheme, host, port, and path. Query and user info are omitted because they can carry secrets.
    /// </summary>
    public sealed record OutboundGaveUp(HttpMethod Method, Uri Target, RetryDecision.GiveUp Outcome) : RateLimitEvent
    {
        protected override string Summary =>
            $"outbound {Method} {DescribeTarget(Target)} returned {(int)Outcome.Last.Status} after {Outcome.Final.Number} {AttemptWord(Outcome.Final.Number)} ({ReasonText(Outcome.Reason)})";

        public override string FullMessage =>
            string.Join('\n',
                "Direction: outbound",
                "Request: " + Method + " " + DescribeTarget(Target),
                "Status: " + ((int)Outcome.Last.Status).ToString(CultureInfo.InvariantCulture),
                "Attempts: " + Outcome.Final.Number.ToString(CultureInfo.InvariantCulture) + " of " + Outcome.MaxAttempts.ToString(CultureInfo.InvariantCulture),
                "Reason: " + Outcome.Reason,
                "Last Retry-After: " + RetryAfterLabel(Outcome.Last.RetryAfter));

        private static string AttemptWord(int attempts) => attempts == 1 ? "attempt" : "attempts";

        private static string ReasonText(GiveUpReason reason) => reason switch
        {
            GiveUpReason.AttemptsExhausted => "attempts exhausted",
            GiveUpReason.RetryAfterTooLong => "retry-after too long",
            _ => reason.ToString()
        };

        private static string RetryAfterLabel(TimeSpan? retryAfter)
        {
            if (retryAfter is not TimeSpan value)
                return "none";

            var seconds = value.TotalSeconds;
            var format = value.Ticks % TimeSpan.TicksPerSecond == 0 ? "0" : "0.###";
            return seconds.ToString(format, CultureInfo.InvariantCulture) + "s";
        }

        private static string DescribeTarget(Uri target)
        {
            if (target is null)
                return string.Empty;

            if (!target.IsAbsoluteUri)
            {
                var relative = target.OriginalString;
                var query = relative.IndexOf('?');
                if (query >= 0)
                    relative = relative[..query];
                return relative;
            }

            var port = target.IsDefaultPort
                ? string.Empty
                : ":" + target.Port.ToString(CultureInfo.InvariantCulture);
            return target.Scheme + "://" + target.Host + port + target.AbsolutePath;
        }
    }
}

public partial class RateLimitEventLog
{
    protected readonly IServiceScopeFactory _serviceScopeFactory;

    public RateLimitEventLog(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Writes one warning. A logging failure is swallowed.
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
        }
    }
}
