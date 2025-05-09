// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Storage
{
	public interface IWindowsFolderWatcher : IFolderWatcher
	{
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? EventOccurred;

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? ItemAssocChanged; // SHCNE_ASSOCCHANGED
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? ItemAttributesChanged; // SHCNE_ATTRIBUTES
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? ItemImageUpdated; // SHCNE_UPDATEIMAGE

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FileRenamed; // SHCNE_RENAMEITEM
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FileCreated; // SHCNE_CREATE
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FileDeleted; // SHCNE_DELETE
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FileUpdated; // SHCNE_UPDATEITEM

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FolderRenamed; // SHCNE_RENAMEFOLDER
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FolderCreated; // SHCNE_MKDIR
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FolderDeleted; // SHCNE_RMDIR
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FolderUpdated; // SHCNE_UPDATEDIR

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? MediaInserted; // SHCNE_MEDIAINSERTED
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? MediaRemoved; // SHCNE_MEDIAREMOVED
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? DriveRemoved; // SHCNE_DRIVEREMOVED
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? DriveAdded; // SHCNE_DRIVEADD
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? DriveAddedViaGUI; // SHCNE_DRIVEADDGUI
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? FreeSpaceUpdated; // SHCNE_FREESPACE

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? SharingStarted; // SHCNE_NETSHARE
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? SharingStopped; // SHCNE_NETUNSHARE

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? DisconnectedFromServer; // SHCNE_SERVERDISCONNECT

		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? ExtendedEventOccurred; // SHCNE_EXTENDED_EVENT
		public event TypedEventHandler<WindowsFolderWatcher, WindowsFolderWatcherEventArgs>? SystemInterruptOccurred; // SHCNE_INTERRUPT
	}
}
