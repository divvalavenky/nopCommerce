using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.MarketBasedPricing.Domain;

namespace Nop.Plugin.Misc.MarketBasedPricing.Data.Migrations;

[NopMigration("2026-11-03 10:00:00", "Misc.MarketBasedPricing base schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<Market>();
        this.CreateTableIfNotExists<MarketCountry>();
        this.CreateTableIfNotExists<MarketProductPrice>();
    }

    public override void Down()
    {
        this.DeleteTableIfExists<MarketProductPrice>();
        this.DeleteTableIfExists<MarketCountry>();
        this.DeleteTableIfExists<Market>();
    }
}
