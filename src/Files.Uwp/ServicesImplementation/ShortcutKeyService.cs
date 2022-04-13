using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Services;

namespace Files.Uwp.ServicesImplementation
{
    public class ShortcutKeyService : ObservableObject, IShortcutKeyService
    {
        private readonly Dictionary<ShortcutKey, Func<Task>> _shortcutKeys = new();
        private bool _canInvokeShortcutKeys = true;

        public bool CanInvokeShortcutKeys
        {
            get => _canInvokeShortcutKeys;
            set => SetProperty(ref _canInvokeShortcutKeys, value);
        }

        public Task Invoke(bool control, bool shift, bool alt, bool tab, int key, out bool handled)
        {
            // Create equality check.
            var incomingKey = new ShortcutKey(control, shift, alt, tab, key);

            if (!_shortcutKeys.ContainsKey(incomingKey))
            {
                // Shortcut key not found, returning.
                handled = false;
                return Task.CompletedTask;
            }

            (ShortcutKey shortcutKey, var shortcutAction) = _shortcutKeys.FirstOrDefault(x => x.Key.Equals(incomingKey));

            // Check if shortcut keys can currently be invoked,
            // or if this particular shortcut key can always be invoked.
            if (CanInvokeShortcutKeys || shortcutKey.CanAlwaysInvoke)
            {
                shortcutAction.Invoke();
                handled = true;
            }

            handled = false;
            return Task.CompletedTask;
        }

        public void Add(bool control, bool shift, bool alt, bool tab, int key, bool alwaysInvoke, Func<Task> action)
        {
            var keyToAdd = new ShortcutKey(control, shift, alt, tab, key, alwaysInvoke);

            if (_shortcutKeys.ContainsKey(keyToAdd))
            {
                //TODO: Use ILogger service when implemented.
                App.Logger.Warn($"Shortcut key already exists, t:{tab} c:{control} a:{alt} s:{shift} key:{key}");
            }

            _shortcutKeys.Add(keyToAdd, action);

        }

        public void Add(IEnumerable<Tuple<bool, bool, bool, bool, int, bool>> shortcutKeys, Func<Task> action)
        {
            foreach (var t in shortcutKeys)
            {
                Add(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, action);
            }
        }
    }

    public sealed class ShortcutKey : IEquatable<ShortcutKey>
    {
        private readonly bool _tab;
        private readonly bool _control;
        private readonly bool _alt;
        private readonly bool _shift;
        private readonly int _key;

        public bool CanAlwaysInvoke { get; }

        public ShortcutKey(bool control, bool shift, bool alt, bool tab, int key, bool alwaysInvoke = false)
        {
            _control = control;
            _shift = shift;
            _alt = alt;
            _tab = tab;
            CanAlwaysInvoke = alwaysInvoke;
            _key = key;
        }

        public bool Equals(ShortcutKey other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return _tab == other._tab &&
                   _control == other._control &&
                   _alt == other._alt &&
                   _shift == other._shift &&
                   _key == other._key;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ShortcutKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _tab.GetHashCode();
                hashCode = (hashCode * 397) ^ _control.GetHashCode();
                hashCode = (hashCode * 397) ^ _alt.GetHashCode();
                hashCode = (hashCode * 397) ^ _shift.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)_key;
                return hashCode;
            }
        }
    }
}
