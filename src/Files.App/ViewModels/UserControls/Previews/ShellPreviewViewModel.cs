// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.DirectComposition;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

// Description: Feature is for evaluation purposes only and is subject to change or removal in future updates.
// Justification: We have to use ContentExternalOutputLink for shell previews.
#pragma warning disable CS8305

namespace Files.App.ViewModels.Previews
{
	public sealed class ShellPreviewViewModel : BasePreviewModel
	{
		PreviewHandler? currentHandler;
		ContentExternalOutputLink? outputLink;
		WNDCLASSEXW? wCls;
		HWND hwnd = HWND.Null;
		bool isOfficePreview = false;

		public ShellPreviewViewModel(ListedItem item) : base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> [];

		public static unsafe Guid? FindPreviewHandlerFor(string extension, nint hwnd)
		{
			if (string.IsNullOrEmpty(extension))
				return null;

			try
			{
				fixed (char* pszOutput = new char[1024])
				{
					PWSTR pwszOutput = new(pszOutput);
					uint cchOutput = 512u;

					// Try to find registered preview handler associated with specified extension name
					var res = PInvoke.AssocQueryString(
						ASSOCF.ASSOCF_NOTRUNCATE,
						ASSOCSTR.ASSOCSTR_SHELLEXTENSION,
						extension,
						"{8895b1c6-b41f-4c1c-a562-0d564250836f}",
						pszOutput,
						ref cchOutput);

					return Guid.Parse(pwszOutput.ToString());
				}
			}
			catch
			{
				return null;
			}
		}

		public void SizeChanged(RECT size)
		{
			if (hwnd != HWND.Null)
				PInvoke.SetWindowPos(hwnd, (HWND)0, size.left, size.top, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

			if (currentHandler != null)
				currentHandler.ResetBounds(new(0, 0, size.Width, size.Height));

			if (outputLink is not null)
				outputLink.PlacementVisual.Size = new(size.Width, size.Height);
		}

		private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			//if (msg == 0x0081 /*WM_NCCREATE*/)
			//{
			//	try
			//	{
			//		var cp = Marshal.PtrToStructure<CREATESTRUCTW>(lParam).lpCreateParams;
			//		var pCreateParams = new nint(cp);
			//		if (pCreateParams != nint.Zero && GCHandle.FromIntPtr(pCreateParams).Target is IWindowInit wnd)
			//			return wnd.InitWndProcOnNCCreate(
			//				hwnd,
			//				msg,
			//				Marshal.GetFunctionPointerForDelegate(wndProc ?? throw new NullReferenceException()),
			//				lParam);
			//	}
			//	catch { }
			//}
			//else
			if (msg == 0x0001 /*WM_CREATE*/)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd.Value);

				isOfficePreview = new Guid?[] {
					Guid.Parse("84F66100-FF7C-4fb4-B0C0-02CD7FB668FE"), // 
					Guid.Parse("65235197-874B-4A07-BDC5-E65EA825B718"),
					Guid.Parse("00020827-0000-0000-C000-000000000046")
				}.Contains(clsid);

				try
				{
					currentHandler = new PreviewHandler(clsid.Value, hwnd.Value);
					currentHandler.InitWithFileWithEveryWay(Item.ItemPath);
					currentHandler.DoPreview();
				}
				catch (Exception ex)
				{
					UnloadPreview();
				}
			}
			else if (msg == 0x0002 /*WM_DESTROY*/)
			{
				if (currentHandler is not null)
				{
					currentHandler.UnloadPreview();
					currentHandler = null;
				}
			}

			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public unsafe void LoadPreview(UIElement presenter)
		{
			var parent = MainWindow.Instance.WindowHandle;
			var hInst = PInvoke.GetModuleHandle(default(PWSTR));
			var szClassName = $"{GetType().Name}-{Guid.NewGuid()}";
			var szWindowName = $"Preview";

			fixed (char* pszClassName = szClassName)
			{
				wCls = new WNDCLASSEXW
				{
					cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
					lpfnWndProc = new(WndProc),
					hInstance = hInst,
					lpszClassName = pszClassName,
					style = 0,
					hIcon = default,
					hIconSm = default,
					hCursor = default,
					hbrBackground = default,
					lpszMenuName = null,
					cbClsExtra = 0,
					cbWndExtra = 0,
				};

				fixed (char* pszWindowName = szWindowName)
				{
					hwnd = PInvoke.CreateWindowEx(
						WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED,
						wCls.Value.lpszClassName,
						pszWindowName,
						WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_CLIPSIBLINGS | WINDOW_STYLE.WS_VISIBLE,
						0, 0, 0, 0,
						new(parent),
						HMENU.Null,
						hInst);
				}
			}

			_ = ChildWindowToXaml(parent, presenter);
		}

