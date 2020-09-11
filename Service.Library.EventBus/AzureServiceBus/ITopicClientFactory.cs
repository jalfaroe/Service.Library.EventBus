using Microsoft.Azure.ServiceBus;

namespace Service.Library.EventBus.AzureServiceBus
{
    public interface ITopicClientFactory
    {
        ITopicClient Create(string connectionString, string topicName, RetryPolicyBase retryPolicy);
    }
}