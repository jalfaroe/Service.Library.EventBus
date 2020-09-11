using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Service.Library.EventBus.Internal;

namespace Service.Library.EventBus.AzureServiceBus
{
    [ExcludeFromCodeCoverage]
    public class EventPublisher : IEventPublisher, IDisposable
    {
        private readonly IEventPublisherConfiguration configuration;

        private readonly ITopicClientFactory topicClientFactory;

        private IDictionary<string, ITopicClient> topicClients;

        private bool disposed; // Have we been disposed

        private readonly object thisLock = new object();

        public EventPublisher(ITopicClientFactory topicClientFactory, IEventPublisherConfiguration configuration)
        {
            ValidateConstructorInputs(topicClientFactory, configuration);

            this.topicClientFactory = topicClientFactory;
            this.configuration = configuration;
            topicClients = new Dictionary<string, ITopicClient>();
        }

        ~EventPublisher()
        {
            /*
             * When a class is using unmanaged resources, and implements IDisposable, it should also define
             * a finalizer in case the consumer of the class doesn't call Dispose. When the GC is cleaning up
             * the dead object and sees the finalizer defined and it hasn't been told not to finalize that object,
             * it does run the finalizer. The finalizer is the last change for unmanaged resources to be safely
             * cleaned up.
             */
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            // Suppress finalization for this object, since we've already handled our resource cleanup tasks.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (thisLock)
            {
                // I only do the dispose if I haven't already been disposed.
                // This allow us to call this method multiple times safely.
                if (disposed)
                {
                    return;
                }

                // Cleanup managed resources.
                if (disposing)
                {
                    // If we have any managed, IDisposable resources, Dispose of them here.
                    // In this case, we don't, so this is unneeded.
                }

                // Cleanup unmanaged resources.
                ReleaseTopicClient();
                disposed = true;
            }
        }

        public Task PublishEventsAsync(IEnumerable<IntegrationEvent> eventsToPublish)
        {
            CanSendMessagesValidateAndThrow(eventsToPublish);
            return SendMessagesAsync(eventsToPublish);
        }

        private async Task SendMessagesAsync(IEnumerable<IntegrationEvent> eventsToPublish)
        {
            foreach (var @event in eventsToPublish)
            {
                await SendMessageAsync(@event).ConfigureAwait(false);
            }
        }

        private Task SendMessageAsync(IntegrationEvent @event)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(topicClients));
            }

            var publisherInfo = GetPublisherInfo(@event.GetType());
            ITopicClient topicClient;

            lock (thisLock)
            {
                if (topicClients.ContainsKey(publisherInfo.TopicName))
                {
                    topicClient = topicClients[publisherInfo.TopicName];
                }
                else
                {
                    topicClient = topicClientFactory.Create(configuration.ServiceBusConnectionString,
                        publisherInfo.TopicName, publisherInfo.RetryPolicy);
                    topicClients.Add(publisherInfo.TopicName, topicClient);
                }
            }

            var message = CreateServiceBusMessage(@event);
            return topicClient.SendAsync(message);
        }

        private static Message CreateServiceBusMessage(IntegrationEvent @event)
        {
            var json = JsonConvert.SerializeObject(@event);

            return new Message
            {
                MessageId = Guid.NewGuid().ToString("N", CultureInfo.CurrentCulture),
                Body = Encoding.UTF8.GetBytes(json),
                Label = @event.GetType().Name
            };
        }

        private void CanSendMessagesValidateAndThrow(IEnumerable<IntegrationEvent> eventsToPublish)
        {
            // There is a list of events to publish.
            eventsToPublish.GuardArgumentIsNotNull(nameof(eventsToPublish));

            // All events must have a topic name mapped.
            foreach (var @event in eventsToPublish)
            {
                var publisherInfo = GetPublisherInfo(@event.GetType());

                if (publisherInfo == null)
                {
                    throw new InvalidOperationException($"{Constants.CannotProcessMessagesMsg} {@event.GetType()}");
                }
            }
        }

        private PublisherInfo GetPublisherInfo(MemberInfo eventType)
        {
            return configuration.Publishers.FirstOrDefault(publisherInfo =>
                publisherInfo.EventType.Name.Equals(eventType.Name, StringComparison.CurrentCultureIgnoreCase));
        }

        private static void ValidateConstructorInputs(
            ITopicClientFactory topicClientFactory,
            IEventPublisherConfiguration configuration)
        {
            topicClientFactory.GuardArgumentIsNotNull(nameof(topicClientFactory));

            configuration.GuardArgumentIsNotNull(nameof(configuration));
            configuration.ServiceBusConnectionString.GuardArgumentIsNotNullOrEmpty(
                $"{nameof(configuration)}.{nameof(configuration.ServiceBusConnectionString)}");
            configuration.Publishers.GuardArgumentIsNotNullOrEmpty(
                $"{nameof(configuration)}.{nameof(configuration.Publishers)}");
        }

        private void ReleaseTopicClient()
        {
            foreach (var client in topicClients)
            {
                if (!client.Value.IsClosedOrClosing)
                {
                    client.Value.CloseAsync();
                }
            }

            topicClients.Clear();
            topicClients = null;
        }
    }
}