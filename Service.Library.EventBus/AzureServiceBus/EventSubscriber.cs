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
using Service.Library.EventBus.Loggers;

namespace Service.Library.EventBus.AzureServiceBus
{
    [ExcludeFromCodeCoverage]
    public class EventSubscriber : IEventSubscriber, IDisposable
    {
        private IEventSubscriberConfiguration configuration;

        private IServiceProvider inversionOfControlContainer;

        private ISubscriptionClientFactory subscriptionClientFactory;

        private IDictionary<string, ISubscriptionClient> subscriptionClients;

        private ILogger logger;

        private bool disposed; // Have we been disposed

        private readonly object thisLock = new object();

        public EventSubscriber(ISubscriptionClientFactory subscriptionClientFactory,
            IServiceProvider inversionOfControlContainer,
            IEventSubscriberConfiguration configuration)
        {
            InitializeInstance(subscriptionClientFactory,
                inversionOfControlContainer,
                configuration,
                new NullLogger());
        }

        public EventSubscriber(ISubscriptionClientFactory subscriptionClientFactory,
            IServiceProvider inversionOfControlContainer,
            IEventSubscriberConfiguration configuration,
            ILogger logger)
        {
            InitializeInstance(subscriptionClientFactory, inversionOfControlContainer, configuration, logger);
        }

        ~EventSubscriber()
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
                ReleaseSubscriptionClients();
                disposed = true;
            }
        }

        public bool TraceEnabled { get; set; }

        public void StartReceivingEvents()
        {
            // Initialize the subscriptions to the Azure Service Bus Topics
            foreach (var subscriptionInfo in configuration.Subscriptions)
            {
                InitializeSubscription(subscriptionInfo);
            }
        }

        private void InitializeSubscription(ISubscriptionInfo subscriptionInfo)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(subscriptionClients));
            }

            lock (thisLock)
            {
                if (subscriptionClients.ContainsKey(subscriptionInfo.TopicName))
                {
                    return;
                }
            }

            var subscriptionClient = subscriptionClientFactory.Create(
                configuration.ServiceBusConnectionString,
                subscriptionInfo.TopicName,
                subscriptionInfo.SubscriptionName,
                subscriptionInfo.RetryPolicy);

            // Register the function that processes messages.
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    await ProcessMessagesAsync(message).ConfigureAwait(false);
                    await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                },
                new MessageHandlerOptions(ExceptionReceivedHandlerAsync)
                {
                    // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                    // Set it according to how many messages the application wants to process in parallel.
                    MaxConcurrentCalls = 1,

                    // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                    // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                    AutoComplete = false
                });


            lock (thisLock)
            {
                subscriptionClients.Add(subscriptionInfo.TopicName, subscriptionClient);
            }
        }

        private async Task ProcessMessagesAsync(Message message)
        {
            var eventName = message.Label;

            var subscriptionInfo = GetSubscriptionInfo(eventName);

            if (subscriptionInfo != null)
            {
                try
                {
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    if (TraceEnabled)
                    {
                        logger.LogTrace(subscriptionInfo, string.Format(CultureInfo.CurrentCulture, "MessageId {0}, CorrelationId {1}: {2}", message.MessageId, message.CorrelationId, messageData));
                    }

                    var integrationEventToHandle = JsonConvert.DeserializeObject(messageData, subscriptionInfo.EventType);
                    var handler = inversionOfControlContainer.GetService(subscriptionInfo.HandlerType);
                    var handlerConcreteType =
                        typeof(IIntegrationEventHandler<>).MakeGenericType(subscriptionInfo.EventType);

                    await ((Task)handlerConcreteType.GetMethod(Constants.EventHandleMethodName).Invoke(
                        handler,
                        BindingFlags.Default,
                        null,
                        new[] { integrationEventToHandle },
                        CultureInfo.CurrentCulture)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogCriticalError(ex, subscriptionInfo);
                    throw;
                }
            }
        }

        private Task ExceptionReceivedHandlerAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs.Exception is ServiceBusException serviceBusException &&
                serviceBusException.IsTransient)
            {
                logger.LogError(exceptionReceivedEventArgs.Exception);
            }
            else
            {
                logger.LogCriticalError(exceptionReceivedEventArgs.Exception);
            }

            return Task.CompletedTask;
        }

        private SubscriptionInfo GetSubscriptionInfo(string eventName)
        {
            return configuration.Subscriptions.FirstOrDefault(
                subscriptionInfo => subscriptionInfo.EventType.Name.Equals(
                    eventName,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        private void InitializeInstance(ISubscriptionClientFactory subscriptionFactory,
            IServiceProvider iocContainer,
            IEventSubscriberConfiguration config,
            ILogger logging)
        {
            ValidateConstructorInputs(subscriptionFactory, iocContainer, config);

            subscriptionClientFactory = subscriptionFactory;
            inversionOfControlContainer = iocContainer;
            configuration = config;
            logger = logging;

            subscriptionClients = new Dictionary<string, ISubscriptionClient>();
        }

        private static void ValidateConstructorInputs(
            ISubscriptionClientFactory subscriptionClientFactory,
            IServiceProvider inversionOfControlContainer,
            IEventSubscriberConfiguration configuration)
        {
            subscriptionClientFactory.GuardArgumentIsNotNull(nameof(subscriptionClientFactory));
            inversionOfControlContainer.GuardArgumentIsNotNull(nameof(inversionOfControlContainer));

            configuration.GuardArgumentIsNotNull(nameof(configuration));
            configuration.ServiceBusConnectionString.GuardArgumentIsNotNullOrEmpty(
                $"{nameof(configuration)}.{nameof(configuration.ServiceBusConnectionString)}");
            configuration.Subscriptions.GuardArgumentIsNotNullOrEmpty(
                $"{nameof(configuration)}.{nameof(configuration.Subscriptions)}");
        }

        private void ReleaseSubscriptionClients()
        {
            foreach (var client in subscriptionClients)
            {
                if (!client.Value.IsClosedOrClosing)
                {
                    client.Value.CloseAsync();
                }
            }

            subscriptionClients.Clear();
            subscriptionClients = null;
        }
    }
}