#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// The way of the event synchronization.
    /// </summary>
    public enum EventSynchronizationStrategy
    {
        /// <summary>
        /// Events are called synchronously if user can do some action; that is, cancel the execution process for example.
        /// </summary>
        Default,
        /// <summary>
        /// Always call events asynchronously.
        /// </summary>
        AlwaysAsynchronous,
        /// <summary>
        /// Always call events synchronously.
        /// </summary>
        AlwaysSynchronous
    }
}

#endif
