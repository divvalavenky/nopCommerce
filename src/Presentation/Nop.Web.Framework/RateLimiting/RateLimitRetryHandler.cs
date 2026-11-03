using Nop.Core.Http;

namespace Nop.Web.Framework.RateLimiting;

/// <summary>
/// Retries throttled outbound calls. Transport exceptions propagate unchanged.
/// </summary>
public partial class RateLimitRetryHandler : DelegatingHandler
{
    public const long MaxBufferedBodyBytes = 1024 * 1024;

    protected readonly OutboundRetryPolicy _policy;
    protected readonly RateLimitEventLog _eventLog;

    public RateLimitRetryHandler(OutboundRetryPolicy policy, RateLimitEventLog eventLog)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(eventLog);

        _policy = policy;
        _eventLog = eventLog;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(NopHttpDefaults.SkipRateLimitRetry, out var skip) && skip)
            return await base.SendAsync(request, cancellationToken);

        if (!CanReplay(request.Content))
            return await base.SendAsync(request, cancellationToken);

        if (request.Content is not null and not ByteArrayContent)
            await request.Content.LoadIntoBufferAsync(MaxBufferedBodyBytes, cancellationToken);

        var attempt = RetryAttempt.First;
        while (true)
        {
            var response = await base.SendAsync(request, cancellationToken);
            switch (_policy.Decide(attempt, Throttle.From(response, request.Method, DateTimeOffset.UtcNow), Random.Shared.NextDouble()))
            {
                case RetryDecision.Deliver:
                    return response;
                case RetryDecision.Wait wait:
                    response.Dispose();
                    await Task.Delay(wait.Delay, cancellationToken);
                    attempt = wait.Next;
                    break;
                case RetryDecision.GiveUp giveUp:
                    await _eventLog.WriteAsync(new RateLimitEvent.OutboundGaveUp(request.Method, request.RequestUri, giveUp));
                    return response;
            }
        }
    }

    private static bool CanReplay(HttpContent content)
    {
        if (content is null)
            return true;

        var length = content.Headers.ContentLength;
        if (length is null)
            return content is ByteArrayContent;

        return length.Value >= 0 && length.Value <= MaxBufferedBodyBytes;
    }
}
