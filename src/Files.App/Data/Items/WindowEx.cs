// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents base <see cref="Window"/> class to extend its features.
	/// </summary>
	public unsafe class WindowEx : Window
	{
		private readonly WNDPROC _oldWndProc;
		private readonly WNDPROC _newWndProc;

		private readonly ApplicationDataContainer _applicationDataContainer = ApplicationData.Current.LocalSettings;

		/// <summary>
		/// Gets hWnd of this <see cref="Window"/>.
		/// </summary>
		public nint WindowHandle { get; }

		/// <summary>
		/// Gets or sets min width of this <see cref="Window"/>.
		/// </summary>
		public int MinWidth { get; set; }

		/// <summary>
		/// Gets or sets min height of this <see cref="Window"/>.
		/// </summary>
		public int MinHeight { get; set; }

		private bool _IsMaximizable = true;
		/// <summary>
		/// Gets or sets a value that indicates whether this <see cref="Window"/> can be maximizable.
		/// </summary>
		public bool IsMaximizable
		{
			get => _IsMaximizable;
			set
			{
				_IsMaximizable = value;

				if (AppWindow.Presenter is OverlappedPresenter overlapped)
					overlapped.IsMaximizable = value;

				if (value)
				{
					// NOTE:
					//  Indicates to the Shell that the window should not be treated as full-screen.
					// WORKAROUND:
					//  https://github.com/microsoft/microsoft-ui-xaml/issues/8431
					//  Not to mess up the taskbar when being full-screen mode.
					//  This property should only be set if the "Automatically hide the taskbar" in Windows 11,
					//  or "Automatically hide the taskbar in desktop mode" in Windows 10 is enabled.
					//  Setting this property when the setting is disabled will result in the taskbar overlapping the application.
					if (AppLifecycleHelper.IsAutoHideTaskbarEnabled())
						Win32PInvoke.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
				}
			}
		}

		private bool _IsMinimizable = true;
		/// <summary>
		/// Gets or sets a value that indicates whether this <see cref="Window"/> can be minimizable.
		/// </summary>
		public bool IsMinimizable
		{
			get => _IsMinimizable;
			set
			{
				_IsMinimizable = value;

				if (AppWindow.Presenter is OverlappedPresenter overlapped)
					overlapped.IsMinimizable = value;
			}
		}

		/// <summary>
		/// Initializes <see cref="WindowEx"/> class.
		/// </summary>
		/// <param name="minWidth">Min width to set when initialized.</param>
		/// <param name="minHeight">Min height to set when initialized.</param>
		public unsafe WindowEx(int minWidth = 400, int minHeight = 300)
		{
			WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
			MinWidth = minWidth;
			MinHeight = minHeight;

			RestoreWindowPlacementData();

			_newWndProc = new(NewWindowProc);
			var pNewWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProc);
			var pOldWndProc = PInvoke.SetWindowLongPtr(new(WindowHandle), WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, pNewWndProc);
			_oldWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(pOldWndProc);

			Closed += (s, e) => { StoreWindowPlacementData(); };
		}

		private unsafe void StoreWindowPlacementData()
		{
			// Save window placement only for MainWindow
			if (!GetType().Name.Equals(nameof(MainWindow), StringComparison.OrdinalIgnoreCase))
				return;

			// Store monitor info
			using var data = new SystemIO.MemoryStream();
			using var sw = new SystemIO.BinaryWriter(data);

			var monitors = GetAllMonitorInfo();
			int nMonitors = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CMONITORS);
			sw.Write(nMonitors);

			foreach (var monitor in monitors)
			{
				sw.Write(monitor.Item1);
				sw.Write(monitor.Item2.Left);
				sw.Write(monitor.Item2.Top);
				sw.Write(monitor.Item2.Right);
				sw.Write(monitor.Item2.Bottom);
			}

			WINDOWPLACEMENT placement = default;
			PInvoke.GetWindowPlacement(new(WindowHandle), ref placement);

			int structSize = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
			IntPtr buffer = Marshal.AllocHGlobal(structSize);
			Marshal.StructureToPtr(placement, buffer, false);
			byte[] placementData = new byte[structSize];
			Marshal.Copy(buffer, placementData, 0, structSize);
			Marshal.FreeHGlobal(buffer);

			sw.Write(placementData);
			sw.Flush();

			var values = GetDataStore(out _, true);

			if (_applicationDataContainer.Containers.ContainsKey("WinUIEx"))
				_applicationDataContainer.Values.Remove("WinUIEx");

			values["MainWindowPlacementData"] = Convert.ToBase64String(data.ToArray());
		}

		private void RestoreWindowPlacementData()
		{
			// Save window placement only for MainWindow
			if (!GetType().Name.Equals(nameof(MainWindow), StringComparison.OrdinalIgnoreCase))
				return;

			var values = GetDataStore(out var oldDataExists, false);

			byte[]? data = null;
			if (values.TryGetValue(oldDataExists ? "WindowPersistance_FilesMainWindow" : "MainWindowPlacementData", out object? value))
			{
				if (value is string base64)
					data = Convert.FromBase64String(base64);
			}

			if (data is null)
				return;

			SystemIO.BinaryReader br = new(new SystemIO.MemoryStream(data));

			// Check if monitor layout changed since we stored position
			var monitors = GetAllMonitorInfo();
			int monitorCount = br.ReadInt32();
			int nMonitors = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CMONITORS);
			if (monitorCount != nMonitors)
				return;

			for (int i = 0; i < monitorCount; i++)
			{
				var pMonitor = monitors[i];
				br.ReadString();
				if (pMonitor.Item2.Left != br.ReadDouble() ||
					pMonitor.Item2.Top != br.ReadDouble() ||
					pMonitor.Item2.Right != br.ReadDouble() ||
					pMonitor.Item2.Bottom != br.ReadDouble())
					return;
			}

			int structSize = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
			byte[] placementData = br.ReadBytes(structSize);
			IntPtr buffer = Marshal.AllocHGlobal(structSize);
			Marshal.Copy(placementData, 0, buffer, structSize);
			var windowPlacementData = (WINDOWPLACEMENT)Marshal.PtrToStructure(buffer, typeof(WINDOWPLACEMENT))!;

			Marshal.FreeHGlobal(buffer);

			// Ignore anything by maximized or normal
			if (windowPlacementData.showCmd == (SHOW_WINDOW_CMD)0x0002 /*SW_INVALIDATE*/ &&
				windowPlacementData.flags == WINDOWPLACEMENT_FLAGS.WPF_RESTORETOMAXIMIZED)
				windowPlacementData.showCmd = SHOW_WINDOW_CMD.SW_MAXIMIZE;
			else if (windowPlacementData.showCmd != SHOW_WINDOW_CMD.SW_MAXIMIZE)
				windowPlacementData.showCmd = SHOW_WINDOW_CMD.SW_NORMAL;

			PInvoke.SetWindowPlacement(new(WindowHandle), in windowPlacementData);

			return;
		}

		private IPropertySet GetDataStore(out bool oldDataExists, bool useNewStore = true)
		{
			IPropertySet values;
			oldDataExists = false;

			// TODO: Remove this after a couple of months past
			if (!useNewStore && _applicationDataContainer.Containers.TryGetValue("WinUIEx", out var oldDataContainer))
			{
				values = oldDataContainer.Values;
				oldDataExists = true;
			}
			else if (_applicationDataContainer.Containers.TryGetValue("Files", out var dataContainer))
			{
				values = dataContainer.Values;
			}
			else
			{
				values = _applicationDataContainer.CreateContainer(
					"Files",
					ApplicationDataCreateDisposition.Always).Values;
			}

			return values;
		}

		private unsafe List<Tuple<string, Rect>> GetAllMonitorInfo()
		{
			List<Tuple<string, Rect>> monitors = [];
			MONITORENUMPROC callback = new((HMONITOR monitor, HDC deviceContext, RECT* rect, LPARAM data) =>
			{
				MONITORINFOEXW info = default;
				info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

				//fixed (MONITORINFOEXW* pInfo = &info)
				//{
					PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info);

					monitors.Add(new(
						info.szDevice.ToString(),
						new(new Point(rect->left, rect->top), new Point(rect->right, rect->bottom))));
				//}

				return true;
			});

			LPARAM lParam = default;
			bool ok = PInvoke.EnumDisplayMonitors(new(nint.Zero), (RECT*)null, callback, lParam);
			if (!ok)
				Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());

			return monitors;
		}

		private LRESULT NewWindowProc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
		{
			switch (param1)
			{
				case 0x0024: /*WM_GETMINMAXINFO*/
					{
						var dpi = PInvoke.GetDpiForWindow(new(param0));
						float scalingFactor = (float)dpi / 96;

						var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(param3);
						minMaxInfo.ptMinTrackSize.X = (int)(MinWidth * scalingFactor);
						minMaxInfo.ptMinTrackSize.Y = (int)(MinHeight * scalingFactor);
						Marshal.StructureToPtr(minMaxInfo, param3, true);
						break;
					}
			}

			return PInvoke.CallWindowProc(_oldWndProc, param0, param1, param2, param3);
		}
	}
}
