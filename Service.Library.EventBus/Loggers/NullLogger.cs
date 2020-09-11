using System;
using System.Diagnostics.CodeAnalysis;

namespace Service.Library.EventBus.Loggers
{
    [ExcludeFromCodeCoverage]
    internal class NullLogger : ILogger
    {
        public void LogTrace(ISubscriptionInfo subscriptionInfo)
        {
            // Stub implementation
        }

        public void LogTrace(string details)
        {
            // Stub implementation
        }

        public void LogTrace(ISubscriptionInfo subscriptionInfo, string details)
        {
            // Stub implementation
        }

        public void LogError(Exception ex)
        {
            // Stub implementation
        }

        public void LogError(Exception ex, ISubscriptionInfo subscriptionInfo)
        {
            // Stub implementation
        }

        public void LogError(Exception ex, string additionalDetails)
        {
            // Stub implementation
        }

        public void LogError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails)
        {
            // Stub implementation
        }

        public void LogCriticalError(Exception ex)
        {
            // Stub implementation
        }

        public void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo)
        {
            // Stub implementation
        }

        public void LogCriticalError(Exception ex, string additionalDetails)
        {
            // Stub implementation
        }

        public void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails)
        {
            // Stub implementation
        }
    }
}