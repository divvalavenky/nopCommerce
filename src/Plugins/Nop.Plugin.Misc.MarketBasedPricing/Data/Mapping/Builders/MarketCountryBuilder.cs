using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;

namespace Nop.Plugin.Misc.MarketBasedPricing.Data.Mapping.Builders;

public class MarketCountryBuilder : NopEntityBuilder<MarketCountry>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(MarketCountry.MarketId)).AsInt32().NotNullable()
            .WithColumn(nameof(MarketCountry.CountryId)).AsInt32().NotNullable();
    }
}
