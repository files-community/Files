// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Contracts;
using System.IO;

namespace Files.App.Services;

internal sealed partial class ServerTrashWatcher : ITrashWatcher
{
	public event EventHandler<FileSystemEventArgs>? RefreshRequested;
	public event EventHandler<FileSystemEventArgs>? ItemAdded { add { } remove { } }
	public event EventHandler<FileSystemEventArgs>? ItemDeleted { add { } remove { } }
	public event EventHandler<FileSystemEventArgs>? ItemChanged { add { } remove { } }
	public event EventHandler<FileSystemEventArgs>? ItemRenamed { add { } remove { } }

	internal ServerTrashWatcher() => StartWatcher();

	public void StartWatcher()
	{
		App.ServerChanged -= OnServerChanged;
		App.ServerChanged += OnServerChanged;
	}

	public void StopWatcher() => App.ServerChanged -= OnServerChanged;
	public void Dispose() => StopWatcher();

	private void OnServerChanged(object? sender, string name)
	{
		if (name == ServerChangeNames.RecycleBin)
			RefreshRequested?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.All, string.Empty, string.Empty));
	}
}
