using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Service.Library.EventBus.Loggers
{
    [ExcludeFromCodeCoverage]
    public class ApplicationInsightLogger : ILogger
    {
        private const string CommonErrorMessage = "Event Bus Error";
        private const string CriticalErrorMessage = "Event Bus Critical Error";

        private const string SubscriptionDetailsMessage =
            "Processing message for event {0} from topic {1} and subscription {2}, and sending to event handler {3}.";

        private readonly TelemetryClient telemetryClient;

        public ApplicationInsightLogger(string instrumentationKey)
        {
            var telemetryConfiguration = new TelemetryConfiguration(instrumentationKey);
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public void LogTrace(ISubscriptionInfo subscriptionInfo)
        {
            LogTraceImpl(string.Empty);
        }

        public void LogTrace(string details)
        {
            LogTraceImpl(FormatMessage(null, details));
        }

        public void LogTrace(ISubscriptionInfo subscriptionInfo, string details)
        {
            LogTraceImpl(FormatMessage(subscriptionInfo, details));
        }

        public void LogError(Exception ex)
        {
            LogErrorImpl(ex, SeverityLevel.Error, null);
        }

        public void LogError(Exception ex, ISubscriptionInfo subscriptionInfo)
        {
            LogErrorImpl(ex, SeverityLevel.Error, FormatMessage(subscriptionInfo, null));
        }

        public void LogError(Exception ex, string additionalDetails)
        {
            LogErrorImpl(ex, SeverityLevel.Error, FormatMessage(null, additionalDetails));
        }

        public void LogError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails)
        {
            LogErrorImpl(ex, SeverityLevel.Error, FormatMessage(subscriptionInfo, additionalDetails));
        }

        public void LogCriticalError(Exception ex)
        {
            LogErrorImpl(ex, SeverityLevel.Critical, null);
        }

        public void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo)
        {
            LogErrorImpl(ex, SeverityLevel.Critical, FormatMessage(subscriptionInfo, null));
        }

        public void LogCriticalError(Exception ex, string additionalDetails)
        {
            LogErrorImpl(ex, SeverityLevel.Critical, FormatMessage(null, additionalDetails));
        }

        public void LogCriticalError(Exception ex, ISubscriptionInfo subscriptionInfo, string additionalDetails)
        {
            LogErrorImpl(ex, SeverityLevel.Critical, FormatMessage(subscriptionInfo, additionalDetails));
        }

        private void LogTraceImpl(string message)
        {
            var telemetry = new TraceTelemetry
            {
                Timestamp = DateTimeOffset.Now,
                Message = message,
                SeverityLevel = SeverityLevel.Verbose
            };

            telemetryClient.TrackTrace(telemetry);
        }

        private void LogErrorImpl(Exception ex, SeverityLevel severity, string message)
        {
            var telemetry = new ExceptionTelemetry(ex ?? throw new ArgumentNullException(nameof(ex)))
            {
                Timestamp = DateTimeOffset.Now,
                Message = string.Format(CultureInfo.CurrentCulture, "{0}: {1}",
                    severity == SeverityLevel.Critical ? CriticalErrorMessage : CommonErrorMessage, message),
                SeverityLevel = severity
            };

            telemetryClient.TrackException(telemetry);
        }

        private static string FormatMessage(ISubscriptionInfo subscriptionInfo, string additionalDetails)
        {
            var message = string.Empty;

            if (subscriptionInfo != null)
            {
                message = string.Format(CultureInfo.CurrentCulture, SubscriptionDetailsMessage,
                    subscriptionInfo.EventType, subscriptionInfo.TopicName,
                    subscriptionInfo.SubscriptionName, subscriptionInfo.HandlerType.FullName);

                if (!string.IsNullOrEmpty(additionalDetails))
                    message = string.Concat(message, Environment.NewLine, Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(additionalDetails)) message += additionalDetails;
            return message;
        }
    }
}