// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Files.App.Server;

internal sealed class SettingsStore
{
	private static readonly SettingsStore user = new("user_settings.json");
	private static readonly SettingsStore tags = new("filetags.json");
	private readonly object gate = new();
	private readonly string fileName;
	private readonly string path;
	private Dictionary<string, JsonElement>? values;
	private bool invalidated;
	private DateTime lastWrite;
	internal static event Action<string>? Changed;
	private long length;
	private readonly FileSystemWatcher watcher;
	private readonly Dictionary<string, SettingChange> pendingWrites = new(StringComparer.Ordinal);
	private long? replacementVersion;
	private bool writing;
	private long version;
	private long persistedVersion;
	private long writeFailures;

	// Null Data removes a key; a JSON null remains a stored value.
	private readonly record struct SettingChange(long Version, JsonElement? Data);

	private SettingsStore(string fileName)
	{
		this.fileName = fileName;
		var directory = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");
		Directory.CreateDirectory(directory);
		path = Path.Combine(directory, fileName);
		watcher = new FileSystemWatcher(directory, fileName)
		{
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
		};
		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Deleted += OnChanged;
		watcher.Renamed += OnChanged;
		watcher.Error += (_, _) => Invalidate();
		watcher.EnableRaisingEvents = true;
	}

	internal static SettingsStore Get(string name) => name switch
	{
		"user_settings.json" => user,
		"filetags.json" => tags,
		_ => throw new ArgumentException("Unknown settings store.", nameof(name)),
	};

	private void OnChanged(object sender, FileSystemEventArgs e) => Invalidate();

	private void Invalidate()
	{
		lock (gate)
			invalidated = true;
		Changed?.Invoke(fileName);
		ServerEvents.Notify(ServerChangeNames.Settings(fileName));
	}

	private Dictionary<string, JsonElement> Read()
	{
		var file = new FileInfo(path);
		if (!invalidated && values is not null && file.LastWriteTimeUtc == lastWrite && (file.Exists ? file.Length : 0) == length)
			return values;
		var result = ReadFile(values ?? []);
		ApplyChanges(result, pendingWrites, replacementVersion.HasValue);
		values = result;
		invalidated = false;
		lastWrite = file.LastWriteTimeUtc;
		length = file.Exists ? file.Length : 0;
		return result;
	}

	private Dictionary<string, JsonElement> ReadFile(Dictionary<string, JsonElement> fallback)
	{
		var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
		if (!File.Exists(path))
			return result;
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		if (stream.Length == 0)
			return result;
		try
		{
			using var document = JsonDocument.Parse(stream);
			if (document.RootElement.ValueKind == JsonValueKind.Object)
				foreach (var property in document.RootElement.EnumerateObject())
					result[property.Name] = property.Value.Clone();
			return result;
		}
		catch (JsonException ex)
		{
			Program.Logger.LogWarning(ex, "Could not read settings.");
			return new(fallback, StringComparer.Ordinal);
		}
	}

