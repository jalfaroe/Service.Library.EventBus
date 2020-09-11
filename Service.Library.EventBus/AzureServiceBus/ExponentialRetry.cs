namespace Service.Library.EventBus.AzureServiceBus
{
    public class ExponentialRetry : RetryPolicyBase
    {
        public ExponentialRetry(double minimumAllowableRetryTime, double maximumAllowableRetryTime,
            int maximumRetryCount)
        {
            MinimumAllowableRetrySeconds = minimumAllowableRetryTime;
            MaximumAllowableRetrySeconds = maximumAllowableRetryTime;
            MaximumRetryCount = maximumRetryCount;
        }

        public override double MinimumAllowableRetrySeconds { get; }

        public override double MaximumAllowableRetrySeconds { get; }

        public override int MaximumRetryCount { get; }
    }
}