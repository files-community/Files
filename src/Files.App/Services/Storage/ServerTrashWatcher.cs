// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage;
using System.IO;

namespace Files.App.Services;

internal sealed partial class ServerTrashWatcher : ITrashWatcher
{
	private readonly SemaphoreSlim changesGate = new(1, 1);
	internal event EventHandler? BatchReceived;
	public event EventHandler<FileSystemEventArgs>? RefreshRequested;
	public event EventHandler<FileSystemEventArgs>? ItemAdded;
	public event EventHandler<FileSystemEventArgs>? ItemDeleted;
	public event EventHandler<FileSystemEventArgs>? ItemChanged;
	public event EventHandler<FileSystemEventArgs>? ItemRenamed;

	internal ServerTrashWatcher() => StartWatcher();

	public void StartWatcher()
	{
		App.ServerChanged -= OnServerChanged;
		App.ServerChanged += OnServerChanged;
	}

	public void StopWatcher() => App.ServerChanged -= OnServerChanged;
	public void Dispose() => StopWatcher();

	private async void OnServerChanged(object? sender, string name)
	{
		if (name != ServerChangeNames.RecycleBin)
			return;
		await SafetyExtensions.IgnoreExceptions(async () =>
		{
			var server = await App.ServerSession.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			await changesGate.WaitAsync().ConfigureAwait(false);
			try
			{
				var changes = server.RecycleBin.GetChanges();
				foreach (var change in changes)
				{
					var type = change.Kind switch
					{
						Server.RecycleBinChangeKind.Created => WatcherChangeTypes.Created,
						Server.RecycleBinChangeKind.Deleted => WatcherChangeTypes.Deleted,
						Server.RecycleBinChangeKind.Changed => WatcherChangeTypes.Changed,
						Server.RecycleBinChangeKind.Renamed => WatcherChangeTypes.Renamed,
						_ => WatcherChangeTypes.All,
					};
					FileSystemEventArgs args = type == WatcherChangeTypes.Renamed
						? new RenamedEventArgs(type, change.Directory, change.Name, change.OldName)
						: new FileSystemEventArgs(type, change.Directory, change.Name);
					var handler = type switch
					{
						WatcherChangeTypes.Created => ItemAdded,
						WatcherChangeTypes.Deleted => ItemDeleted,
						WatcherChangeTypes.Changed => ItemChanged,
						WatcherChangeTypes.Renamed => ItemRenamed,
						_ => RefreshRequested,
					};
					handler?.Invoke(this, args);
				}
				BatchReceived?.Invoke(this, EventArgs.Empty);
			}
			finally
			{
				changesGate.Release();
			}
		}, App.Logger);
	}
}
