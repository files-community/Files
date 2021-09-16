﻿using Files.Common;
using System.Collections;
using System.Linq;

namespace Files.Models.JsonSettings.Implementation
{
    public sealed class CachingJsonSettingsDatabase : DefaultJsonSettingsDatabase
    {
        private int _cacheMisses = 0;

        public CachingJsonSettingsDatabase(IJsonSettingsSerializer jsonSettingsSerializer, ISettingsSerializer settingsSerializer)
            : base(jsonSettingsSerializer, settingsSerializer)
        {
        }

        public override object GetValue(string key, object defaultValue = null)
        {
            if (settingsCache.ContainsKey(key))
            {
                return settingsCache[key];
            }
            else
            {
                _cacheMisses++;
                return base.GetValue(key, defaultValue);
            }
        }

        public override bool AddKey(string key, object value)
        {
            if (settingsCache.ContainsKey(key))
            {
                return false;
            }

            _cacheMisses++;
            return base.AddKey(key, value);
        }

        public override bool RemoveKey(string key)
        {
            if (!settingsCache.ContainsKey(key))
            {
                return false;
            }

            _cacheMisses++;
            return base.RemoveKey(key);
        }

        public override bool UpdateKey(string key, object newValue)
        {
            if (!settingsCache.ContainsKey(key))
            {
                // Doesn't contain setting, add it
                return this.AddKey(key, newValue);
            }
            else
            {
                object value = settingsCache[key];

                bool isDifferent;
                if (newValue is IEnumerable enumerableNewValue && value is IEnumerable enumerableValue)
                {
                    isDifferent = enumerableValue.Cast<object>().SequenceEqual(enumerableNewValue.Cast<object>());
                }
                else
                {
                    isDifferent = value != newValue;
                }

                if (isDifferent)
                {
                    // Values are different, update value and reload the cache
                    _cacheMisses++;
                    return base.UpdateKey(key, newValue);
                }
                else
                {
                    // The cache does not need to be updated, continue
                    return false;
                }
            }
        }
    }
}
