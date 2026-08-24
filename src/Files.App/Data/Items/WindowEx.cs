// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
using WinRT;
using MONITORENUMPROC = Windows.Win32.Extras.ManagedMONITORENUMPROC;
using WNDPROC = Windows.Win32.Extras.ManagedWNDPROC;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents base <see cref="Window"/> class to extend its features.
	/// </summary>
	public unsafe partial class WindowEx : Window, IDisposable
	{
		private bool _isInitialized;
		private bool _isClosing;
		private bool _isRestoringPlacement;
		private WINDOWPLACEMENT _lastOverlappedPlacement;
		private bool _hasOverlappedPlacement;
		private readonly nint _oldWndProc;
		private readonly WNDPROC _newWndProc;

		private readonly ApplicationDataContainer _applicationDataContainer = ApplicationData.Current.LocalSettings;

		/// <summary>
		/// Gets hWnd of this <see cref="Window"/>.
		/// </summary>
		public nint WindowHandle { get; }

		/// <summary>
		/// Gets min width of this <see cref="Window"/>.
		/// </summary>
		public int MinWidth { get; }

		/// <summary>
		/// Gets min height of this <see cref="Window"/>.
		/// </summary>
		public int MinHeight { get; }

		/// <summary>
		private bool _IsMaximizable = true;
		/// <summary>
		/// Gets or sets a value that indicates whether this <see cref="Window"/> can be maximizable.
		/// </summary>
		public bool IsMaximizable
		{
			get => _IsMaximizable;
			[DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
			set
			{
				_IsMaximizable = value;

				if (AppWindow.Presenter is OverlappedPresenter overlapped)
					overlapped.IsMaximizable = value;
			}
		}

		private bool _IsMinimizable = true;
		/// <summary>
		/// Gets or sets a value that indicates whether this <see cref="Window"/> can be minimizable.
		/// </summary>
		public bool IsMinimizable
		{
			get => _IsMinimizable;
			[DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
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
			IsMaximizable = true;
			IsMinimizable = true;

			_newWndProc = new(NewWindowProc);
			var pNewWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProc);
			_oldWndProc = PInvoke.SetWindowLongPtr(new(WindowHandle), WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, pNewWndProc);

			Closed += WindowEx_Closed;
			Activated += WindowEx_Activated;
		}

		protected virtual bool PersistPlacement => false;

		private unsafe void StoreWindowPlacementData()
		{
			if (!PersistPlacement)
				return;

			// Store monitor info
			using var data = new SystemIO.MemoryStream();
			using var sw = new SystemIO.BinaryWriter(data);

			var monitors = GetAllMonitorInfo();
			sw.Write(monitors.Count);

			foreach (var monitor in monitors)
			{
				sw.Write(monitor.Item1);
				sw.Write(monitor.Item2.Left);
				sw.Write(monitor.Item2.Top);
				sw.Write(monitor.Item2.Right);
				sw.Write(monitor.Item2.Bottom);
			}

			WINDOWPLACEMENT placement = default;
			if (IsOverlappedPresenter())
				PInvoke.GetWindowPlacement(new(WindowHandle), ref placement);
			else if (_hasOverlappedPlacement)
				// Closing in fullscreen/compact overlay: persist the last overlapped geometry instead
				placement = _lastOverlappedPlacement;
			else
				return;

			int structSize = Marshal.SizeOf<WINDOWPLACEMENT>();
			IntPtr buffer = Marshal.AllocHGlobal(structSize);
			Marshal.StructureToPtr(placement, buffer, false);
			byte[] placementData = new byte[structSize];
			Marshal.Copy(buffer, placementData, 0, structSize);
			Marshal.FreeHGlobal(buffer);

			sw.Write(placementData);
			sw.Flush();

			var values = GetDataStore(out _, true);

			if (_applicationDataContainer.Containers.ContainsKey("WinUIEx"))
				_applicationDataContainer.DeleteContainer("WinUIEx");

			values["MainWindowPlacementData"] = Convert.ToBase64String(data.ToArray());
		}

		private void RestoreWindowPlacementData()
		{
			if (!PersistPlacement)
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
			if (monitorCount != monitors.Count)
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

			int structSize = Marshal.SizeOf<WINDOWPLACEMENT>();
			byte[] placementData = br.ReadBytes(structSize);
			IntPtr buffer = Marshal.AllocHGlobal(structSize);
			Marshal.Copy(placementData, 0, buffer, structSize);
			var windowPlacementData = Marshal.PtrToStructure<WINDOWPLACEMENT>(buffer);

			Marshal.FreeHGlobal(buffer);

			// Ignore anything by maximized or normal
			if (windowPlacementData.showCmd == (SHOW_WINDOW_CMD)0x0002 /*SW_INVALIDATE*/ &&
				windowPlacementData.flags == WINDOWPLACEMENT_FLAGS.WPF_RESTORETOMAXIMIZED)
				windowPlacementData.showCmd = SHOW_WINDOW_CMD.SW_MAXIMIZE;
			else if (windowPlacementData.showCmd != SHOW_WINDOW_CMD.SW_MAXIMIZE)
				windowPlacementData.showCmd = SHOW_WINDOW_CMD.SW_NORMAL;

			// Suppress DPI-change reflow and min-size clamping while the persisted placement is applied
			_isRestoringPlacement = true;
			try
			{
				PInvoke.SetWindowPlacement(new(WindowHandle), in windowPlacementData);
			}
			finally
			{
				_isRestoringPlacement = false;
			}
		}

		private IPropertySet GetDataStore(out bool oldDataExists, bool useNewStore = true)
		{
			IPropertySet values;
			oldDataExists = false;

			if (_applicationDataContainer.Containers.TryGetValue("Files", out var dataContainer))
			{
				values = dataContainer.Values;
			}
			else if (!useNewStore && _applicationDataContainer.Containers.TryGetValue("WinUIEx", out var oldDataContainer))
			{
				values = oldDataContainer.Values;
				oldDataExists = true;
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

			MONITORENUMPROC monitorEnumProc = new((HMONITOR monitor, HDC deviceContext, RECT* rect, LPARAM data) =>
			{
				MONITORINFOEXW info = default;
				info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

				PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info);

				monitors.Add(new(
					info.szDevice.ToString(),
					new(new Point(rect->left, rect->top), new Point(rect->right, rect->bottom))));

				return true;
			});

			var pMonitorEnumProc = Marshal.GetFunctionPointerForDelegate(monitorEnumProc);
			var pfnMonitorEnumProc = (delegate* unmanaged[Stdcall]<HMONITOR, HDC, RECT*, LPARAM, BOOL>)pMonitorEnumProc;

			LPARAM lParam = default;
			BOOL fRes = PInvoke.EnumDisplayMonitors(new(nint.Zero), (RECT*)null, pfnMonitorEnumProc, lParam);
			if (!fRes)
				Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());

			return monitors;
		}

		// Return true to mark the message handled and skip the original window procedure
		protected virtual bool OnWindowMessageReceived(uint message, WPARAM wParam, LPARAM lParam, ref LRESULT result)
		{
			return false;
		}

		private bool IsOverlappedPresenter()
		{
			// COMException when the AppWindow is queried during window teardown
			try
			{
				return AppWindow?.Presenter?.Kind is AppWindowPresenterKind.Overlapped;
			}
			catch (COMException)
			{
				return false;
			}
		}

		private LRESULT NewWindowProc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
		{
			LRESULT overrideResult = default;
			if (OnWindowMessageReceived(param1, param2, param3, ref overrideResult))
				return overrideResult;

			switch (param1)
			{
				case 0x0018 /*WM_SHOWWINDOW*/ when param2 == (WPARAM)1 && !_isInitialized:
					{
						_isInitialized = true;

						// A malformed persisted blob (FormatException/EndOfStreamException) must not unwind through the native wndproc
						try
						{
							RestoreWindowPlacementData();
						}
						catch (Exception)
						{
						}

						break;
					}
				case 0x0024 /*WM_GETMINMAXINFO*/ when !_isRestoringPlacement:
					{
						var dpi = PInvoke.GetDpiForWindow(param0);
						float scalingFactor = (float)dpi / 96;

						var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(param3);
						minMaxInfo.ptMinTrackSize.X = (int)(MinWidth * scalingFactor);
						minMaxInfo.ptMinTrackSize.Y = (int)(MinHeight * scalingFactor);
						Marshal.StructureToPtr(minMaxInfo, param3, false);
						break;
					}
				case 0x02E0 /*WM_DPICHANGED*/ when _isRestoringPlacement:
					{
						// Keep the persisted rect: don't let the default handler apply the DPI-suggested rectangle
						return default;
					}
				case 0x0047 /*WM_WINDOWPOSCHANGED*/ when PersistPlacement && IsOverlappedPresenter():
					{
						WINDOWPLACEMENT placement = default;
						PInvoke.GetWindowPlacement(param0, ref placement);
						_lastOverlappedPlacement = placement;
						_hasOverlappedPlacement = true;
						break;
					}
			}

			var pfnOldWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)_oldWndProc;

			return PInvoke.CallWindowProc(pfnOldWndProc, param0, param1, param2, param3);
		}

		private void WindowEx_Closed(object sender, WindowEventArgs args)
		{
			_isClosing = true;

			// A LocalSettings write failure (COMException/UnauthorizedAccessException) must not crash the close path
			try
			{
				StoreWindowPlacementData();
			}
			catch (Exception)
			{
			}
		}

		private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (args.WindowActivationState is not WindowActivationState.Deactivated)
				_isClosing = false;

			if (!_isClosing && SystemBackdrop is AppSystemBackdrop appSystemBackdrop)
				appSystemBackdrop.SetInputActive(args.WindowActivationState is not WindowActivationState.Deactivated);
		}

		public void Dispose()
		{
			Closed -= WindowEx_Closed;
			Activated -= WindowEx_Activated;
		}
	}
}
