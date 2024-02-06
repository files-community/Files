// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Files.App.Utils.Serialization
{
	internal class DefaultJsonSettingsDatabase : IJsonSettingsDatabase
	{
		public static readonly JsonSerializerOptions jsonSerializerOptions = new()
		{
			WriteIndented = true
		};

		protected ISettingsSerializer SettingsSerializer { get; }

		public DefaultJsonSettingsDatabase(ISettingsSerializer settingsSerializer)
		{
			SettingsSerializer = settingsSerializer;
		}

		protected IDictionary<string, object?>? GetFreshSettings()
		{
			string data = SettingsSerializer.ReadFromFile();

			if (string.IsNullOrWhiteSpace(data))
				data = "null";

			try
			{
				return JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>?>(data) ?? new();
			}
			catch (Exception ex)
			{
				// Show a dialog to notify
				if (App.AppModel.ShouldBrokenJsonBeRefreshed)
				{
					return JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>?>("null") ?? new();
				}
				else
				{
					App.AppModel.RaiseReloadJsonSettingsFailedEvent(ex);

					return null;
				}
			}
		}

		protected bool SaveSettings(IDictionary<string, object?> data)
		{
			var jsonData = JsonSerializer.Serialize(data, jsonSerializerOptions);

			return SettingsSerializer.WriteToFile(jsonData);
		}

		public virtual TValue? GetValue<TValue>(string key, TValue? defaultValue = default)
		{
			var data = GetFreshSettings();
			if (data is null)
				return defaultValue;

			if (data.TryGetValue(key, out var objVal))
			{
				return GetValueFromObject<TValue>(objVal) ?? defaultValue;
			}
			else
			{
				SetValue(key, defaultValue);
				return defaultValue;
			}
		}

		public virtual bool SetValue<TValue>(string key, TValue? newValue)
		{
			var data = GetFreshSettings();
			if (data is null)
				return false;

			if (!data.TryAdd(key, newValue))
				data[key] = newValue;

			return SaveSettings(data);
		}

		public virtual bool RemoveKey(string key)
		{
			var data = GetFreshSettings();

			return data.Remove(key) && SaveSettings(data);
		}

		public bool FlushSettings()
		{
			// The settings are always flushed automatically, return true.
			return true;
		}

		public virtual bool ImportSettings(object? import)
		{
			try
			{
				// Try convert
				var data = (IDictionary<string, object?>?)import;
				if (data is null)
				{
					return false;
				}

				// Serialize
				var serialized = JsonSerializer.Serialize(data, jsonSerializerOptions);

				// Write to file
				return SettingsSerializer.WriteToFile(serialized);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				Debugger.Break();

				return false;
			}
		}

		public object? ExportSettings()
		{
			return GetFreshSettings();
		}

		protected static TValue? GetValueFromObject<TValue>(object? obj)
		{
			if (obj is JsonElement jElem)
			{
				return jElem.Deserialize<TValue>();
			}

			return (TValue?)obj;
		}
	}
}
