using System.Net;
using AwesomeAssertions;
using Nop.Core.Configuration;
using Nop.Web.Framework.RateLimiting;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.RateLimiting;

[TestFixture]
public class OutboundRetryPolicyTests
{
    [Test]
    public void FromConfig_uses_the_shipped_defaults()
    {
        var policy = OutboundRetryPolicy.FromConfig(new CommonConfig());

        policy.MaxAttempts.Should().Be(3);
        policy.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(200));
        policy.MaxDelay.Should().Be(TimeSpan.FromMilliseconds(2000));
    }

    [TestCase(1)]
    [TestCase(0)]
    [TestCase(-4)]
    public void FromConfig_returns_null_when_retry_is_off(int attempts)
    {
        OutboundRetryPolicy.FromConfig(new CommonConfig { OutboundRetryMaxAttempts = attempts }).Should().BeNull();
    }

    [Test]
    public void FromConfig_clamps_attempts_and_delays()
    {
        var clampedAttempts = OutboundRetryPolicy.FromConfig(new CommonConfig
        {
            OutboundRetryMaxAttempts = 9,
            OutboundRetryBaseDelayMilliseconds = 200,
            OutboundRetryMaxDelayMilliseconds = 2000
        });
        clampedAttempts.MaxAttempts.Should().Be(5);

        var clampedDelays = OutboundRetryPolicy.FromConfig(new CommonConfig
        {
            OutboundRetryMaxAttempts = 2,
            OutboundRetryBaseDelayMilliseconds = 0,
            OutboundRetryMaxDelayMilliseconds = -10
        });
        clampedDelays.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(1));
        clampedDelays.MaxDelay.Should().Be(TimeSpan.FromMilliseconds(1));

        var raisedMax = OutboundRetryPolicy.FromConfig(new CommonConfig
        {
            OutboundRetryMaxAttempts = 2,
            OutboundRetryBaseDelayMilliseconds = 500,
            OutboundRetryMaxDelayMilliseconds = 100
        });
        raisedMax.MaxDelay.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Test]
    public void Decide_delivers_when_the_response_is_not_throttled()
    {
        var policy = Policy(3, 200, 2000);

        policy.Decide(new RetryAttempt(3), Throttle.None.Instance, 0.9)
            .Should().Be(RetryDecision.Deliver.Instance);
    }

    [Test]
    public void Decide_waits_for_the_server_retry_after_and_ignores_jitter()
    {
        var policy = Policy(4, 200, 2000);
        var throttled = new Throttle.Limited(HttpStatusCode.TooManyRequests, TimeSpan.FromMilliseconds(250));

        policy.Decide(RetryAttempt.First, throttled, 0.9)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(250), new RetryAttempt(2)));
    }

    [Test]
    public void Decide_honors_retry_after_up_to_60_seconds_even_when_max_delay_is_shorter()
    {
        var shortBackoffCap = Policy(4, 200, 2000);
        shortBackoffCap.Decide(RetryAttempt.First, Limited(TimeSpan.FromSeconds(30)), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromSeconds(30), new RetryAttempt(2)));
        shortBackoffCap.Decide(RetryAttempt.First, Limited(TimeSpan.FromSeconds(60)), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromSeconds(60), new RetryAttempt(2)));
        shortBackoffCap.Decide(RetryAttempt.First, Limited(TimeSpan.FromSeconds(61)), 0)
            .Should().BeOfType<RetryDecision.GiveUp>()
            .Which.Reason.Should().Be(GiveUpReason.RetryAfterTooLong);

        var longBackoffCap = Policy(4, 200, 90_000);
        longBackoffCap.Decide(RetryAttempt.First, Limited(TimeSpan.FromSeconds(60)), 0.5)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromSeconds(60), new RetryAttempt(2)));
        longBackoffCap.Decide(RetryAttempt.First, Limited(TimeSpan.FromSeconds(61)), 0.5)
            .Should().BeOfType<RetryDecision.GiveUp>()
            .Which.Reason.Should().Be(GiveUpReason.RetryAfterTooLong);
    }

    [Test]
    public void Decide_uses_equal_jitter_and_caps_the_step_at_max_delay()
    {
        var policy = Policy(6, 1000, 2500);

        policy.Decide(RetryAttempt.First, Limited(null), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(500), new RetryAttempt(2)));
        policy.Decide(RetryAttempt.First, Limited(null), 0.5)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(750), new RetryAttempt(2)));
        policy.Decide(new RetryAttempt(2), Limited(null), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(1000), new RetryAttempt(3)));
        policy.Decide(new RetryAttempt(2), Limited(null), 0.5)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(1500), new RetryAttempt(3)));
        policy.Decide(new RetryAttempt(3), Limited(null), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(1250), new RetryAttempt(4)));
        policy.Decide(new RetryAttempt(3), Limited(null), 0.5)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(1875), new RetryAttempt(4)));
    }

    [Test]
    public void Decide_gives_up_on_the_shipped_third_attempt()
    {
        var policy = OutboundRetryPolicy.FromConfig(new CommonConfig());

        policy.Decide(RetryAttempt.First, Limited(null), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(100), new RetryAttempt(2)));
        policy.Decide(new RetryAttempt(2), Limited(null), 0)
            .Should().Be(new RetryDecision.Wait(TimeSpan.FromMilliseconds(200), new RetryAttempt(3)));

        var giveUp = policy.Decide(new RetryAttempt(3), Limited(TimeSpan.FromMilliseconds(10)), 0)
            .Should().BeOfType<RetryDecision.GiveUp>().Subject;
        giveUp.Reason.Should().Be(GiveUpReason.AttemptsExhausted);
        giveUp.Final.Number.Should().Be(3);
        giveUp.MaxAttempts.Should().Be(3);
    }

    private static OutboundRetryPolicy Policy(int attempts, int baseMilliseconds, int maxMilliseconds)
    {
        return OutboundRetryPolicy.FromConfig(new CommonConfig
        {
            OutboundRetryMaxAttempts = attempts,
            OutboundRetryBaseDelayMilliseconds = baseMilliseconds,
            OutboundRetryMaxDelayMilliseconds = maxMilliseconds
        });
    }

    private static Throttle.Limited Limited(TimeSpan? retryAfter)
    {
        return new Throttle.Limited(HttpStatusCode.TooManyRequests, retryAfter);
    }
}
