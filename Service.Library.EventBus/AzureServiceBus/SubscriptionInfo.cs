using System;

namespace Service.Library.EventBus.AzureServiceBus
{
    public class SubscriptionInfo : ISubscriptionInfo
    {
        public SubscriptionInfo(
            Type eventType,
            Type handlerType,
            string topicName,
            string subscriptionName,
            IRetryPolicy retryPolicy)
        {
            EventType = eventType;
            HandlerType = handlerType;
            TopicName = topicName;
            SubscriptionName = subscriptionName;
            RetryPolicy = retryPolicy;
        }

        public Type EventType { get; }

        public Type HandlerType { get; }

        public string TopicName { get; }

        public string SubscriptionName { get; }

        public IRetryPolicy RetryPolicy { get; }
    }
}