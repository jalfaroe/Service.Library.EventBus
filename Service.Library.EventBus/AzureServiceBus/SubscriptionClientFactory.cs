using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.ServiceBus;
using Service.Library.EventBus.Internal;

namespace Service.Library.EventBus.AzureServiceBus
{
    [ExcludeFromCodeCoverage]
    public class SubscriptionClientFactory : ISubscriptionClientFactory
    {
        public ISubscriptionClient Create(string connectionString, string topicName, string subscriptionName,
            IRetryPolicy retryPolicy)
        {
            ValidateInputs(connectionString, topicName, subscriptionName, retryPolicy);

            if (retryPolicy == RetryPolicyBase.NoRetry)
                return new SubscriptionClient(
                    connectionString,
                    topicName,
                    subscriptionName,
                    ReceiveMode.PeekLock,
                    new NoRetry());

            /*
             * Uses an exponential back off, this means that the first retry will be performed within a fairly short space of time,
             * and the retries will get a progressively longer.
             */
            var policy = new RetryExponential(
                TimeSpan.FromSeconds(retryPolicy.MinimumAllowableRetrySeconds),
                TimeSpan.FromSeconds(retryPolicy.MaximumAllowableRetrySeconds),
                retryPolicy.MaximumRetryCount
            );

            return new SubscriptionClient(
                connectionString,
                topicName,
                subscriptionName,
                ReceiveMode.PeekLock,
                policy);
        }

        private static void ValidateInputs(string connectionString, string topicName, string subscriptionName,
            IRetryPolicy retryPolicy)
        {
            connectionString.GuardArgumentIsNotNullOrEmpty(nameof(connectionString));
            topicName.GuardArgumentIsNotNullOrEmpty(nameof(topicName));
            subscriptionName.GuardArgumentIsNotNullOrEmpty(nameof(subscriptionName));
            retryPolicy.GuardArgumentIsNotNull(nameof(retryPolicy));
        }
    }
}