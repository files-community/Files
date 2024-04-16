﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Files.Uwp.Serialization.Implementation
{
    internal sealed class CachingJsonSettingsDatabase : DefaultJsonSettingsDatabase
    {
        private Dictionary<string, object?>? _settingsCache;

        public CachingJsonSettingsDatabase(ISettingsSerializer settingsSerializer, IJsonSettingsSerializer jsonSettingsSerializer)
            : base(settingsSerializer, jsonSettingsSerializer)
        {
        }

        public override TValue? GetValue<TValue>(string key, TValue? defaultValue = default) where TValue : default
        {
            _settingsCache ??= GetFreshSettings();

            if (_settingsCache.TryGetValue(key, out var objVal))
            {
                return GetValueFromObject<TValue>(objVal) ?? defaultValue;
            }
            else
            {
                base.SetValue(key, defaultValue);
                return defaultValue;
            }
        }

        public override bool SetValue<TValue>(string key, TValue? newValue) where TValue : default
        {
            _settingsCache ??= GetFreshSettings();

            if (!_settingsCache.ContainsKey(key))
            {
                _settingsCache.Add(key, newValue);
                return SaveSettings(_settingsCache);
            }
            else
            {
                return UpdateValueInCache(_settingsCache[key]);
            }

            bool UpdateValueInCache(object? value)
            {
                bool isDifferent;
                if (newValue is IEnumerable enumerableNewValue && value is IEnumerable enumerableValue)
                {
                    isDifferent = !enumerableValue.Cast<object>().SequenceEqual(enumerableNewValue.Cast<object>());
                }
                else
                {
                    isDifferent = value != (object?)newValue;
                }

                if (isDifferent)
                {
                    // Values are different, update the value and reload the cache.
                    _settingsCache[key] = newValue;
                    return SaveSettings(_settingsCache);
                }
                else
                {
                    // The cache does not need to be updated, continue.
                    return false;
                }
            }
        }

        public override bool RemoveKey(string key)
        {
            _settingsCache ??= GetFreshSettings();

            if (_settingsCache.Remove(key))
            {
                return SaveSettings(_settingsCache);
            }

            return false;
        }

        public override bool ImportSettings(object? import)
        {
            if (base.ImportSettings(import))
            {
                _settingsCache = GetFreshSettings();
                return true;
            }
            return false;
        }
    }
}