		private unsafe bool ChildWindowToXaml(nint parent, UIElement presenter)
		{
			D3D_DRIVER_TYPE[] driverTypes =
			[
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
			];

			ID3D11Device? d3d11Device = null;
			ID3D11DeviceContext? d3d11DeviceContext = null;

			foreach (var driveType in driverTypes)
			{
				var hr = PInvoke.D3D11CreateDevice(
					null,
					driveType,
					new(nint.Zero),
					D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
					null,
					0,
					7,
					out d3d11Device,
					null,
					out d3d11DeviceContext);

				if (hr.Succeeded)
					break;
			}

			if (d3d11Device is null)
				return false;

			IDXGIDevice dxgiDevice = (IDXGIDevice)d3d11Device;
			if (PInvoke.DCompositionCreateDevice(dxgiDevice, typeof(IDCompositionDevice).GUID, out var compDevicePtr).Failed)
				return false;

			IDCompositionDevice compDevice = (IDCompositionDevice)compDevicePtr;

			compDevice.CreateVisual(out var childVisual);
			compDevice.CreateSurfaceFromHwnd(hwnd, out var controlSurface);
			childVisual.SetContent(controlSurface);
			if (childVisual is null || controlSurface is null)
				return false;

			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			outputLink = ContentExternalOutputLink.Create(compositor);
			IDCompositionTarget target = outputLink.As<IDCompositionTarget>();
			target.SetRoot(childVisual);

			outputLink.PlacementVisual.Size = new(0, 0);
			outputLink.PlacementVisual.Scale = new(1/(float)presenter.XamlRoot.RasterizationScale);
			ElementCompositionPreview.SetElementChildVisual(presenter, outputLink.PlacementVisual);

			compDevice.Commit();

			Marshal.ReleaseComObject(target);
			Marshal.ReleaseComObject(childVisual);
			Marshal.ReleaseComObject(controlSurface);
			Marshal.ReleaseComObject(compDevice);
			Marshal.ReleaseComObject(compDevicePtr);
			Marshal.ReleaseComObject(dxgiDevice);
			Marshal.ReleaseComObject(d3d11Device);
			Marshal.ReleaseComObject(d3d11DeviceContext);

			unsafe
			{
				var dwAttrib = Convert.ToUInt32(true);

				return
					PInvoke.DwmSetWindowAttribute(
						new((nint)hwnd),
						DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
						&dwAttrib,
						(uint)Marshal.SizeOf(dwAttrib))
					.Succeeded;
			}
		}

		public void UnloadPreview()
		{
			if (hwnd != HWND.Null)
				PInvoke.DestroyWindow(hwnd);

			outputLink?.Dispose();

			if (wCls is not null)
				PInvoke.UnregisterClass(wCls.Value.lpszClassName, PInvoke.GetModuleHandle(default(PWSTR)));
		}

		public void PointerEntered(bool onPreview)
		{
			if (onPreview)
			{
				unsafe
				{
					var dwAttrib = Convert.ToUInt32(false);

					PInvoke.DwmSetWindowAttribute(
						new((nint)hwnd),
						DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
						&dwAttrib,
						(uint)Marshal.SizeOf(dwAttrib));
				}

				if (isOfficePreview)
					PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0);
			}
			else
			{
				PInvoke.SetWindowLong(
					hwnd,
					WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
					(int)(WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED));

				unsafe
				{
					var dwAttrib = Convert.ToUInt32(true);

					PInvoke.DwmSetWindowAttribute(
						new((nint)hwnd),
						DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
						&dwAttrib,
						(uint)Marshal.SizeOf(dwAttrib));
				}
			}
		}
	}
}
