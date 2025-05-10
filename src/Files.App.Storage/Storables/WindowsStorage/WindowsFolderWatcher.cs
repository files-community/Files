// Copyright (c) Files Community
// Licensed under the MIT License.

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
	public unsafe partial class WindowsFolderWatcher : IFolderWatcher
	{
		// Fields

		private const uint WM_FOLDERWATCHER = PInvoke.WM_APP | 0x0001U;
		private readonly WNDPROC _wndProc;

		private uint _watcherRegID = 0U;
		private ITEMIDLIST* _targetItemPIDL = default;

		// Properties

		public IMutableFolder Folder { get; private set; }

		// Events

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? ItemAssocChanged; // SHCNE_ASSOCCHANGED
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? ItemAttributesChanged; // SHCNE_ATTRIBUTES
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? ItemImageUpdated; // SHCNE_UPDATEIMAGE

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FileRenamed; // SHCNE_RENAMEITEM
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FileCreated; // SHCNE_CREATE
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FileDeleted; // SHCNE_DELETE
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FileUpdated; // SHCNE_UPDATEITEM

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FolderRenamed; // SHCNE_RENAMEFOLDER
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FolderCreated; // SHCNE_MKDIR
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FolderDeleted; // SHCNE_RMDIR
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FolderUpdated; // SHCNE_UPDATEDIR

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? MediaInserted; // SHCNE_MEDIAINSERTED
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? MediaRemoved; // SHCNE_MEDIAREMOVED
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? DriveRemoved; // SHCNE_DRIVEREMOVED
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? DriveAdded; // SHCNE_DRIVEADD
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? DriveAddedViaGUI; // SHCNE_DRIVEADDGUI
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? DiskEventOccurred; // SHCNE_DISKEVENTS
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? FreeSpaceUpdated; // SHCNE_FREESPACE

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? SharingStarted; // SHCNE_NETSHARE
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? SharingStopped; // SHCNE_NETUNSHARE

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? DisconnectedFromServer; // SHCNE_SERVERDISCONNECT

		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? ExtendedEventOccurred; // SHCNE_EXTENDED_EVENT
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? GlobalEventOccurred; // SHCNE_GLOBALEVENTS
		public event TypedEventHandler<WindowsFolderWatcher, IWindowsStorable>? SystemInterruptOccurred; // SHCNE_INTERRUPT

		// Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsFolderWatcher"/> class.
		/// </summary>
		/// <param name="folder">Specifies the folder to be monitored for changes.</param>
		public WindowsFolderWatcher(WindowsFolder folder)
		{
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

		private unsafe LRESULT WndProc(HWND hWnd, uint uMessage, WPARAM wParam, LPARAM lParam)
		{
			switch (uMessage)
			{
				case PInvoke.WM_CREATE:
					{
						PInvoke.CoInitialize();

						ITEMIDLIST* pidl = default;
						IWindowsFolder folder = (IWindowsFolder)Folder;
						PInvoke.SHGetIDListFromObject((IUnknown*)folder.ThisPtr.Get(), &pidl);
						_targetItemPIDL = pidl;

						SHChangeNotifyEntry changeNotifyEntry = default;
						changeNotifyEntry.pidl = pidl;

						_watcherRegID = PInvoke.SHChangeNotifyRegister(
							hWnd,
							SHCNRF_SOURCE.SHCNRF_ShellLevel | SHCNRF_SOURCE.SHCNRF_NewDelivery,
							(int)SHCNE_ID.SHCNE_ALLEVENTS,
							WM_FOLDERWATCHER,
							1,
							&changeNotifyEntry);

						if (_watcherRegID is 0U)
							break;
					}
					break;
				case WM_FOLDERWATCHER:
					{
						ITEMIDLIST** ppidl;
						int lEvent = 0;
						HANDLE hLock = PInvoke.SHChangeNotification_Lock((HANDLE)(nint)wParam.Value, (uint)lParam.Value, &ppidl, &lEvent);

						if (hLock.IsNull)
							break;

						// TODO: Fire events

						PInvoke.SHChangeNotification_Unlock(hLock);
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

		public void Dispose()
		{
			PInvoke.SHChangeNotifyDeregister(_watcherRegID);
			PInvoke.CoTaskMemFree(_targetItemPIDL);
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
