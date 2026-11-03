using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.MarketBasedPricing.Models;

public record MarketSearchModel : BaseSearchModel;

public record MarketListModel : BasePagedListModel<MarketModel>;

public record MarketModel : BaseNopEntityModel
{
    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Active")]
    public bool Active { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Store")]
    public int StoreId { get; set; }

    public string StoreName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Countries")]
    public IList<int> SelectedCountryIds { get; set; } = new List<int>();

    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public MarketProductPriceSearchModel MarketProductPriceSearchModel { get; set; } = new();
}

public record MarketProductPriceSearchModel : BaseSearchModel
{
    public int MarketId { get; set; }
}

public record MarketProductPriceListModel : BasePagedListModel<MarketProductPriceModel>;

public record MarketProductPriceModel : BaseNopEntityModel
{
    public int MarketId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.ProductId")]
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.MarketBasedPricing.Fields.Price")]
    public decimal Price { get; set; }
}
