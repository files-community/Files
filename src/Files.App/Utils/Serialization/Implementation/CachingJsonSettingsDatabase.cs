// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class CachingJsonSettingsDatabase : DefaultJsonSettingsDatabase
	{
		private ConcurrentDictionary<string, JsonElement>? _settingsCache;

		public CachingJsonSettingsDatabase(
			ISettingsSerializer settingsSerializer,
			IJsonSettingsSerializer jsonSettingsSerializer,
			JsonSerializerContext jsonSerializerContext)
			: base(settingsSerializer, jsonSettingsSerializer, jsonSerializerContext)
		{
		}

		public override TValue? GetValue<TValue>(string key, TValue? defaultValue = default) where TValue : default
		{
			_settingsCache ??= GetFreshSettings();

			if (_settingsCache.TryGetValue(key, out var objVal))
			{
				return GetValueFromElement<TValue>(objVal) ?? defaultValue;
			}
			else
			{
				var defaultElement = GetElementFromValue(defaultValue);
				if (_settingsCache.TryAdd(key, defaultElement) && !SaveSettings(_settingsCache))
					_settingsCache.TryRemove(key, out _);

				return defaultValue;
			}
		}

		public override bool SetValue<TValue>(string key, TValue? newValue) where TValue : default
		{
			_settingsCache ??= GetFreshSettings();
			var newElement = GetElementFromValue(newValue);

			if (_settingsCache.TryAdd(key, newElement))
				return SaveSettings(_settingsCache);

			if (JsonElement.DeepEquals(_settingsCache[key], newElement))
				return false;

			_settingsCache[key] = newElement;
			return SaveSettings(_settingsCache);
		}

		public override bool RemoveKey(string key)
		{
			_settingsCache ??= GetFreshSettings();

			return _settingsCache.TryRemove(key, out _) && SaveSettings(_settingsCache);
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
