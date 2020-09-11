using System;

namespace Service.Library.EventBus
{
    public interface ILogger
    {
        void LogTrace(ISubscriptionInfo subscriptionInfo);

        void LogTrace(string details);

        void LogTrace(ISubscriptionInfo subscriptionInfo, string details);

        void LogError(Exception ex);

        void LogError(Exception ex, ISubscriptionInfo subscriptionInfo);

        void LogError(Exception ex, string additionalDetails);

        void LogError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails);

        void LogCriticalError(Exception ex);

        void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo);

        void LogCriticalError(Exception ex, string additionalDetails);

        void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails);
    }
}