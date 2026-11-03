using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;

namespace Nop.Plugin.Misc.MarketBasedPricing.Data.Mapping.Builders;

public class MarketBuilder : NopEntityBuilder<Market>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table.WithColumn(nameof(Market.Name)).AsString(400).NotNullable();
    }
}
