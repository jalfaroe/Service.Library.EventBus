using Microsoft.Azure.ServiceBus;

namespace Service.Library.EventBus.AzureServiceBus
{
    public interface ISubscriptionClientFactory
    {
        ISubscriptionClient Create(string connectionString, string topicName, string subscriptionName,
            IRetryPolicy retryPolicy);
    }
}