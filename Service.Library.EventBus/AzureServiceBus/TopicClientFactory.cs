using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.ServiceBus;
using Service.Library.EventBus.Internal;

namespace Service.Library.EventBus.AzureServiceBus
{
    [ExcludeFromCodeCoverage]
    public class TopicClientFactory : ITopicClientFactory
    {
        public ITopicClient Create(string connectionString, string topicName, RetryPolicyBase retryPolicy)
        {
            ValidateInputs(connectionString, topicName, retryPolicy);

            if (retryPolicy == RetryPolicyBase.NoRetry)
                return new TopicClient(connectionString, topicName, new NoRetry());

            /*
             * Uses an exponential back off, this means that the first retry will be performed within a fairly short space of time,
             * and the retries will get a progressively longer.
             */
            var policy = new RetryExponential(
                TimeSpan.FromSeconds(retryPolicy.MinimumAllowableRetrySeconds),
                TimeSpan.FromSeconds(retryPolicy.MaximumAllowableRetrySeconds),
                retryPolicy.MaximumRetryCount
            );

            return new TopicClient(connectionString, topicName, policy);
        }

        private static void ValidateInputs(string connectionString, string topicName, RetryPolicyBase retryPolicy)
        {
            connectionString.GuardArgumentIsNotNullOrEmpty(nameof(connectionString));
            topicName.GuardArgumentIsNotNullOrEmpty(nameof(topicName));
            retryPolicy.GuardArgumentIsNotNull(nameof(retryPolicy));
        }
    }
}