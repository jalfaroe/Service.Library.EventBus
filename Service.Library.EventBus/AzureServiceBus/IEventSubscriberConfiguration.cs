using System.Collections.Generic;

namespace Service.Library.EventBus.AzureServiceBus
{
    public interface IEventSubscriberConfiguration
    {
        string ServiceBusConnectionString { get; }

        IList<SubscriptionInfo> Subscriptions { get; }
    }
}