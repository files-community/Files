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
using Windows.Win32.Graphics.DirectComposition;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Files.App.ViewModels.Previews
{
	public sealed class ShellPreviewViewModel : BasePreviewModel
	{
		// Fields

		PreviewHandler? _currentPreviewHandler;
		ContentExternalOutputLink? _contentExternalOutputLink;
		WNDCLASSEXW _windowClass;
		WNDPROC _windProc = null!;
		HWND _hWnd = HWND.Null;
		bool _isOfficePreview = false;

		// Initializer

		public ShellPreviewViewModel(ListedItem item) : base(item)
		{
		}

		// Methods

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
			if (_hWnd != HWND.Null)
				PInvoke.SetWindowPos(_hWnd, (HWND)0, size.left, size.top, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

			_currentPreviewHandler?.ResetBounds(new(0, 0, size.Width, size.Height));

			if (_contentExternalOutputLink is not null)
				_contentExternalOutputLink.PlacementVisual.Size = new(size.Width, size.Height);
		}

		private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			if (msg == 0x0001 /*WM_CREATE*/)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd);

				_isOfficePreview = new Guid?[]
				{
					Guid.Parse("84F66100-FF7C-4fb4-B0C0-02CD7FB668FE"), // preview handler for Word files
					Guid.Parse("65235197-874B-4A07-BDC5-E65EA825B718"), // preview handler for PowerPoint files
					Guid.Parse("00020827-0000-0000-C000-000000000046")  // preview handler for Excel files
				}.Contains(clsid);

				try
				{
					_currentPreviewHandler = new PreviewHandler(clsid.Value, hwnd);
					_currentPreviewHandler.Initialize(Item.ItemPath);
					_currentPreviewHandler.DoPreview();
				}
				catch
				{
					UnloadPreview();
				}
			}
			else if (msg == 0x0002 /*WM_DESTROY*/)
			{
				if (_currentPreviewHandler is not null)
				{
					_currentPreviewHandler.UnloadPreview();
					_currentPreviewHandler = null;
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
				_windProc = new(WndProc);
				var pWindProc = Marshal.GetFunctionPointerForDelegate(_windProc);
				var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

				_windowClass = new WNDCLASSEXW()
				{
					cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
					lpfnWndProc = pfnWndProc,
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

				PInvoke.RegisterClassEx(_windowClass);

				fixed (char* pszWindowName = szWindowName)
				{
					_hWnd = PInvoke.CreateWindowEx(
						WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED,
						pszClassName,
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

			ID3D11Device* pD3D11Device = default;
			ID3D11DeviceContext* pD3D11DeviceContext = default;

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
					&pD3D11Device,
					null,
					&pD3D11DeviceContext);

				if (hr.Succeeded)
					break;
			}

			if (pD3D11Device is null)
				return false;

			IDXGIDevice* pDXGIDevice = (IDXGIDevice*)pD3D11Device;
			if (PInvoke.DCompositionCreateDevice(pDXGIDevice, typeof(IDCompositionDevice).GUID, out var compositionDevicePtr).Failed)
				return false;

			var pDCompositionDevice = (IDCompositionDevice*)compositionDevicePtr;
			IDCompositionVisual* pChildVisual = default;
			IUnknown* pControlSurface = default;

			pDCompositionDevice->CreateVisual(&pChildVisual);
			pDCompositionDevice->CreateSurfaceFromHwnd(new(_hWnd), &pControlSurface);
			pChildVisual->SetContent(pControlSurface);
			if (pChildVisual is null || pControlSurface is null)
				return false;

			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			_contentExternalOutputLink = ContentExternalOutputLink.Create(compositor);

			var target = _contentExternalOutputLink.As<IDCompositionTarget.Interface>();
			target.SetRoot(pChildVisual);

			_contentExternalOutputLink.PlacementVisual.Size = new(0, 0);
			_contentExternalOutputLink.PlacementVisual.Scale = new(1 / (float)presenter.XamlRoot.RasterizationScale);
			ElementCompositionPreview.SetElementChildVisual(presenter, _contentExternalOutputLink.PlacementVisual);

			pDCompositionDevice->Commit();

			Marshal.ReleaseComObject(target);
			pChildVisual->Release();
			pControlSurface->Release();
			pDCompositionDevice->Release();
			pDXGIDevice->Release();
			pD3D11Device->Release();
			pD3D11DeviceContext->Release();

			var dwAttrib = Convert.ToUInt32(true);

			return
				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib))
				.Succeeded;
		}

		public unsafe void PointerEntered(bool onPreview)
		{
			if (onPreview)
			{
				var dwAttrib = Convert.ToUInt32(false);

				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib));

				if (_isOfficePreview)
					PInvoke.SetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0);
			}
			else
			{
				PInvoke.SetWindowLong(
					_hWnd,
					WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
					(int)(WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED));

				var dwAttrib = Convert.ToUInt32(true);

				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib));
			}
		}

		// Disposer

		public void UnloadPreview()
		{
			if (_hWnd != HWND.Null)
				PInvoke.DestroyWindow(_hWnd);

			try
			{
				var target = _contentExternalOutputLink.As<IDCompositionTarget.Interface>();
				Marshal.ReleaseComObject(target);
				_contentExternalOutputLink?.Dispose();
			}
			finally
			{
				_contentExternalOutputLink = null;
			}

			PInvoke.UnregisterClass(_windowClass.lpszClassName, PInvoke.GetModuleHandle(default(PWSTR)));
		}
	}
}

#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
