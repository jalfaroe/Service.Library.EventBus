namespace Service.Library.EventBus
{
    public interface IRetryPolicy
    {
        double MinimumAllowableRetrySeconds { get; }

        double MaximumAllowableRetrySeconds { get; }

        int MaximumRetryCount { get; }
    }
}