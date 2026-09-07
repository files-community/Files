// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Files.App.Utils.Serialization.Implementation
{
	internal class DefaultJsonSettingsDatabase : IJsonSettingsDatabase
	{
		protected ISettingsSerializer SettingsSerializer { get; }

		protected IJsonSettingsSerializer JsonSettingsSerializer { get; }

		private JsonSerializerContext JsonSerializerContext { get; }

		private JsonTypeInfo<ConcurrentDictionary<string, JsonElement>?> SettingsTypeInfo { get; }

		public DefaultJsonSettingsDatabase(
			ISettingsSerializer settingsSerializer,
			IJsonSettingsSerializer jsonSettingsSerializer,
			JsonSerializerContext jsonSerializerContext)
		{
			SettingsSerializer = settingsSerializer;
			JsonSettingsSerializer = jsonSettingsSerializer;
			JsonSerializerContext = jsonSerializerContext;
			SettingsTypeInfo = GetRequiredTypeInfo<ConcurrentDictionary<string, JsonElement>>();
		}

		protected ConcurrentDictionary<string, JsonElement> GetFreshSettings()
		{
			string data = SettingsSerializer.ReadFromFile();

			if (string.IsNullOrWhiteSpace(data))
			{
				data = "null";
			}

			try
			{
				return JsonSettingsSerializer.DeserializeFromJson(data, SettingsTypeInfo) ?? [];
			}
			catch (Exception)
			{
				// Occurs if the settings file has invalid json
				// TODO Display prompt to notify user #710
				return JsonSettingsSerializer.DeserializeFromJson("null", SettingsTypeInfo) ?? [];
			}
		}

		protected bool SaveSettings(ConcurrentDictionary<string, JsonElement> data)
		{
			var jsonData = JsonSettingsSerializer.SerializeToJson(data, SettingsTypeInfo);

			return SettingsSerializer.WriteToFile(jsonData);
		}

		public virtual TValue? GetValue<TValue>(string key, TValue? defaultValue = default)
		{
			var data = GetFreshSettings();

			if (data.TryGetValue(key, out var objVal))
			{
				return GetValueFromElement<TValue>(objVal) ?? defaultValue;
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
			var newElement = GetElementFromValue(newValue);

			if (!data.TryAdd(key, newElement))
				data[key] = newElement;

			return SaveSettings(data);
		}

		public virtual bool RemoveKey(string key)
		{
			var data = GetFreshSettings();

			return data.TryRemove(key, out _) && SaveSettings(data);
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
				var data = import switch
				{
					IDictionary<string, JsonElement> jsonElements => new ConcurrentDictionary<string, JsonElement>(jsonElements),
					IDictionary<string, object?> objects => new ConcurrentDictionary<string, JsonElement>(
						objects.Select(x => new KeyValuePair<string, JsonElement>(x.Key, GetElementFromObject(x.Value)))),
					_ => null,
				};

				return data is not null && SaveSettings(data);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				Debugger.Break();

				return false;
			}
		}

		public IDictionary<string, JsonElement> ExportSettings()
		{
			return GetFreshSettings();
		}

		protected JsonElement GetElementFromValue<TValue>(TValue? value)
		{
			return JsonSettingsSerializer.SerializeToElement(value, GetRequiredTypeInfo<TValue>());
		}

		protected TValue? GetValueFromElement<TValue>(JsonElement element)
		{
			try
			{
				return JsonSettingsSerializer.DeserializeFromElement(element, GetRequiredTypeInfo<TValue>());
			}
			catch (JsonException)
			{
				// Deserialization failed (e.g., incompatible type in settings file)
				// Return null to fall back to the default value
				return default;
			}
		}

		private JsonElement GetElementFromObject(object? value)
		{
			return JsonSettingsSerializer.SerializeToElement<object>(value, GetRequiredTypeInfo<object>());
		}

		private JsonTypeInfo<TValue?> GetRequiredTypeInfo<TValue>()
		{
			return JsonSerializerContext.GetTypeInfo(typeof(TValue)) as JsonTypeInfo<TValue?>
				?? throw new InvalidOperationException($"JSON serialization metadata is missing for {typeof(TValue)}.");
		}
	}
}
