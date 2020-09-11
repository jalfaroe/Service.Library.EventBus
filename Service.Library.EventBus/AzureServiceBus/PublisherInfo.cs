using System;

namespace Service.Library.EventBus.AzureServiceBus
{
    public class PublisherInfo
    {
        public PublisherInfo(Type eventType, string topicName, RetryPolicyBase retryPolicy)
        {
            EventType = eventType;
            TopicName = topicName;
            RetryPolicy = retryPolicy;
        }

        public Type EventType { get; }

        public string TopicName { get; }

        public RetryPolicyBase RetryPolicy { get; }
    }
}