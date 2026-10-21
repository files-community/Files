// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Files.App.Server;

internal sealed class SettingsStore
{
	private static readonly SettingsStore user = new("user_settings.json");
	private static readonly SettingsStore tags = new("filetags.json");
	private readonly Lock gate = new();
	private readonly string fileName;
	private readonly string path;
	private Dictionary<string, JsonElement>? values;
	private bool invalidated;
	private DateTime lastWrite;
	internal static event Action<string>? Changed;
	private long length;
	private readonly FileSystemWatcher watcher;

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

	private Dictionary<string, JsonElement> Read(bool fresh = false)
	{
		var file = new FileInfo(path);
		if (!fresh && !invalidated && values is not null && file.LastWriteTimeUtc == lastWrite && (file.Exists ? file.Length : 0) == length)
			return values;
		var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
		if (file.Exists)
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			if (stream.Length > 0)
			{
				try
				{
					using var document = JsonDocument.Parse(stream);
					if (document.RootElement.ValueKind == JsonValueKind.Object)
						foreach (var property in document.RootElement.EnumerateObject())
							result[property.Name] = property.Value.Clone();
				}
				catch (JsonException ex) when (!fresh)
				{
					Program.Logger.LogWarning(ex, "Could not read settings.");
					return values ?? result;
				}
			}
		}
		values = result;
		invalidated = false;
		lastWrite = file.LastWriteTimeUtc;
		length = file.Exists ? file.Length : 0;
		return result;
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
	}

	internal SettingsValue[] ReadValues()
	{
		lock (gate)
			return SettingsValueCodec.EncodeObject(Read());
	}

	internal bool Set(string key, SettingsValue[] value, bool remove)
	{
		var element = remove ? default : SettingsValueCodec.Decode(value);
		return Mutate(data =>
		{
			if (remove)
				return data.Remove(key);
			if (data.TryGetValue(key, out var old) && JsonElement.DeepEquals(old, element))
				return false;
			data[key] = element;
			return true;
		});
	}

	internal bool Import(SettingsValue[] properties)
	{
		var replacement = SettingsValueCodec.DecodeObject(properties);
		return Mutate(data =>
		{
			data.Clear();
			foreach (var pair in replacement)
				data.Add(pair.Key, pair.Value);
			return true;
		});
	}

	private bool Mutate(Func<Dictionary<string, JsonElement>, bool> mutate)
	{
		lock (gate)
		{
			// Elevated and unelevated servers for this user share the same persisted settings.
			using var lease = AcquireWriteLease();
			var data = new Dictionary<string, JsonElement>(Read(fresh: true));
			if (!mutate(data))
				return false;
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
				values = data;
				lastWrite = File.GetLastWriteTimeUtc(path);
				length = new FileInfo(path).Length;
			}
			finally
			{
				if (File.Exists(temporary))
					File.Delete(temporary);
			}
		}
		Changed?.Invoke(fileName);
		ServerEvents.Notify(ServerChangeNames.Settings(fileName));
		return true;
	}

	private FileStream AcquireWriteLease() => AcquireWriteLease(path);

	internal static FileStream AcquireWriteLease(string path)
	{
		var expires = Environment.TickCount64 + 5000;
		while (true)
		{
			try
			{
				return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException) when (Environment.TickCount64 < expires)
			{
				Thread.Sleep(20);
			}
		}
	}
}