	internal bool GetBoolean(string key, bool fallback)
	{
		lock (gate)
		{
			return Read().TryGetValue(key, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
				? value.GetBoolean() : fallback;
		}
	}

	internal static void Stop()
	{
		user.watcher.Dispose();
		tags.watcher.Dispose();
		if (!FlushAll())
			Program.Logger.LogError("Could not flush all settings before server shutdown.");
	}

	internal static bool FlushAll()
	{
		var userFlushed = user.Flush();
		var tagsFlushed = tags.Flush();
		return userFlushed && tagsFlushed;
	}

	internal SettingsValue[] ReadValues()
	{
		lock (gate)
			return SettingsValueCodec.EncodeObject(Read());
	}

	internal bool Set(string key, SettingsValue[] value, bool remove)
	{
		var element = remove ? default : SettingsValueCodec.Decode(value);
		bool changed;
		lock (gate)
		{
			var data = Read();
			changed = remove ? data.Remove(key) : !data.TryGetValue(key, out var old) || !JsonElement.DeepEquals(old, element);
			if (changed)
			{
				if (!remove)
					data[key] = element;
				pendingWrites[key] = new(++version, remove ? null : element);
			}
			StartWriter();
		}
		if (changed)
		{
			Changed?.Invoke(fileName);
			ServerEvents.Notify(ServerChangeNames.Settings(fileName));
		}
		return changed;
	}

	internal bool Import(SettingsValue[] properties)
	{
		var replacement = SettingsValueCodec.DecodeObject(properties);
		lock (gate)
		{
			values = replacement;
			replacementVersion = ++version;
			pendingWrites.Clear();
			foreach (var pair in replacement)
				pendingWrites[pair.Key] = new(version, pair.Value);
			StartWriter();
		}
		Changed?.Invoke(fileName);
		ServerEvents.Notify(ServerChangeNames.Settings(fileName));
		return true;
	}

	private static void ApplyChanges(Dictionary<string, JsonElement> data, IEnumerable<KeyValuePair<string, SettingChange>> changes, bool replace)
	{
		if (replace)
			data.Clear();
		foreach (var change in changes)
		{
			if (change.Value.Data is { } value)
				data[change.Key] = value;
			else
				data.Remove(change.Key);
		}
	}

	private void StartWriter()
	{
		if (writing || (pendingWrites.Count == 0 && !replacementVersion.HasValue))
			return;
		writing = true;
		_ = Task.Run(PersistPendingChanges);
	}

	private void PersistPendingChanges()
	{
		try
		{
			while (true)
			{
				KeyValuePair<string, SettingChange>[] batch;
				bool replace;
				Dictionary<string, JsonElement> fallback;
				long batchVersion;
				lock (gate)
				{
					if (pendingWrites.Count == 0 && !replacementVersion.HasValue)
					{
						writing = false;
						Monitor.PulseAll(gate);
						return;
					}
					batch = pendingWrites.ToArray();
					replace = replacementVersion.HasValue;
					fallback = new(values!);
					batchVersion = version;
				}

				// Merge against disk under the shared lease so elevated servers cannot lose each other's writes.
				using var lease = FileWriteLease.Acquire(path);
				var data = ReadFile(fallback);
				ApplyChanges(data, batch, replace);
				WriteFile(data);
				var file = new FileInfo(path);
				var lastWriteTime = file.LastWriteTimeUtc;
				var fileLength = file.Length;
				lock (gate)
				{
					foreach (var key in pendingWrites.Where(pair => pair.Value.Version <= batchVersion).Select(pair => pair.Key).ToArray())
						pendingWrites.Remove(key);
					if (replacementVersion is { } replaceVersion && replaceVersion <= batchVersion)
						replacementVersion = null;
					values = data;
					ApplyChanges(values, pendingWrites, replacementVersion.HasValue);
					persistedVersion = batchVersion;
					lastWrite = lastWriteTime;
					length = fileLength;
					invalidated = false;
					Monitor.PulseAll(gate);
				}
			}
		}
		catch (Exception ex)
		{
			lock (gate)
			{
				writing = false;
				writeFailures++;
				Monitor.PulseAll(gate);
			}
			Program.Logger.LogError(ex, "Could not persist settings. Pending changes are retained for the next write or flush.");
		}
	}

	internal bool Flush()
	{
		lock (gate)
		{
			var requestedVersion = version;
			var failures = writeFailures;
			StartWriter();
			while (persistedVersion < requestedVersion && writeFailures == failures && writing)
				Monitor.Wait(gate);
			return persistedVersion >= requestedVersion;
		}
	}

	private void WriteFile(Dictionary<string, JsonElement> data)
	{
		var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
				writer.WriteStartObject();
				foreach (var pair in data)
				{
					writer.WritePropertyName(pair.Key);
					pair.Value.WriteTo(writer);
				}
				writer.WriteEndObject();
				writer.Flush();
				stream.Flush(true);
			}
			File.Move(temporary, path, true);
		}
		finally
		{
			if (File.Exists(temporary))
				File.Delete(temporary);
		}
	}
}
