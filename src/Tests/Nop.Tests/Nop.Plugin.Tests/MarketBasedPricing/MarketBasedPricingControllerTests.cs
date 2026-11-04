using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Plugin.Misc.MarketBasedPricing;
using Nop.Plugin.Misc.MarketBasedPricing.Controllers;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;
using Nop.Plugin.Misc.MarketBasedPricing.Models;
using Nop.Plugin.Misc.MarketBasedPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Mvc;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.MarketBasedPricing;

[TestFixture]
public class MarketBasedPricingControllerTests
{
    private const string InvalidPriceMessage = "Enter a valid product ID and a non-negative price.";

    private Mock<IMarketPricingService> _marketPricing;
    private MarketBasedPricingController _controller;

    [SetUp]
    public void SetUp()
    {
        _marketPricing = new Mock<IMarketPricingService>();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(s => s.GetResourceAsync("Plugins.Misc.MarketBasedPricing.ProductPrice.Invalid"))
            .ReturnsAsync(InvalidPriceMessage);

        _controller = new MarketBasedPricingController(
            Mock.Of<ICountryService>(),
            localization.Object,
            _marketPricing.Object,
            Mock.Of<INotificationService>(),
            Mock.Of<IPermissionService>(),
            Mock.Of<IProductService>(),
            Mock.Of<ISettingService>(),
            Mock.Of<IStoreService>(),
            new MarketBasedPricingSettings());
    }

    [Test]
    public async Task Update_RejectsNegativePrice()
    {
        var entity = new MarketProductPrice { Id = 5, Price = 10m };
        _marketPricing.Setup(s => s.GetMarketProductPriceByIdAsync(5)).ReturnsAsync(entity);

        var result = await _controller.MarketProductPriceUpdate(new MarketProductPriceModel { Id = 5, Price = -0.01m });

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().NotBeNull();
        json.Value.GetType().GetProperty("error")!.GetValue(json.Value).Should().Be(InvalidPriceMessage);
        entity.Price.Should().Be(10m);
        _marketPricing.Verify(s => s.UpdateMarketProductPriceAsync(It.IsAny<MarketProductPrice>()), Times.Never);
        _marketPricing.Verify(s => s.GetMarketProductPriceByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Update_AllowsZeroPrice()
    {
        var entity = new MarketProductPrice { Id = 5, Price = 10m };
        _marketPricing.Setup(s => s.GetMarketProductPriceByIdAsync(5)).ReturnsAsync(entity);

        var result = await _controller.MarketProductPriceUpdate(new MarketProductPriceModel { Id = 5, Price = decimal.Zero });

        result.Should().BeOfType<NullJsonResult>();
        entity.Price.Should().Be(decimal.Zero);
        _marketPricing.Verify(s => s.UpdateMarketProductPriceAsync(entity), Times.Once);
    }

    [Test]
    public async Task Update_MissingRow_ReturnsEmptyJson()
    {
        _marketPricing.Setup(s => s.GetMarketProductPriceByIdAsync(9)).ReturnsAsync((MarketProductPrice)null);

        var result = await _controller.MarketProductPriceUpdate(new MarketProductPriceModel { Id = 9, Price = 3m });

        result.Should().BeOfType<NullJsonResult>();
        _marketPricing.Verify(s => s.UpdateMarketProductPriceAsync(It.IsAny<MarketProductPrice>()), Times.Never);
    }
}
