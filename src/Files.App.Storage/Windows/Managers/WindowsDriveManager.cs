// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe sealed class WindowsDriveManager : IDisposable
	{
		private readonly WindowsFolderChangeWatcher _folderChangeWatcher;
		private bool _isDisposed;

		public event EventHandler<DeviceEventArgs>? DeviceAdded;
		public event EventHandler<DeviceEventArgs>? DeviceRemoved;
		public event EventHandler<DeviceEventArgs>? DeviceInserted;
		public event EventHandler<DeviceEventArgs>? DeviceEjected;

		public static WindowsDriveManager Default { get; } = new();

		private WindowsDriveManager()
		{
			Guid computerFolderId = FOLDERID.FOLDERID_ComputerFolder;
			_folderChangeWatcher = new(
				computerFolderId,
				SHCNE_ID.SHCNE_DRIVEADD | SHCNE_ID.SHCNE_DRIVEREMOVED | SHCNE_ID.SHCNE_MEDIAINSERTED | SHCNE_ID.SHCNE_MEDIAREMOVED,
				recursive: true);
			_folderChangeWatcher.FolderChanged += FolderChangeWatcher_FolderChanged;
		}

		public void Start()
		{
			ObjectDisposedException.ThrowIf(_isDisposed, this);

			_folderChangeWatcher.Start();
		}

		public void Stop()
		{
			_folderChangeWatcher.Stop();
		}

		private static string? NormalizeDriveId(string drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return null;

			string root = SystemIO.Path.GetPathRoot(drivePath) ?? drivePath;
			return root.TrimEnd(SystemIO.Path.DirectorySeparatorChar, SystemIO.Path.AltDirectorySeparatorChar);
		}

		private void FolderChangeWatcher_FolderChanged(object? sender, WindowsFolderChangeEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Path))
				return;

			string? driveId = NormalizeDriveId(e.Path);
			if (string.IsNullOrEmpty(driveId))
				return;

			RaiseDriveEvent(e.ChangeType, driveId);
		}

		private void RaiseDriveEvent(SHCNE_ID changeType, string driveId)
		{
			DeviceEventArgs args = new(driveId, driveId);

			if ((changeType & SHCNE_ID.SHCNE_DRIVEADD) != 0)
				DeviceAdded?.Invoke(this, args);

			if ((changeType & SHCNE_ID.SHCNE_DRIVEREMOVED) != 0)
				DeviceRemoved?.Invoke(this, args);

			if ((changeType & SHCNE_ID.SHCNE_MEDIAINSERTED) != 0)
				DeviceInserted?.Invoke(this, args);

			if ((changeType & SHCNE_ID.SHCNE_MEDIAREMOVED) != 0)
				DeviceEjected?.Invoke(this, args);
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			_folderChangeWatcher.FolderChanged -= FolderChangeWatcher_FolderChanged;
			_folderChangeWatcher.Dispose();
			_isDisposed = true;
		}
	}
}
