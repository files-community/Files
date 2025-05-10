// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Runtime.InteropServices;
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
