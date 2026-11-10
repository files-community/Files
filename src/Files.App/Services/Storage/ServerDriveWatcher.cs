// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services;

public sealed class ServerDriveWatcher : IStorageDeviceWatcher
{
	public event EventHandler? Changed;
	private bool started;
	public bool CanBeStarted => !started;

	public void Start()
	{
		if (started)
			return;
		started = true;
		App.ServerChanged += OnServerChanged;
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private void OnServerChanged(object? sender, string name)
	{
		if (name == ServerChangeNames.Drives)
			Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Stop()
	{
		started = false;
		App.ServerChanged -= OnServerChanged;
	}
}
