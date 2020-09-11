using System.Collections.Generic;

namespace Service.Library.EventBus.AzureServiceBus
{
    public interface IEventPublisherConfiguration
    {
        string ServiceBusConnectionString { get; }

        IList<PublisherInfo> Publishers { get; }
    }
}