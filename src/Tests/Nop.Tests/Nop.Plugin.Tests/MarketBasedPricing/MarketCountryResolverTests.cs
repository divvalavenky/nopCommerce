using AwesomeAssertions;
using Nop.Plugin.Misc.MarketBasedPricing.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.MarketBasedPricing;

[TestFixture]
public class MarketCountryResolverTests
{
    [Test]
    public void PreferShipping_UsesShippingWhenPresent()
    {
        MarketCountryResolver.Resolve(true, 10, 20).Should().Be(10);
    }

    [Test]
    public void PreferShipping_FallsBackToBilling()
    {
        MarketCountryResolver.Resolve(true, null, 20).Should().Be(20);
        MarketCountryResolver.Resolve(true, 0, 20).Should().Be(20);
    }

    [Test]
    public void PreferBilling_UsesBillingWhenPresent()
    {
        MarketCountryResolver.Resolve(false, 10, 20).Should().Be(20);
    }

    [Test]
    public void PreferBilling_FallsBackToShipping()
    {
        MarketCountryResolver.Resolve(false, 10, null).Should().Be(10);
    }

    [Test]
    public void NoAddress_ReturnsNull()
    {
        MarketCountryResolver.Resolve(true, null, null).Should().BeNull();
        MarketCountryResolver.Resolve(false, 0, 0).Should().BeNull();
    }
}
