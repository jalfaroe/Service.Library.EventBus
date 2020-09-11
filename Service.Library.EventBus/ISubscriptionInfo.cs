using System;

namespace Service.Library.EventBus
{
    public interface ISubscriptionInfo
    {
        Type EventType { get; }

        Type HandlerType { get; }

        string TopicName { get; }

        string SubscriptionName { get; }

        IRetryPolicy RetryPolicy { get; }
    }
}