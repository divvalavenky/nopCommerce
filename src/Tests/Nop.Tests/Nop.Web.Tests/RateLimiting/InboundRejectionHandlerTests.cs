using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nop.Core.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Web.Framework.RateLimiting;
using NUnit.Framework;
using System.Threading.RateLimiting;

namespace Nop.Tests.Nop.Web.Tests.RateLimiting;

[TestFixture]
public class InboundRateLimitPolicyTests
{
    [Test]
    public void FromConfig_returns_null_when_the_limiter_is_off()
    {
        InboundRateLimitPolicy.FromConfig(new CommonConfig { PermitLimit = 0 }).Should().BeNull();
        InboundRateLimitPolicy.FromConfig(new CommonConfig { PermitLimit = -3 }).Should().BeNull();
    }

    [TestCase(429, 429)]
    [TestCase(503, 503)]
    [TestCase(401, 401)]
    [TestCase(400, 429)]
    [TestCase(404, 429)]
    [TestCase(399, 429)]
    [TestCase(600, 429)]
    [TestCase(0, 429)]
    public void FromConfig_coerces_rejection_status(int configured, int expected)
    {
        var policy = InboundRateLimitPolicy.FromConfig(new CommonConfig
        {
            PermitLimit = 10,
            RejectionStatusCode = configured
        });

        policy.Should().NotBeNull();
        policy.RejectionStatusCode.Should().Be(expected);
        policy.Window.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Test]
    public void FromConfig_clamps_a_negative_queue_to_zero()
    {
        var policy = InboundRateLimitPolicy.FromConfig(new CommonConfig
        {
            PermitLimit = 4,
            QueueCount = -8
        });

        policy.QueueLimit.Should().Be(0);
        policy.PermitLimit.Should().Be(4);
    }

    [Test]
    public void Global_limiter_partitions_by_host_and_prefers_an_authenticated_name()
    {
        var policy = InboundRateLimitPolicy.FromConfig(new CommonConfig
        {
            PermitLimit = 1,
            QueueCount = 0
        });

        using var limiter = policy.CreateGlobalLimiter();

        using var first = limiter.AttemptAcquire(Http("shop.example.com"));
        using var second = limiter.AttemptAcquire(Http("shop.example.com"));
        using var otherHost = limiter.AttemptAcquire(Http("other.example.com"));
        using var named = limiter.AttemptAcquire(Http("shop.example.com", "alice"));

        first.IsAcquired.Should().BeTrue();
        second.IsAcquired.Should().BeFalse();
        otherHost.IsAcquired.Should().BeTrue();
        named.IsAcquired.Should().BeTrue();
    }

    private static DefaultHttpContext Http(string host, string userName = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        if (!string.IsNullOrEmpty(userName))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, userName) },
                authenticationType: "test"));
        }

        return httpContext;
    }
}

