// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils;

public sealed class WindowsStorageDeviceWatcher : IStorageDeviceWatcher
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
		if (name == "Drives")
			Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Stop()
	{
		started = false;
		App.ServerChanged -= OnServerChanged;
	}
}
