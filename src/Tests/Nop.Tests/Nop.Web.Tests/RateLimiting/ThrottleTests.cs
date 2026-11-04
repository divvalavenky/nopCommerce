using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Nop.Web.Framework.RateLimiting;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.RateLimiting;

[TestFixture]
public class ThrottleTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Too_many_requests_is_limited_with_or_without_retry_after()
    {
        From(HttpStatusCode.TooManyRequests, HttpMethod.Post, null)
            .Should().Be(new Throttle.Limited(HttpStatusCode.TooManyRequests, null));

        From(HttpStatusCode.TooManyRequests, HttpMethod.Post, TimeSpan.FromSeconds(12))
            .Should().Be(new Throttle.Limited(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(12)));
    }

    [Test]
    public void Retry_after_http_date_is_a_delta_from_now_and_past_dates_are_zero()
    {
        using var future = Response(HttpStatusCode.TooManyRequests, null);
        future.Headers.RetryAfter = new RetryConditionHeaderValue(Now.AddSeconds(30));
        Throttle.From(future, HttpMethod.Get, Now)
            .Should().Be(new Throttle.Limited(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(30)));

        using var past = Response(HttpStatusCode.TooManyRequests, null);
        past.Headers.RetryAfter = new RetryConditionHeaderValue(Now.AddSeconds(-5));
        Throttle.From(past, HttpMethod.Get, Now)
            .Should().Be(new Throttle.Limited(HttpStatusCode.TooManyRequests, TimeSpan.Zero));
    }

    [TestCase("GET")]
    [TestCase("HEAD")]
    [TestCase("OPTIONS")]
    [TestCase("PUT")]
    [TestCase("DELETE")]
    [TestCase("TRACE")]
    public void Service_unavailable_with_retry_after_limits_idempotent_methods(string method)
    {
        From(HttpStatusCode.ServiceUnavailable, new HttpMethod(method), TimeSpan.FromSeconds(5))
            .Should().Be(new Throttle.Limited(HttpStatusCode.ServiceUnavailable, TimeSpan.FromSeconds(5)));
    }

    [TestCase("POST")]
    [TestCase("PATCH")]
    public void Service_unavailable_with_retry_after_does_not_limit_other_methods(string method)
    {
        From(HttpStatusCode.ServiceUnavailable, new HttpMethod(method), TimeSpan.FromSeconds(5))
            .Should().Be(Throttle.None.Instance);
    }

    [Test]
    public void Service_unavailable_without_retry_after_and_other_statuses_are_not_throttled()
    {
        From(HttpStatusCode.ServiceUnavailable, HttpMethod.Get, null).Should().Be(Throttle.None.Instance);
        From(HttpStatusCode.InternalServerError, HttpMethod.Get, TimeSpan.FromSeconds(5)).Should().Be(Throttle.None.Instance);
    }

    private static Throttle From(HttpStatusCode status, HttpMethod method, TimeSpan? retryAfter)
    {
        using var response = Response(status, retryAfter);
        return Throttle.From(response, method, Now);
    }

    private static HttpResponseMessage Response(HttpStatusCode status, TimeSpan? retryAfter)
    {
        var response = new HttpResponseMessage(status);
        if (retryAfter is TimeSpan delay)
            response.Headers.RetryAfter = new RetryConditionHeaderValue(delay);
        return response;
    }
}
