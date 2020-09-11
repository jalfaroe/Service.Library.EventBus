using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Library.EventBus
{
    /// <summary>
    ///     A service publishes a message to the Service Bus using the IEventPublisher interface.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        ///     Publishes a message to the Service Bus.
        /// </summary>
        /// <param name="eventsToPublish">
        ///     The eventsToPublish parameter is the list of events to publish to the Service Bus.
        /// </param>
        Task PublishEventsAsync(IEnumerable<IntegrationEvent> eventsToPublish);
    }
}