[TestFixture]
public class InboundRejectionHandlerTests
{
    [TestCase(200, "1")]
    [TestCase(1000, "1")]
    [TestCase(1001, "2")]
    [TestCase(60000, "60")]
    public async Task Rejection_sets_retry_after_in_whole_seconds(int retryAfterMilliseconds, string expected)
    {
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>())).Returns(Task.CompletedTask);
        var handler = Handler(log.Object, out _);
        var context = Rejection(TimeSpan.FromMilliseconds(retryAfterMilliseconds));

        await handler.HandleAsync(context, CancellationToken.None);

        context.HttpContext.Response.Headers.RetryAfter.ToString().Should().Be(expected);
    }

    [Test]
    public async Task Rejection_without_retry_metadata_does_not_set_the_header()
    {
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>())).Returns(Task.CompletedTask);
        var handler = Handler(log.Object, out _);
        var context = Rejection(null);

        await handler.HandleAsync(context, CancellationToken.None);

        context.HttpContext.Response.Headers.RetryAfter.ToString().Should().BeEmpty();
    }

    [Test]
    public async Task Warning_uses_the_stable_prefix_and_drops_the_query_string()
    {
        RateLimitEvent captured = null;
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()))
            .Callback<RateLimitEvent>(rateLimitEvent => captured = rateLimitEvent)
            .Returns(Task.CompletedTask);
        var handler = Handler(log.Object, out _, permitLimit: 100);
        var context = Rejection(TimeSpan.FromSeconds(60), "shop.example.com", "/search", "?q=secret");

        await handler.HandleAsync(context, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.ShortMessage.Should().Be(
            "Rate limit exceeded: inbound GET /search rejected with 429 for partition \"shop.example.com\"; retry after 60s");
        captured.FullMessage.Should().Be(string.Join('\n',
            "Direction: inbound",
            "Partition: shop.example.com",
            "Request: GET /search",
            "Status: 429",
            "Limit: 100 requests per 00:01:00",
            "Retry-After: 60s",
            "Note: logged at most once per partition per window; further rejections in this window are not logged"));
        captured.ShortMessage.Should().NotContain("secret");
        captured.FullMessage.Should().NotContain("secret");
        captured.ShortMessage.Should().StartWith(RateLimitEvent.ShortMessagePrefix);
    }

    [Test]
    public async Task One_partition_is_logged_once_per_aligned_window()
    {
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>())).Returns(Task.CompletedTask);
        var handler = Handler(log.Object, out var time);
        time.UtcNow = new DateTimeOffset(2026, 6, 1, 12, 0, 30, TimeSpan.Zero);

        await handler.HandleAsync(Rejection(TimeSpan.FromSeconds(60), "shop.example.com"), CancellationToken.None);
        await handler.HandleAsync(Rejection(TimeSpan.FromSeconds(60), "shop.example.com"), CancellationToken.None);
        time.UtcNow = new DateTimeOffset(2026, 6, 1, 12, 0, 50, TimeSpan.Zero);
        await handler.HandleAsync(Rejection(TimeSpan.FromSeconds(60), "shop.example.com"), CancellationToken.None);
        await handler.HandleAsync(Rejection(TimeSpan.FromSeconds(60), "other.example.com"), CancellationToken.None);

        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Exactly(2));

        time.UtcNow = new DateTimeOffset(2026, 6, 1, 12, 1, 5, TimeSpan.Zero);
        await handler.HandleAsync(Rejection(TimeSpan.FromSeconds(60), "shop.example.com"), CancellationToken.None);

        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Exactly(3));
    }

    [Test]
    public async Task The_101st_distinct_partition_in_a_window_is_not_logged()
    {
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>())).Returns(Task.CompletedTask);
        var handler = Handler(log.Object, out var time);
        time.UtcNow = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 100; index++)
            await handler.HandleAsync(Rejection(null, "host-" + index.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);

        await handler.HandleAsync(Rejection(null, "host-100"), CancellationToken.None);
        await handler.HandleAsync(Rejection(null, "host-0"), CancellationToken.None);

        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Exactly(100));

        time.UtcNow = time.UtcNow.AddMinutes(1);
        await handler.HandleAsync(Rejection(null, "host-100"), CancellationToken.None);

        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Exactly(101));
    }

    [Test]
    public async Task A_logging_failure_does_not_escape_and_the_retry_after_header_is_still_set()
    {
        var previous = SwapDatabaseInstalled(true);
        try
        {
            var factory = new Mock<IServiceScopeFactory>();
            factory.Setup(scopeFactory => scopeFactory.CreateScope()).Throws(new InvalidOperationException("log down"));
            var handler = new InboundRejectionHandler(
                InboundRateLimitPolicy.FromConfig(new CommonConfig { PermitLimit = 10 }),
                new RateLimitEventLog(factory.Object),
                new ManualTimeProvider { UtcNow = DateTimeOffset.UnixEpoch });
            var context = Rejection(TimeSpan.FromMilliseconds(10));

            await handler.HandleAsync(context, CancellationToken.None);

            context.HttpContext.Response.Headers.RetryAfter.ToString().Should().Be("1");
        }
        finally
        {
            SwapDatabaseInstalled(previous);
        }
    }

    [Test]
    public async Task WriteAsync_inserts_a_warning_with_no_customer()
    {
        var previous = SwapDatabaseInstalled(true);
        try
        {
            LogLevel level = 0;
            string shortMessage = null;
            Customer customer = new();
            var logger = new Mock<ILogger>();
            logger.Setup(log => log.InsertLogAsync(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Customer>()))
                .Callback<LogLevel, string, string, Customer>((logLevel, message, _, associatedCustomer) =>
                {
                    level = logLevel;
                    shortMessage = message;
                    customer = associatedCustomer;
                })
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton(logger.Object);
            using var provider = services.BuildServiceProvider();
            var scope = new Mock<IServiceScope>();
            scope.SetupGet(serviceScope => serviceScope.ServiceProvider).Returns(provider);
            var factory = new Mock<IServiceScopeFactory>();
            factory.Setup(scopeFactory => scopeFactory.CreateScope()).Returns(scope.Object);

            var rejection = new RateLimitRejection(
                "shop.example.com", "GET", "/search", 429, TimeSpan.FromSeconds(60), 100, TimeSpan.FromMinutes(1));
            await new RateLimitEventLog(factory.Object).WriteAsync(new RateLimitEvent.InboundRejected(rejection));

            level.Should().Be(LogLevel.Warning);
            customer.Should().BeNull();
            shortMessage.Should().Be(
                "Rate limit exceeded: inbound GET /search rejected with 429 for partition \"shop.example.com\"; retry after 60s");
        }
        finally
        {
            SwapDatabaseInstalled(previous);
        }
    }

    [Test]
    public async Task WriteAsync_does_nothing_when_the_database_is_not_installed()
    {
        var previous = SwapDatabaseInstalled(false);
        try
        {
            var factory = new Mock<IServiceScopeFactory>();
            factory.Setup(scopeFactory => scopeFactory.CreateScope()).Throws(new InvalidOperationException("should not open a scope"));
            var rejection = new RateLimitRejection("shop.example.com", "GET", "/search", 429, null, 10, TimeSpan.FromMinutes(1));

            await new RateLimitEventLog(factory.Object).WriteAsync(new RateLimitEvent.InboundRejected(rejection));

            factory.Verify(scopeFactory => scopeFactory.CreateScope(), Times.Never);
        }
        finally
        {
            SwapDatabaseInstalled(previous);
        }
    }

    private static InboundRejectionHandler Handler(RateLimitEventLog eventLog, out ManualTimeProvider time, int permitLimit = 10)
    {
        time = new ManualTimeProvider { UtcNow = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero) };
        var policy = InboundRateLimitPolicy.FromConfig(new CommonConfig { PermitLimit = permitLimit });
        return new InboundRejectionHandler(policy, eventLog, time);
    }

    private static OnRejectedContext Rejection(TimeSpan? retryAfter, string host = "shop.example.com", string path = "/search", string query = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = path;
        if (!string.IsNullOrEmpty(query))
            httpContext.Request.QueryString = new QueryString(query);

        return new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = new RetryLease(retryAfter)
        };
    }

    private static bool? SwapDatabaseInstalled(bool? value)
    {
        var field = typeof(DataSettingsManager).GetField("_databaseIsInstalled", BindingFlags.Static | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        var previous = (bool?)field.GetValue(null);
        field.SetValue(null, value);
        return previous;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class RetryLease : RateLimitLease
    {
        private readonly TimeSpan? _retryAfter;

        public RetryLease(TimeSpan? retryAfter)
        {
            _retryAfter = retryAfter;
        }

        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames =>
            _retryAfter is null ? Array.Empty<string>() : new[] { MetadataName.RetryAfter.Name };

        public override bool TryGetMetadata(string metadataName, out object metadata)
        {
            if (_retryAfter is TimeSpan retryAfter && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}

[TestFixture]
public class AddNopRateLimitingTests
{
    [Test]
    public void Enabled_limiter_publishes_the_coerced_status()
    {
        var services = Services();
        services.AddNopRateLimiting(new CommonConfig
        {
            PermitLimit = 10,
            RejectionStatusCode = 400
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        provider.GetRequiredService<InboundRejectionHandler>().Should().NotBeNull();
    }

    [Test]
    public void Configured_503_is_left_in_place()
    {
        var services = Services();
        services.AddNopRateLimiting(new CommonConfig
        {
            PermitLimit = 5,
            RejectionStatusCode = 503
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value.RejectionStatusCode.Should().Be(503);
    }

    [Test]
    public void Disabled_limiter_does_not_register_rate_limiter_options()
    {
        var services = Services();
        services.AddNopRateLimiting(new CommonConfig { PermitLimit = 0 });

        using var provider = services.BuildServiceProvider();
        provider.GetService<InboundRejectionHandler>().Should().BeNull();
        provider.GetService<IOptions<RateLimiterOptions>>()?.Value.GlobalLimiter.Should().BeNull();
        provider.GetRequiredService<RateLimitEventLog>().Should().NotBeNull();
    }

    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IServiceScopeFactory>());
        return services;
    }
}
