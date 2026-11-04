using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;

namespace Nop.Plugin.Misc.MarketBasedPricing.Data.Mapping.Builders;

public class MarketProductPriceBuilder : NopEntityBuilder<MarketProductPrice>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(MarketProductPrice.MarketId)).AsInt32().NotNullable()
            .WithColumn(nameof(MarketProductPrice.ProductId)).AsInt32().NotNullable()
            .WithColumn(nameof(MarketProductPrice.Price)).AsDecimal(18, 4).NotNullable();
    }
}
