using Nop.Services.Plugins;
using Nop.Web.Framework.Events;

namespace Nop.Plugin.Misc.MarketBasedPricing.Services;

/// <summary>
/// Represents plugin admin menu event consumer
/// </summary>
public class EventConsumer : BaseAdminMenuCreatedEventConsumer
{
    public EventConsumer(IPluginManager<IPlugin> pluginManager) :
        base(pluginManager)
    {
    }

    protected override string PluginSystemName => MarketBasedPricingDefaults.SystemName;

    protected override string BeforeMenuSystemName => "Local plugins";
}
