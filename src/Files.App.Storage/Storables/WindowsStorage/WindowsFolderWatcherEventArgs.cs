// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public class WindowsFolderWatcherEventArgs : EventArgs
	{
		public SHCNE_ID EventType { get; init; }

		public IWindowsStorable? OldItem { get; init; }

		public IWindowsStorable? NewItem { get; init; }

		public WindowsFolderWatcherEventArgs(SHCNE_ID eventType, IWindowsStorable? _oldItem = null, IWindowsStorable? _newItem = null)
		{
			EventType = eventType;
			OldItem = _oldItem;
			NewItem = _newItem;
		}
	}
}
