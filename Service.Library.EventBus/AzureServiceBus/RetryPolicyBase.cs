namespace Service.Library.EventBus.AzureServiceBus
{
    public abstract class RetryPolicyBase : IRetryPolicy
    {
        public static RetryPolicyBase DefaultRetry { get; } = new DefaultRetryPolicy();

        public static RetryPolicyBase NoRetry { get; } = new NoRetryPolicy();
        public abstract double MinimumAllowableRetrySeconds { get; }

        public abstract double MaximumAllowableRetrySeconds { get; }

        public abstract int MaximumRetryCount { get; }

        private sealed class DefaultRetryPolicy : RetryPolicyBase
        {
            private const double MinSeconds = 0.1;
            private const int MaxSeconds = 30;
            private const int MaxRetryCount = 3;

            public override double MinimumAllowableRetrySeconds
            {
                get => MinSeconds;
            }

            public override double MaximumAllowableRetrySeconds
            {
                get => MaxSeconds;
            }

            public override int MaximumRetryCount
            {
                get => MaxRetryCount;
            }
        }

        private sealed class NoRetryPolicy : RetryPolicyBase
        {
            public override double MinimumAllowableRetrySeconds
            {
                get => 0;
            }

            public override double MaximumAllowableRetrySeconds
            {
                get => 0;
            }

            public override int MaximumRetryCount
            {
                get => 0;
            }
        }
    }
}