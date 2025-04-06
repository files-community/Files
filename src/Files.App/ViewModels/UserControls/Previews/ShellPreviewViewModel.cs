// Copyright (c) Files Community
// Licensed under the MIT License.

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
	public sealed partial class ShellPreviewViewModel : BasePreviewModel
	{
		// Fields

		ContentExternalOutputLink? _contentExternalOutputLink;
		PreviewHandler? _previewHandler;
		WNDCLASSEXW _windowClass;
		WNDPROC _windProc = null!;
		HWND _hWnd = HWND.Null;
		bool _isOfficePreview = false;

		// Constructor

		public ShellPreviewViewModel(ListedItem item) : base(item)
		{
		}

		// Methods

		public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> Task.FromResult<List<FileProperty>>([]);

		public static unsafe Guid? FindPreviewHandlerFor(string extension, nint hwnd)
		{
			if (string.IsNullOrEmpty(extension))
				return null;

			try
			{
				fixed (char* pszAssoc = extension,
					pszExtra = "{8895b1c6-b41f-4c1c-a562-0d564250836f}",
					pszOutput = new char[1024])
				{
					PWSTR pwszAssoc = new(pszAssoc);
					PWSTR pwszExtra = new(pszExtra);
					PWSTR pwszOutput = new(pszOutput);
					uint cchOutput = 2024;

					// Try to find registered preview handler associated with specified extension name
					var res = PInvoke.AssocQueryString(
						ASSOCF.ASSOCF_NOTRUNCATE,
						ASSOCSTR.ASSOCSTR_SHELLEXTENSION,
						pwszAssoc,
						pwszExtra,
						pwszOutput,
						&cchOutput);

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
				PInvoke.SetWindowPos(_hWnd, new(0), size.left, size.top, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

			_previewHandler?.ResetBounds(new(0, 0, size.Width, size.Height));

			if (_contentExternalOutputLink is not null)
				_contentExternalOutputLink.PlacementVisual.Size = new(size.Width, size.Height);
		}

		private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			if (msg is PInvoke.WM_CREATE)
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
					_previewHandler = new PreviewHandler(clsid!.Value, hwnd);
					_previewHandler.InitWithFileWithEveryWay(Item.ItemPath);
					_previewHandler.DoPreview();
				}
				catch
				{
					UnloadPreview();
				}
			}
			else if (msg is PInvoke.WM_DESTROY)
			{
				if (_previewHandler is not null)
				{
					_previewHandler.Dispose();
					_previewHandler = null;
				}
			}

			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public unsafe void LoadPreview(UIElement presenter)
		{
			var parent = MainWindow.Instance.WindowHandle;
			var hInst = PInvoke.GetModuleHandle(default(PWSTR));
			var szClassName = $"{nameof(ShellPreviewViewModel)}-{Guid.NewGuid()}";
			var szWindowName = $"Preview";

			fixed (char* pszClassName = szClassName)
			{
				_windProc = new(WndProc);
				var pWindProc = Marshal.GetFunctionPointerForDelegate(_windProc);
				var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

				_windowClass = new WNDCLASSEXW()
				{
					cbSize = (uint)sizeof(WNDCLASSEXW),
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

			HRESULT hr = default;
			Guid IID_IDCompositionDevice = typeof(IDCompositionDevice).GUID;
			using ComPtr<ID3D11Device> pD3D11Device = default;
			using ComPtr<ID3D11DeviceContext> pD3D11DeviceContext = default;
			using ComPtr<IDXGIDevice> pDXGIDevice = default;
			using ComPtr<IDCompositionDevice> pDCompositionDevice = default;
			using ComPtr<IUnknown> pControlSurface = default;
			ComPtr<IDCompositionVisual> pChildVisual = default; // Don't dispose this one, it's used by the compositor

			// Create the D3D11 device
			foreach (var driverType in driverTypes)
			{
				hr = PInvoke.D3D11CreateDevice(
					null, driverType, new(nint.Zero),
					D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
					null, /* FeatureLevels */ 0, /* SDKVersion */ 7,
					pD3D11Device.GetAddressOf(), null,
					pD3D11DeviceContext.GetAddressOf());

				if (hr.Succeeded)
					break;
			}

			if (pD3D11Device.IsNull)
				return false;

			// Create the DComp device
			pDXGIDevice.Attach((IDXGIDevice*)pD3D11Device.Get());
			hr = PInvoke.DCompositionCreateDevice(
				pDXGIDevice.Get(),
				&IID_IDCompositionDevice,
				(void**)pDCompositionDevice.GetAddressOf());
			if (hr.Failed)
				return false;

			// Create the visual
			hr = pDCompositionDevice.Get()->CreateVisual(pChildVisual.GetAddressOf());
			hr = pDCompositionDevice.Get()->CreateSurfaceFromHwnd(_hWnd, pControlSurface.GetAddressOf());
			hr = pChildVisual.Get()->SetContent(pControlSurface.Get());
			if (pChildVisual.IsNull || pControlSurface.IsNull)
				return false;

			// Get the compositor and set the visual on it
			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			_contentExternalOutputLink = ContentExternalOutputLink.Create(compositor);

			var target = _contentExternalOutputLink.As<Windows.Win32.Extras.IDCompositionTarget>();
			target.SetRoot((nint)pChildVisual.Get());

			_contentExternalOutputLink.PlacementVisual.Size = new(0, 0);
			_contentExternalOutputLink.PlacementVisual.Scale = new(1 / (float)presenter.XamlRoot.RasterizationScale);
			ElementCompositionPreview.SetElementChildVisual(presenter, _contentExternalOutputLink.PlacementVisual);

			// Commit the all pending DComp commands
			pDCompositionDevice.Get()->Commit();

			var dwAttrib = Convert.ToUInt32(true);

			return
				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib))
				.Succeeded;
		}

		public void UnloadPreview()
		{
			if (_hWnd != HWND.Null)
				PInvoke.DestroyWindow(_hWnd);

			_contentExternalOutputLink?.Dispose();
			_contentExternalOutputLink = null;

			PInvoke.UnregisterClass(_windowClass.lpszClassName, PInvoke.GetModuleHandle(default(PWSTR)));
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
					PInvoke.SetWindowLongPtr(new((nint)_hWnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0);
			}
			else
			{
				PInvoke.SetWindowLongPtr(
					new((nint)_hWnd),
					WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
					(nint)(WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED));

				var dwAttrib = Convert.ToUInt32(true);

				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib));
			}
		}
	}
}

#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
