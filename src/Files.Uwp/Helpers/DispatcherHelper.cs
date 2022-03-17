using Microsoft.Toolkit.Uwp;
using System;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Core;

namespace Files.Helpers
{
    /// <summary>
    /// This class provides static methods helper for executing code in UI thread of the main window.
    /// </summary>
    internal static class DispatcherQueueHelper
    { /// <summary>
      /// This struct represents an awaitable dispatcher.
      /// </summary>
        public struct DispatcherQueuePriorityAwaitable
        {
            private readonly DispatcherQueue dispatcher;
            private readonly DispatcherQueuePriority priority;

            internal DispatcherQueuePriorityAwaitable(DispatcherQueue dispatcher, DispatcherQueuePriority priority)
            {
                this.dispatcher = dispatcher;
                this.priority = priority;
            }

            /// <summary>
            /// Get awaiter of DispatcherPriorityAwaiter
            /// </summary>
            /// <returns>Awaiter of DispatcherPriorityAwaiter</returns>
            public DispatcherQueuePriorityAwaiter GetAwaiter()
            {
                return new DispatcherQueuePriorityAwaiter(this.dispatcher, this.priority);
            }
        }

        /// <summary>
        /// This struct represents the awaiter of a dispatcher.
        /// </summary>
        public struct DispatcherQueuePriorityAwaiter : INotifyCompletion
        {
            private readonly DispatcherQueue dispatcher;
            private readonly DispatcherQueuePriority priority;

            /// <summary>
            /// Gets a value indicating whether task has completed
            /// </summary>
            public bool IsCompleted => false;

            internal DispatcherQueuePriorityAwaiter(DispatcherQueue dispatcher, DispatcherQueuePriority priority)
            {
                this.dispatcher = dispatcher;
                this.priority = priority;
            }

            /// <summary>
            /// Get result for this awaiter
            /// </summary>
            public void GetResult()
            {
            }

            /// <summary>
            /// Fired once task has completed for notify completion
            /// </summary>
            /// <param name="continuation">Continuation action</param>
            public async void OnCompleted(Action continuation)
            {
                await this.dispatcher.EnqueueAsync(continuation, this.priority);
            }
        }

        /// <summary>
        /// Yield and allow UI update during tasks.
        /// </summary>
        /// <param name="dispatcher">Dispatcher of a thread to yield</param>
        /// <param name="priority">Dispatcher execution priority, default is low</param>
        /// <returns>Awaitable dispatcher task</returns>
        public static DispatcherQueuePriorityAwaitable YieldAsync(this DispatcherQueue dispatcher, DispatcherQueuePriority priority = DispatcherQueuePriority.Low)
        {
            return new DispatcherQueuePriorityAwaitable(dispatcher, priority);
        }
    }
}