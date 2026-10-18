// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.App.Server;

internal sealed partial class ServerSettings : IDisposable
{
	private readonly Lock gate = new();
	private readonly Timer reloadTimer;
	private bool disposed;
	private int readFailures;

	internal bool KeepRunning { get; private set; } = BackgroundSettings.CloseToBackgroundDefault;
	internal bool ShowTrayIcon { get; private set; } = BackgroundSettings.ShowSystemTrayIconDefault;
	internal event Action? Changed;

	internal ServerSettings()
	{
		reloadTimer = new Timer(_ => Refresh(), null, Timeout.Infinite, Timeout.Infinite);
		SettingsStore.Changed += OnChanged;
		Refresh();
	}

	private void OnChanged(string name)
	{
		if (name == "user_settings.json")
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
				var settings = SettingsStore.Get("user_settings.json");
				KeepRunning = settings.GetBoolean(BackgroundSettings.CloseToBackgroundKey, BackgroundSettings.CloseToBackgroundDefault);
				ShowTrayIcon = settings.GetBoolean(BackgroundSettings.ShowSystemTrayIconKey, BackgroundSettings.ShowSystemTrayIconDefault);
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
			SettingsStore.Changed -= OnChanged;
			reloadTimer.Dispose();
		}
	}
}
