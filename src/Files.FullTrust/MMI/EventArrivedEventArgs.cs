using Microsoft.Management.Infrastructure;
using System;

namespace Files.FullTrust.MMI
{
    /// <summary>
    /// CimWatcher event args, which contains CimSubscriptionResult
    /// </summary>
    public class EventArrivedEventArgs : EventArgs
    {
        public CimSubscriptionResult NewEvent { get; }

        public EventArrivedEventArgs(CimSubscriptionResult cimSubscriptionResult)
        {
            NewEvent = cimSubscriptionResult;
        }
    }
}
