using AwesomeAssertions;
using Moq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.MarketBasedPricing.Services;
using Nop.Services.Catalog;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.MarketBasedPricing;

[TestFixture]
public class MarketBasedPriceCalculationServiceTests
{
    private Mock<IPriceCalculationService> _inner;
    private Mock<IMarketPricingService> _marketPricing;
    private Mock<IProductService> _productService;
    private MarketBasedPriceCalculationService _service;
    private Customer _customer;
    private Store _store;

    [SetUp]
    public void SetUp()
    {
        _inner = new Mock<IPriceCalculationService>();
        _marketPricing = new Mock<IMarketPricingService>();
        _productService = new Mock<IProductService>();
        _customer = new Customer();
        _store = new Store();

        _inner.Setup(s => s.GetFinalPriceAsync(
                It.IsAny<Product>(),
                It.IsAny<Customer>(),
                It.IsAny<Store>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync((Product product, Customer _, Store _, decimal? overriddenProductPrice, decimal additionalCharge, bool _, int _, DateTime? _, DateTime? _) =>
            {
                var price = (overriddenProductPrice ?? product.Price) + additionalCharge;
                return (price, price, decimal.Zero, new List<Discount>());
            });

        _inner.Setup(s => s.GetProductAttributeValuePriceAdjustmentAsync(
                It.IsAny<Product>(),
                It.IsAny<ProductAttributeValue>(),
                It.IsAny<Customer>(),
                It.IsAny<Store>(),
                It.IsAny<decimal?>(),
                It.IsAny<int>()))
            .ReturnsAsync((Product _, ProductAttributeValue value, Customer _, Store _, decimal? productPrice, int _) =>
            {
                if (value.PriceAdjustmentUsePercentage)
                    return (decimal)((float)(productPrice ?? 0) * (float)value.PriceAdjustment / 100f);

                return value.PriceAdjustment;
            });

        _service = new MarketBasedPriceCalculationService(_inner.Object, _marketPricing.Object, _productService.Object);
    }

    [Test]
    public async Task PercentageAdjustment_UsesMarketFinalPrice()
    {
        var product = new Product { Id = 1, Price = 100m };
        _marketPricing.Setup(s => s.GetMarketPriceAsync(product, _customer, _store)).ReturnsAsync(50m);

        var value = new ProductAttributeValue
        {
            AttributeValueType = AttributeValueType.Simple,
            PriceAdjustmentUsePercentage = true,
            PriceAdjustment = 10m
        };

        var adjustment = await _service.GetProductAttributeValuePriceAdjustmentAsync(product, value, _customer, _store);

        adjustment.Should().Be(5m);
        _inner.Verify(s => s.GetProductAttributeValuePriceAdjustmentAsync(
            product, value, _customer, _store, 50m, 1), Times.Once);
    }

    [Test]
    public async Task PercentageAdjustment_KeepsCallerSuppliedPrice()
    {
        var product = new Product { Id = 1, Price = 100m };
        _marketPricing.Setup(s => s.GetMarketPriceAsync(product, _customer, _store)).ReturnsAsync(50m);

        var value = new ProductAttributeValue
        {
            AttributeValueType = AttributeValueType.Simple,
            PriceAdjustmentUsePercentage = true,
            PriceAdjustment = 10m
        };

        var adjustment = await _service.GetProductAttributeValuePriceAdjustmentAsync(product, value, _customer, _store, productPrice: 20m);

        adjustment.Should().Be(2m);
        _marketPricing.Verify(s => s.GetMarketPriceAsync(It.IsAny<Product>(), It.IsAny<Customer>(), It.IsAny<Store>()), Times.Never);
    }

    [Test]
    public async Task AssociatedProductAdjustment_UsesMarketFinalPrice()
    {
        var product = new Product { Id = 1, Price = 100m };
        var associated = new Product { Id = 2, Price = 80m };
        _marketPricing.Setup(s => s.GetMarketPriceAsync(associated, _customer, _store)).ReturnsAsync(40m);
        _productService.Setup(s => s.GetProductByIdAsync(2)).ReturnsAsync(associated);

        var value = new ProductAttributeValue
        {
            AttributeValueType = AttributeValueType.AssociatedToProduct,
            AssociatedProductId = 2,
            Quantity = 3
        };

        var adjustment = await _service.GetProductAttributeValuePriceAdjustmentAsync(product, value, _customer, _store);

        adjustment.Should().Be(120m);
        _inner.Verify(s => s.GetProductAttributeValuePriceAdjustmentAsync(
            It.IsAny<Product>(),
            It.IsAny<ProductAttributeValue>(),
            It.IsAny<Customer>(),
            It.IsAny<Store>(),
            It.IsAny<decimal?>(),
            It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task AssociatedProductAdjustment_MissingProduct_ReturnsZero()
    {
        _productService.Setup(s => s.GetProductByIdAsync(2)).ReturnsAsync((Product)null);

        var value = new ProductAttributeValue
        {
            AttributeValueType = AttributeValueType.AssociatedToProduct,
            AssociatedProductId = 2,
            Quantity = 3
        };

        var adjustment = await _service.GetProductAttributeValuePriceAdjustmentAsync(new Product { Id = 1 }, value, _customer, _store);

        adjustment.Should().Be(0m);
    }

    [Test]
    public async Task AbsoluteAdjustment_DelegatesWithoutMarketLookup()
    {
        var product = new Product { Id = 1, Price = 100m };
        var value = new ProductAttributeValue
        {
            AttributeValueType = AttributeValueType.Simple,
            PriceAdjustmentUsePercentage = false,
            PriceAdjustment = 7m
        };

        var adjustment = await _service.GetProductAttributeValuePriceAdjustmentAsync(product, value, _customer, _store);

        adjustment.Should().Be(7m);
        _marketPricing.Verify(s => s.GetMarketPriceAsync(It.IsAny<Product>(), It.IsAny<Customer>(), It.IsAny<Store>()), Times.Never);
    }
}
