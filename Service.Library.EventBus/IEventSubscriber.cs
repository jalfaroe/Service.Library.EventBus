namespace Service.Library.EventBus
{
    /// <summary>
    ///     A service subscribes to messages using the IEventSubscriber interface.
    ///     Any class that implements this interface must get by configuration (by constructor injection)
    ///     the list of events, event handlers, and all the necessary to do the subscriptions.
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        ///     Dynamically enable detailed tracing
        /// </summary>
        bool TraceEnabled { get; set; }

        /// <summary>
        ///     Register on message handlers and start receiving messages.
        /// </summary>
        void StartReceivingEvents();
    }
}