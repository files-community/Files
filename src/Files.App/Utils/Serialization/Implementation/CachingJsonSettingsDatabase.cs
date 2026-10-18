// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Server;
using System.Text.Json.Serialization.Metadata;

namespace Files.App.Utils.Serialization.Implementation;

internal sealed class CachingJsonSettingsDatabase : IJsonSettingsDatabase
{
	private readonly string fileName;
	private readonly IJsonSettingsSerializer serializer;
	private readonly JsonSerializerContext context;
	private readonly Dictionary<string, JsonElement> defaults = [];
	private readonly Lock gate = new();
	private Dictionary<string, JsonElement>? cache;
	internal event Action<string, JsonElement>? Changed;

	public CachingJsonSettingsDatabase(string fileName, IJsonSettingsSerializer serializer, JsonSerializerContext context)
	{
		this.fileName = fileName;
		this.serializer = serializer;
		this.context = context;
		App.ServerChanged += OnServerChanged;
	}

	private Dictionary<string, JsonElement> ReadValues() => SettingsValueCodec.DecodeObject(Server.AppService.GetSettings(fileName));

	public TValue? GetValue<TValue>(string key, TValue? defaultValue = default)
	{
		lock (gate)
		{
			cache ??= ReadValues();
			if (cache.TryGetValue(key, out var value))
				return GetValueFromElement<TValue>(value) ?? defaultValue;
			if (defaults.TryGetValue(key, out var fallback))
				return GetValueFromElement<TValue>(fallback) ?? defaultValue;
			defaults[key] = GetElementFromValue(defaultValue);
			return defaultValue;
		}
	}

	public bool SetValue<TValue>(string key, TValue? value)
	{
		var element = GetElementFromValue(value);
		lock (gate)
		{
			var changed = Server.AppService.SetSetting(fileName, key, SettingsValueCodec.Encode(element));
			cache ??= ReadValues();
			var localChanged = !cache.TryGetValue(key, out var old) || !JsonElement.DeepEquals(old, element);
			cache[key] = element;
			defaults.Remove(key);
			return changed || localChanged;
		}
	}

	public bool RemoveKey(string key)
	{
		var changed = Server.AppService.RemoveSetting(fileName, key);
		lock (gate)
			cache?.Remove(key);
		return changed;
	}

	public bool ImportSettings(object? import)
	{
		var values = import switch
		{
			IDictionary<string, JsonElement> elements => elements,
			IDictionary<string, object?> objects => objects.ToDictionary(pair => pair.Key, pair => GetElementFromValue(pair.Value)),
			_ => null,
		};
		if (values is null)
			return false;
		var changed = Server.AppService.ImportSettings(fileName, SettingsValueCodec.EncodeObject(values));
		lock (gate)
			cache = null;
		return changed;
	}

	public IDictionary<string, JsonElement> ExportSettings() => ReadValues();

	public bool FlushSettings() => true;

	private JsonTypeInfo<T?> GetTypeInfo<T>() => context.GetTypeInfo(typeof(T)) as JsonTypeInfo<T?>
		?? throw new InvalidOperationException($"JSON serialization metadata is missing for {typeof(T)}.");

	private JsonElement GetElementFromValue<T>(T? value) => serializer.SerializeToElement(value, GetTypeInfo<T>());

	private T? GetValueFromElement<T>(JsonElement element)
	{
		try
		{
			return serializer.DeserializeFromElement(element, GetTypeInfo<T>());
		}
		catch (JsonException)
		{
			return default;
		}
	}

	private async void OnServerChanged(object? sender, string name)
	{
		if (name != $"Settings:{fileName}")
			return;
		await SafetyExtensions.IgnoreExceptions(async () =>
		{
			var differences = await Task.Run(() =>
			{
				lock (gate)
				{
					var fresh = ReadValues();
					var previous = cache;
					cache = fresh;
					var changes = fresh.Where(pair => previous is null || !previous.TryGetValue(pair.Key, out var old) || !JsonElement.DeepEquals(old, pair.Value)).ToList();
					if (previous is not null)
						changes.AddRange(previous.Keys.Except(fresh.Keys).Select(key => new KeyValuePair<string, JsonElement>(key, default)));
					return changes.ToArray();
				}
			});
			foreach (var pair in differences)
				Changed?.Invoke(pair.Key, pair.Value);
		}, App.Logger);
	}
}
