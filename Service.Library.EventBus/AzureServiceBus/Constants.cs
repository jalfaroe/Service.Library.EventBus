namespace Service.Library.EventBus.AzureServiceBus
{
    internal static class Constants
    {
        public const string EventHandleMethodName = "HandleAsync";

        // Messages
        public const string CannotProcessMessagesMsg =
            "Can't process messages - there isn't a configured Topic Name to";
    }
}