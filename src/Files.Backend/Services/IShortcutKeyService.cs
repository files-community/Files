using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IShortcutKeyService
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance can invoke shortcut keys.
        /// </summary>
        /// <remarks>
        /// Shortcut keys that are set to always invoke will ignore this property.
        /// </remarks>
        bool CanInvokeShortcutKeys { get; set; }

        /// <summary>
        /// Invokes the shortcut key combination.
        /// </summary>
        Task Invoke(bool control, bool shift, bool alt, bool tab, int key, out bool handled);

        /// <summary>
        /// Adds a shortcut key to the service.
        /// </summary>
        void Add(bool control, bool shift, bool alt, bool tab, int key, bool alwaysInvoke, Func<Task> action);

        /// <summary>
        /// Adds multiple shortcut keys for the single action.
        /// </summary>
        void Add(IEnumerable<Tuple<bool, bool, bool, bool, int, bool>> shortcutKeys, Func<Task> action);
    }
}
