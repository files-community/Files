// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents an implementation of <see cref="IFolderWatcher"/> that uses Windows Shell notifications to watch for changes in a folder.
	/// </summary>
	public unsafe partial class WindowsFolderWatcher : IWindowsFolderWatcher
	{
		// Fields

		private const uint WM_NOTIFYFOLDERCHANGE = PInvoke.WM_APP | 0x0001U;
		private readonly WNDPROC _wndProc;

		private uint _watcherRegID = 0U;
		private ITEMIDLIST* _folderPidl = default;
		private Debouncer _debouncer;

		// Properties

		public IMutableFolder Folder { get; private set; }

		// Events

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

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

		// Constructor

		/// <summary>Initializes a new instance of the <see cref="WindowsFolderWatcher"/> class.</summary>
		/// <param name="folder">Specifies the folder to be monitored for changes.</param>
		public WindowsFolderWatcher(WindowsFolder folder, int debounceMilliseconds = 1000)
		{
			_debouncer = new(debounceMilliseconds);

			Folder = folder;

			fixed (char* pszClassName = $"FolderWatcherWindowClass{Guid.NewGuid():B}")
			{
				_wndProc = new(WndProc);

				WNDCLASSEXW wndClass = default;
				wndClass.cbSize = (uint)sizeof(WNDCLASSEXW);
				wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
				wndClass.hInstance = PInvoke.GetModuleHandle(default(PWSTR));
				wndClass.lpszClassName = pszClassName;

				PInvoke.RegisterClassEx(&wndClass);
				PInvoke.CreateWindowEx(0, pszClassName, null, 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, default, wndClass.hInstance, null);
			}
		}

		// Methods

		private unsafe LRESULT WndProc(HWND hWnd, uint uMessage, WPARAM wParam, LPARAM lParam)
		{
			switch (uMessage)
			{
				case PInvoke.WM_CREATE:
					{
						PInvoke.CoInitialize();

						ITEMIDLIST* pidl = default;
						IWindowsFolder folder = (IWindowsFolder)Folder;
						PInvoke.SHGetIDListFromObject((IUnknown*)folder.ThisPtr, &pidl);
						_folderPidl = pidl;

						SHChangeNotifyEntry changeNotifyEntry = default;
						changeNotifyEntry.pidl = pidl;

						_watcherRegID = PInvoke.SHChangeNotifyRegister(
							hWnd,
							SHCNRF_SOURCE.SHCNRF_ShellLevel | SHCNRF_SOURCE.SHCNRF_NewDelivery,
							(int)SHCNE_ID.SHCNE_ALLEVENTS,
							WM_NOTIFYFOLDERCHANGE,
							1,
							&changeNotifyEntry);

						if (_watcherRegID is 0U)
							break;
					}
					break;
				case WM_NOTIFYFOLDERCHANGE:
					{
						ITEMIDLIST** ppidl;
						uint lEvent = 0;
						HANDLE hLock = PInvoke.SHChangeNotification_Lock((HANDLE)(nint)wParam.Value, (uint)lParam.Value, &ppidl, (int*)&lEvent);

						if (hLock.IsNull)
							break;

						ITEMIDLIST* pOldPidl = ppidl[0];
						ITEMIDLIST* pNewPidl = ppidl[1];

						SHCNE_ID eventType = (SHCNE_ID)lEvent;
						var oldItem = WindowsStorable.TryParse(pOldPidl);
						var newItem = WindowsStorable.TryParse(pNewPidl);

						_debouncer.Debounce(() =>
						{
							FireEvent(eventType, oldItem, newItem);
						});

						PInvoke.SHChangeNotification_Unlock(hLock);

						PInvoke.CoTaskMemFree(pOldPidl);
						PInvoke.CoTaskMemFree(pNewPidl);
					}
					break;
				case PInvoke.WM_DESTROY:
					{
						Dispose();
					}
					break;
			}

			return PInvoke.DefWindowProc(hWnd, uMessage, wParam, lParam);
		}

		private void FireEvent(SHCNE_ID eventType, IWindowsStorable? oldItem, IWindowsStorable? newItem)
		{
			EventOccurred?.Invoke(this, new(eventType, oldItem, newItem));

			switch (eventType)
			{
				case SHCNE_ID.SHCNE_ASSOCCHANGED:
					{
						ItemAssocChanged?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_ATTRIBUTES:
					{
						ItemAttributesChanged?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_UPDATEIMAGE:
					{
						ItemImageUpdated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_RENAMEITEM:
					{
						FileRenamed?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_CREATE:
					{
						FileCreated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_DELETE:
					{
						FileDeleted?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_UPDATEITEM:
					{
						FileUpdated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_RENAMEFOLDER:
					{
						FolderRenamed?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_MKDIR:
					{
						FolderCreated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_RMDIR:
					{
						FolderDeleted?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_UPDATEDIR:
					{
						FolderUpdated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_MEDIAINSERTED:
					{
						MediaInserted?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_MEDIAREMOVED:
					{
						MediaRemoved?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_DRIVEREMOVED:
					{
						DriveRemoved?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_DRIVEADD:
					{
						DriveAdded?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_DRIVEADDGUI:
					{
						DriveAddedViaGUI?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_FREESPACE:
					{
						FreeSpaceUpdated?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_NETSHARE:
					{
						SharingStarted?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_NETUNSHARE:
					{
						SharingStopped?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_SERVERDISCONNECT:
					{
						DisconnectedFromServer?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_EXTENDED_EVENT:
					{
						ExtendedEventOccurred?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
				case SHCNE_ID.SHCNE_INTERRUPT:
					{
						SystemInterruptOccurred?.Invoke(this, new(eventType, oldItem, newItem));
					}
					break;
			}
		}

		// Disposers

		public void Dispose()
		{
			PInvoke.SHChangeNotifyDeregister(_watcherRegID);
			PInvoke.CoTaskMemFree(_folderPidl);
			PInvoke.CoUninitialize();
			PInvoke.PostQuitMessage(0);
		}

		public ValueTask DisposeAsync()
		{
			Dispose();

			return ValueTask.CompletedTask;
		}
	}
}
