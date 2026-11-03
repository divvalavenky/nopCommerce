using System.Net;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// 1-based send count. Attempt 1 is the original send.
/// </summary>
public readonly record struct RetryAttempt
{
    public int Number { get; }

    public RetryAttempt(int number)
    {
        if (number < 1)
            throw new ArgumentOutOfRangeException(nameof(number));

        Number = number;
    }

    public static RetryAttempt First => new(1);

    public RetryAttempt Next() => new(Number + 1);
}

public abstract record Throttle
{
    private Throttle()
    {
    }

    public sealed record None : Throttle
    {
        public static None Instance { get; } = new();
    }

    /// <summary>
    /// 429, or 503 with Retry-After on an idempotent method.
    /// </summary>
    public sealed record Limited(HttpStatusCode Status, TimeSpan? RetryAfter) : Throttle;

    public static Throttle From(HttpResponseMessage response, HttpMethod method, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(method);

        var retryAfter = response.Headers.RetryAfter switch
        {
            { Delta: { } delta } => delta,
            { Date: { } at } => at - now < TimeSpan.Zero ? TimeSpan.Zero : at - now,
            _ => (TimeSpan?)null
        };

        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => new Limited(response.StatusCode, retryAfter),
            HttpStatusCode.ServiceUnavailable when retryAfter is not null && IsIdempotent(method)
                => new Limited(response.StatusCode, retryAfter),
            _ => None.Instance
        };
    }

    private static bool IsIdempotent(HttpMethod method)
    {
        return method == HttpMethod.Get
            || method == HttpMethod.Head
            || method == HttpMethod.Options
            || method == HttpMethod.Put
            || method == HttpMethod.Delete
            || method == HttpMethod.Trace;
    }
}

public enum GiveUpReason
{
    AttemptsExhausted,
    RetryAfterTooLong
}

public abstract record RetryDecision
{
    private RetryDecision()
    {
    }

    /// <summary>
    /// Not throttled. The caller receives the response unchanged and nothing is logged.
    /// </summary>
    public sealed record Deliver : RetryDecision
    {
        public static Deliver Instance { get; } = new();
    }

    /// <summary>
    /// Dispose the response, wait, and send <paramref name="Next"/>.
    /// </summary>
    public sealed record Wait(TimeSpan Delay, RetryAttempt Next) : RetryDecision;

    /// <summary>
    /// Return the throttled response and write one warning.
    /// </summary>
    public sealed record GiveUp(GiveUpReason Reason, RetryAttempt Final, Throttle.Limited Last, int MaxAttempts) : RetryDecision;
}

/// <summary>
/// Outbound retry options parsed from <see cref="CommonConfig"/>. A null parse means retry is off and the handler is not registered.
/// </summary>
public sealed record OutboundRetryPolicy
{
    /// <summary>
    /// Hard cap on configured attempts. Values above this are clamped.
    /// </summary>
    public const int MaxAttemptsLimit = 5;

    /// <summary>
    /// A Retry-After above this is not waited out, even when <see cref="MaxDelay"/> is larger.
    /// </summary>
    public static readonly TimeSpan RetryAfterHonorLimit = TimeSpan.FromSeconds(60);

    public int MaxAttempts { get; }

    public TimeSpan BaseDelay { get; }

    public TimeSpan MaxDelay { get; }

    private OutboundRetryPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        if (maxAttempts < 2)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (baseDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseDelay));
        if (maxDelay < baseDelay)
            throw new ArgumentOutOfRangeException(nameof(maxDelay));

        MaxAttempts = maxAttempts;
        BaseDelay = baseDelay;
        MaxDelay = maxDelay;
    }

    /// <summary>
    /// Parses retry options. Returns null when <see cref="CommonConfig.OutboundRetryMaxAttempts"/> is 1 or less.
    /// </summary>
    public static OutboundRetryPolicy FromConfig(CommonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var attempts = config.OutboundRetryMaxAttempts;
        if (attempts <= 1)
            return null;

        if (attempts > MaxAttemptsLimit)
            attempts = MaxAttemptsLimit;

        var baseMilliseconds = Math.Max(1, config.OutboundRetryBaseDelayMilliseconds);
        var maxMilliseconds = Math.Max(baseMilliseconds, config.OutboundRetryMaxDelayMilliseconds);
        return new OutboundRetryPolicy(
            attempts,
            TimeSpan.FromMilliseconds(baseMilliseconds),
            TimeSpan.FromMilliseconds(maxMilliseconds));
    }

    public RetryDecision Decide(RetryAttempt attempt, Throttle throttle, double jitterSample)
    {
        if (throttle is not Throttle.Limited limited)
            return RetryDecision.Deliver.Instance;

        if (attempt.Number >= MaxAttempts)
            return new RetryDecision.GiveUp(GiveUpReason.AttemptsExhausted, attempt, limited, MaxAttempts);

        if (limited.RetryAfter is TimeSpan retryAfter)
        {
            var ceiling = MaxDelay < RetryAfterHonorLimit ? MaxDelay : RetryAfterHonorLimit;
            return retryAfter > ceiling
                ? new RetryDecision.GiveUp(GiveUpReason.RetryAfterTooLong, attempt, limited, MaxAttempts)
                : new RetryDecision.Wait(retryAfter, attempt.Next());
        }

        return new RetryDecision.Wait(BackoffDelay(attempt, Math.Clamp(jitterSample, 0d, 1d)), attempt.Next());
    }

    private TimeSpan BackoffDelay(RetryAttempt attempt, double jitterSample)
    {
        var step = BaseDelay;
        for (var extra = 1; extra < attempt.Number; extra++)
        {
            if (step >= MaxDelay)
                break;

            var doubledTicks = step.Ticks * 2;
            step = doubledTicks >= MaxDelay.Ticks ? MaxDelay : TimeSpan.FromTicks(doubledTicks);
        }

        var halfTicks = step.Ticks / 2;
        var jitterTicks = (long)(halfTicks * jitterSample);
        return TimeSpan.FromTicks(halfTicks + jitterTicks);
    }
}
