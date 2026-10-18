// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Files.App.Server;

internal sealed partial class ServerSettings : IDisposable
{
	private readonly Lock gate = new();
	private readonly string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings", "user_settings.json");
	private readonly FileSystemWatcher watcher;
	private readonly Timer reloadTimer;
	private bool disposed;
	private int readFailures;

	internal bool KeepRunning { get; private set; } = true;
	internal bool ShowTrayIcon { get; private set; } = true;
	internal event Action? Changed;

	internal ServerSettings()
	{
		reloadTimer = new Timer(_ => Refresh(), null, Timeout.Infinite, Timeout.Infinite);
		watcher = new FileSystemWatcher(ApplicationData.Current.LocalFolder.Path, "user_settings.json")
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
		};
		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Deleted += OnChanged;
		watcher.Renamed += OnChanged;
		watcher.Error += (_, _) => ScheduleReload();
		watcher.EnableRaisingEvents = true;
		Refresh();
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (string.Equals(e.FullPath, path, StringComparison.OrdinalIgnoreCase))
			ScheduleReload();
	}

	private void ScheduleReload()
	{
		lock (gate)
		{
			if (!disposed)
			{
				readFailures = 0;
				reloadTimer.Change(200, Timeout.Infinite);
			}
		}
	}

	internal bool Refresh()
	{
		lock (gate)
		{
			if (disposed)
				return false;
			try
			{
				if (File.Exists(path))
				{
					var text = File.ReadAllText(path);
					using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "null" : text);
					var root = document.RootElement;
					KeepRunning = root.ValueKind is not JsonValueKind.Object || !root.TryGetProperty("CloseToBackground", out var keep) || keep.ValueKind is not JsonValueKind.False;
					ShowTrayIcon = root.ValueKind is not JsonValueKind.Object || !root.TryGetProperty("ShowSystemTrayIcon", out var show) || show.ValueKind is not JsonValueKind.False;
				}
				else
				{
					KeepRunning = true;
					ShowTrayIcon = true;
				}
				readFailures = 0;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
			{
				Program.Logger.LogDebug(ex, "Background settings are not ready to read.");
				if (++readFailures <= 3)
					reloadTimer.Change(200, Timeout.Infinite);
				return false;
			}
		}
		Changed?.Invoke();
		return true;
	}

	public void Dispose()
	{
		lock (gate)
		{
			disposed = true;
			watcher.Dispose();
			reloadTimer.Dispose();
		}
	}
}
