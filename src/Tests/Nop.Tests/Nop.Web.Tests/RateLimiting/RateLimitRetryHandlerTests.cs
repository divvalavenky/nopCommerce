using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nop.Core.Configuration;
using Nop.Core.Http;
using Nop.Web.Framework.RateLimiting;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.RateLimiting;

[TestFixture]
public class RateLimitRetryHandlerTests
{
    [Test]
    public async Task Retries_a_replayable_body_until_the_server_accepts_it()
    {
        var body = Encoding.UTF8.GetBytes("same-body");
        var log = Logger();
        using var invoker = Invoker(log.Object, new ScriptedHandler(
            Step(HttpStatusCode.TooManyRequests),
            Step(HttpStatusCode.TooManyRequests),
            Step(HttpStatusCode.OK)), out var inner, attempts: 3, baseMilliseconds: 1, maxMilliseconds: 20);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example/v1/items")
        {
            Content = new StreamContent(new OneShotStream(body))
        };
        request.Content.Headers.ContentLength = body.Length;

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Sends.Should().Be(3);
        inner.Bodies.Should().Equal("same-body", "same-body", "same-body");
        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Never);
    }

    [Test]
    public async Task SkipRateLimitRetry_sends_once()
    {
        var log = Logger();
        using var invoker = Invoker(log.Object, new ScriptedHandler(Step(HttpStatusCode.TooManyRequests)), out var inner);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://shop.example/keepalive/index");
        request.Options.Set(NopHttpDefaults.SkipRateLimitRetry, true);

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Sends.Should().Be(1);
        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Never);
    }

    [Test]
    public async Task Unknown_length_and_oversized_bodies_are_not_retried()
    {
        await AssertSingleSend(new StreamContent(new OneShotStream("secret"u8.ToArray())));

        var oversized = new StreamContent(new OneShotStream("x"u8.ToArray()));
        oversized.Headers.ContentLength = RateLimitRetryHandler.MaxBufferedBodyBytes + 1;
        await AssertSingleSend(oversized);
    }

    [Test]
    public async Task Exhausted_retries_return_the_last_429_and_write_one_warning()
    {
        RateLimitEvent captured = null;
        var log = Logger();
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()))
            .Callback<RateLimitEvent>(rateLimitEvent => captured = rateLimitEvent)
            .Returns(Task.CompletedTask);
        using var invoker = Invoker(log.Object, new ScriptedHandler(
            Step(HttpStatusCode.TooManyRequests),
            Step(HttpStatusCode.TooManyRequests),
            Step(HttpStatusCode.TooManyRequests)), out var inner);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://user:secret@api.brevo.com/v3/contacts?api_key=supersecret");

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Sends.Should().Be(3);
        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Once);
        captured.ShortMessage.Should().Be(
            "Rate limit exceeded: outbound POST https://api.brevo.com/v3/contacts returned 429 after 3 attempts (attempts exhausted)");
        captured.FullMessage.Should().Be(string.Join('\n',
            "Direction: outbound",
            "Request: POST https://api.brevo.com/v3/contacts",
            "Status: 429",
            "Attempts: 3 of 3",
            "Reason: AttemptsExhausted",
            "Last Retry-After: none"));
        captured.FullMessage.Should().NotContain("secret");
        captured.FullMessage.Should().NotContain("api_key");
    }

    [Test]
    public async Task A_retry_after_longer_than_the_ceiling_gives_up_on_the_first_response()
    {
        RateLimitEvent captured = null;
        var log = Logger();
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()))
            .Callback<RateLimitEvent>(rateLimitEvent => captured = rateLimitEvent)
            .Returns(Task.CompletedTask);
        using var invoker = Invoker(log.Object, new ScriptedHandler(
            Step(HttpStatusCode.TooManyRequests, TimeSpan.FromMinutes(2))), out var inner);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8443/a/b?token=abc");
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Sends.Should().Be(1);
        captured.ShortMessage.Should().Be(
            "Rate limit exceeded: outbound GET https://example.com:8443/a/b returned 429 after 1 attempt (retry-after too long)");
        captured.FullMessage.Should().Contain("Attempts: 1 of 3");
        captured.FullMessage.Should().Contain("Reason: RetryAfterTooLong");
        captured.FullMessage.Should().Contain("Last Retry-After: 120s");
        captured.FullMessage.Should().NotContain("token");
    }

    [Test]
    public async Task Idempotent_503_with_retry_after_is_retried_and_post_is_not()
    {
        var log = Logger();
        using var getInvoker = Invoker(log.Object, new ScriptedHandler(
            Step(HttpStatusCode.ServiceUnavailable, TimeSpan.FromMilliseconds(1)),
            Step(HttpStatusCode.OK)), out var getInner);
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        using var getResponse = await getInvoker.SendAsync(getRequest, CancellationToken.None);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getInner.Sends.Should().Be(2);

        using var postInvoker = Invoker(log.Object, new ScriptedHandler(
            Step(HttpStatusCode.ServiceUnavailable, TimeSpan.FromMilliseconds(1))), out var postInner);
        using var postRequest = new HttpRequestMessage(HttpMethod.Post, "https://example.com/resource");
        using var postResponse = await postInvoker.SendAsync(postRequest, CancellationToken.None);
        postResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        postInner.Sends.Should().Be(1);
        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Never);
    }

    [Test]
    public async Task Cancellation_during_the_wait_throws()
    {
        var cancellation = new CancellationTokenSource();
        var inner = new ScriptedHandler(Step(HttpStatusCode.TooManyRequests))
        {
            OnSend = () => cancellation.Cancel()
        };
        using var invoker = Invoker(Logger().Object, inner, out _, attempts: 3, baseMilliseconds: 5000, maxMilliseconds: 5000);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/slow");

        var act = async () => await invoker.SendAsync(request, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        inner.Sends.Should().Be(1);
    }

    private static async Task AssertSingleSend(HttpContent content)
    {
        var log = Logger();
        using var invoker = Invoker(log.Object, new ScriptedHandler(Step(HttpStatusCode.TooManyRequests)), out var inner);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/upload")
        {
            Content = content
        };

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Sends.Should().Be(1);
        log.Verify(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>()), Times.Never);
    }

    private static Mock<RateLimitEventLog> Logger()
    {
        var log = new Mock<RateLimitEventLog>(Mock.Of<IServiceScopeFactory>());
        log.Setup(writer => writer.WriteAsync(It.IsAny<RateLimitEvent>())).Returns(Task.CompletedTask);
        return log;
    }

    private static HttpMessageInvoker Invoker(
        RateLimitEventLog eventLog,
        ScriptedHandler inner,
        out ScriptedHandler script,
        int attempts = 3,
        int baseMilliseconds = 1,
        int maxMilliseconds = 20)
    {
        script = inner;
        var policy = OutboundRetryPolicy.FromConfig(new CommonConfig
        {
            OutboundRetryMaxAttempts = attempts,
            OutboundRetryBaseDelayMilliseconds = baseMilliseconds,
            OutboundRetryMaxDelayMilliseconds = maxMilliseconds
        });
        var handler = new RateLimitRetryHandler(policy, eventLog)
        {
            InnerHandler = inner
        };
        return new HttpMessageInvoker(handler, disposeHandler: true);
    }

    private static (HttpStatusCode Status, TimeSpan? RetryAfter) Step(HttpStatusCode status, TimeSpan? retryAfter = null)
    {
        return (status, retryAfter);
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, TimeSpan? RetryAfter)> _steps;
        public List<string> Bodies { get; } = new();
        public int Sends { get; private set; }
        public Action OnSend { get; set; }

        public ScriptedHandler(params (HttpStatusCode Status, TimeSpan? RetryAfter)[] steps)
        {
            _steps = new Queue<(HttpStatusCode, TimeSpan?)>(steps);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sends++;
            OnSend?.Invoke();
            if (request.Content is not null)
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));

            var (status, retryAfter) = _steps.Count > 0
                ? _steps.Dequeue()
                : (HttpStatusCode.OK, (TimeSpan?)null);
            var response = new HttpResponseMessage(status);
            if (retryAfter is TimeSpan delay)
                response.Headers.RetryAfter = new RetryConditionHeaderValue(delay);
            return response;
        }
    }

    private sealed class OneShotStream : Stream
    {
        private readonly byte[] _data;
        private int _position;

        public OneShotStream(byte[] data)
        {
            _data = data;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer) => ReadInto(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(ReadInto(buffer.Span));
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int ReadInto(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, _data.Length - _position);
            if (count <= 0)
                return 0;

            _data.AsSpan(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }
    }
}

[TestFixture]
public class OutboundRetryRegistrationTests
{
    [Test]
    public void Retry_handler_is_registered_only_when_attempts_exceed_one()
    {
        var enabled = Services();
        enabled.AddNopRateLimiting(new CommonConfig
        {
            PermitLimit = 0,
            OutboundRetryMaxAttempts = 3
        });
        enabled.Should().Contain(descriptor => descriptor.ServiceType == typeof(RateLimitRetryHandler));

        var disabled = Services();
        disabled.AddNopRateLimiting(new CommonConfig
        {
            PermitLimit = 0,
            OutboundRetryMaxAttempts = 1
        });
        disabled.Should().NotContain(descriptor => descriptor.ServiceType == typeof(RateLimitRetryHandler));
        disabled.Should().Contain(descriptor => descriptor.ServiceType == typeof(RateLimitEventLog));
    }

    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IServiceScopeFactory>());
        return services;
    }
}
