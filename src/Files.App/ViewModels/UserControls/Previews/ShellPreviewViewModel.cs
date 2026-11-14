// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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
using WNDPROC = Windows.Win32.Extras.ManagedWNDPROC;

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Files.App.ViewModels.Previews
{
	public sealed partial class ShellPreviewViewModel : BasePreviewModel
	{
		// Fields

		ContentExternalOutputLink? _contentExternalOutputLink;
		UIElement? _presenter;
		PreviewHandler? _previewHandler;
		ID3D11Device? _d3d11Device;
		ID3D11DeviceContext? _d3d11DeviceContext;
		IDCompositionDevice? _dCompositionDevice;
		IDCompositionVisual? _childVisual;
		object? _controlSurface;
		WNDCLASSEXW _windowClass;
		WNDPROC _windProc = null!;
		HWND _hWnd = HWND.Null;
		bool _isOfficePreview = false;
		bool _unloaded = true;
		readonly SemaphoreSlim _previewSemaphore = new(1, 1);
		readonly Guid _previewHandlerClassId;
		CancellationToken _loadToken;
		int _resizeVersion;
		unsafe char* _pszClassName;

		public string PreviewHandlerName { get; private set; }

		// Constructor

		public ShellPreviewViewModel(ListedItem item, Guid previewHandlerClassId) : base(item)
		{
			_previewHandlerClassId = previewHandlerClassId;
			PreviewHandlerName = previewHandlerClassId.ToString("B");
		}

		// Methods

		public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var displayName = SafetyExtensions.IgnoreExceptions(() => Registry.GetValue(
				@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers",
				_previewHandlerClassId.ToString("B"), null) as string, App.Logger);
			if (!string.IsNullOrWhiteSpace(displayName))
				PreviewHandlerName = displayName;

			return Task.FromResult<List<FileProperty>>([]);
		}

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
					uint cchOutput = 1024;

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

		public async Task SizeChangedAsync(RECT size, CancellationToken loadToken)
		{
			if (loadToken.IsCancellationRequested)
				return;

			var version = ++_resizeVersion;
			await _previewSemaphore.WaitAsync();
			try
			{
				if (version != _resizeVersion || _unloaded || _hWnd == HWND.Null || _loadToken != loadToken || loadToken.IsCancellationRequested)
					return;

				PInvoke.SetWindowPos(_hWnd, new(0), size.left, size.top, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

				if (_previewHandler is not null)
					await _previewHandler.ResizeAsync(new(0, 0, size.Width, size.Height));

				if (_contentExternalOutputLink is not null)
					_contentExternalOutputLink.PlacementVisual.Size = new(size.Width, size.Height);
			}
			finally
			{
				_previewSemaphore.Release();
			}
		}

		private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			if (msg is PInvoke.WM_DESTROY)
			{
				_hWnd = HWND.Null;
				if (!_unloaded)
				{
					var loadToken = _loadToken;
					_ = SafetyExtensions.IgnoreExceptions(
						() => App.UiDispatcher.EnqueueDeferredAsync(() => UnloadPreviewAsync(loadToken)), App.Logger);
				}
			}

			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public async Task<bool> LoadPreviewAsync(UIElement presenter, CancellationToken loadToken)
		{
			await _previewSemaphore.WaitAsync();
			try
			{
				if (loadToken.IsCancellationRequested)
					return false;

				await UnloadPreviewCoreAsync();
				if (loadToken.IsCancellationRequested)
					return false;

				_loadToken = loadToken;
				_unloaded = false;
				bool loaded = false;
				App.Logger.LogInformation($"ShellPreview.LoadPreview: Item={LogPathHelper.RedactPath(Item?.ItemPath)}");

				try
				{
					if (TryCreatePreviewWindow())
					{
						_previewHandler = await PreviewHandler.CreateAsync(_previewHandlerClassId, _hWnd, Item?.ItemPath!, loadToken);
						loaded = !loadToken.IsCancellationRequested && _hWnd != HWND.Null && ChildWindowToXaml(presenter);
					}
				}
				catch (OperationCanceledException) when (loadToken.IsCancellationRequested)
				{
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, "Failed to load the shell preview.");
				}
				finally
				{
					// Retain the native window until the handler finishes using it.
					if (!loaded || loadToken.IsCancellationRequested)
						await UnloadPreviewCoreAsync();
				}

				return loaded && !loadToken.IsCancellationRequested;
			}
			finally
			{
				_previewSemaphore.Release();
			}
		}

		private unsafe bool TryCreatePreviewWindow()
		{
			var parent = MainWindow.Instance.WindowHandle;

			_isOfficePreview = new Guid?[]
			{
				Guid.Parse("84F66100-FF7C-4fb4-B0C0-02CD7FB668FE"), // preview handler for Word files
				Guid.Parse("65235197-874B-4A07-BDC5-E65EA825B718"), // preview handler for PowerPoint files
				Guid.Parse("00020827-0000-0000-C000-000000000046")  // preview handler for Excel files
			}.Contains(_previewHandlerClassId);

			var hInst = PInvoke.GetModuleHandle(default(PWSTR));
			var szClassName = $"{nameof(ShellPreviewViewModel)}-{Guid.NewGuid()}";
			var szWindowName = $"Preview";

			_pszClassName = (char*)Marshal.StringToHGlobalUni(szClassName);

			_windProc = new(WndProc);
			var pWindProc = Marshal.GetFunctionPointerForDelegate<WNDPROC>(_windProc);
			var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

			_windowClass = new WNDCLASSEXW()
			{
				cbSize = (uint)sizeof(WNDCLASSEXW),
				lpfnWndProc = pfnWndProc,
				hInstance = hInst,
				lpszClassName = _pszClassName,
				style = 0,
				hIcon = default,
				hIconSm = default,
				hCursor = default,
				hbrBackground = default,
				lpszMenuName = null,
				cbClsExtra = 0,
				cbWndExtra = 0,
			};

			if (PInvoke.RegisterClassEx(_windowClass) == 0)
				return false;

			fixed (char* pszWindowName = szWindowName)
			{
				_hWnd = PInvoke.CreateWindowEx(
					WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED,
					_pszClassName,
					pszWindowName,
					WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_CLIPSIBLINGS | WINDOW_STYLE.WS_VISIBLE,
					0, 0, 0, 0,
					new(parent),
					HMENU.Null,
					hInst);
			}

			return _hWnd != HWND.Null;
		}

		private unsafe bool ChildWindowToXaml(UIElement presenter)
		{
			D3D_DRIVER_TYPE[] driverTypes =
			[
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
			];

			HRESULT hr = default;

			// Create the D3D11 device
			foreach (var driverType in driverTypes)
			{
				hr = PInvoke.D3D11CreateDevice(
					null!, driverType, new(nint.Zero),
					D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
					ReadOnlySpan<D3D_FEATURE_LEVEL>.Empty, /* SDKVersion */ 7,
					out _d3d11Device,
					out _d3d11DeviceContext);

				if (hr.Succeeded)
					break;
			}

			if (_d3d11Device is null)
				return false;

			// Create the DComp device
			var pDXGIDevice = (IDXGIDevice)_d3d11Device;
			hr = PInvoke.DCompositionCreateDevice(pDXGIDevice, out _dCompositionDevice);
			if (hr.Failed || _dCompositionDevice is null)
				return false;

			// Create the visual
			hr = _dCompositionDevice.CreateVisual(out _childVisual);
			if (hr.Failed)
				return false;

			hr = _dCompositionDevice.CreateSurfaceFromHwnd(_hWnd, out _controlSurface);
			if (hr.Failed)
				return false;

			hr = _childVisual.SetContent(_controlSurface);
			if (hr.Failed || _childVisual is null || _controlSurface is null)
				return false;

			// Get the compositor and set the visual on it
			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			_contentExternalOutputLink = ContentExternalOutputLink.Create(compositor);

			var target = _contentExternalOutputLink.As<Windows.Win32.Extras.IDCompositionTarget>();
			target.SetRoot(_childVisual);

			_contentExternalOutputLink.PlacementVisual.Size = new(0, 0);
			_contentExternalOutputLink.PlacementVisual.Scale = new(1 / (float)presenter.XamlRoot.RasterizationScale);
			_presenter = presenter;
			ElementCompositionPreview.SetElementChildVisual(presenter, _contentExternalOutputLink.PlacementVisual);

			// Commit the all pending DComp commands
			_dCompositionDevice.Commit();

			var dwAttrib = Convert.ToUInt32(true);

			return
				PInvoke.DwmSetWindowAttribute(
					new((nint)_hWnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib))
				.Succeeded;
		}

		public async Task UnloadPreviewAsync(CancellationToken loadToken)
		{
			await _previewSemaphore.WaitAsync();
			try
			{
				if (_loadToken == loadToken)
					await UnloadPreviewCoreAsync();
			}
			finally
			{
				_previewSemaphore.Release();
			}
		}

		private async Task UnloadPreviewCoreAsync()
		{
			// Main-window teardown and Unloaded can both request cleanup.
			if (_unloaded)
				return;
			_unloaded = true;

			if (_hWnd != HWND.Null)
				PInvoke.ShowWindow(_hWnd, SHOW_WINDOW_CMD.SW_HIDE);

			if (_presenter is not null)
			{
				ElementCompositionPreview.SetElementChildVisual(_presenter, null);
				_presenter = null;
			}

			_contentExternalOutputLink?.Dispose();
			_contentExternalOutputLink = null;

			var handler = _previewHandler;
			_previewHandler = null;
			try
			{
				if (handler is not null)
					await handler.DisposeAsync();
			}
			finally
			{
				// Release the composition chain before destroying its source window.
				ReleaseComObject(ref _childVisual);
				ReleaseComObject(ref _controlSurface);
				ReleaseComObject(ref _dCompositionDevice);
				ReleaseComObject(ref _d3d11DeviceContext);
				ReleaseComObject(ref _d3d11Device);

				DestroyPreviewWindow();
			}
		}

		private unsafe void DestroyPreviewWindow()
		{
			if (_hWnd != HWND.Null)
			{
				var hwnd = _hWnd;
				_hWnd = HWND.Null;
				PInvoke.DestroyWindow(hwnd);
				App.Logger.LogInformation($"ShellPreview.UnloadPreview: HWND={((nint)hwnd)}, Item={LogPathHelper.RedactPath(Item?.ItemPath)}");
			}
			else
				App.Logger.LogInformation($"ShellPreview.UnloadPreview: HWND=, Item={LogPathHelper.RedactPath(Item?.ItemPath)}");

			if (_pszClassName is not null)
			{
				PInvoke.UnregisterClass(_pszClassName, PInvoke.GetModuleHandle(default(PWSTR)));
				Marshal.FreeHGlobal((nint)_pszClassName);
				_pszClassName = null;
			}
		}

		private static void ReleaseComObject<T>(ref T? comInterface) where T : class
		{
			if ((object?)comInterface is ComObject comObject)
				comObject.FinalRelease();

			comInterface = null;
		}

		public unsafe void PointerEntered(bool onPreview)
		{
			if (_unloaded || _loadToken.IsCancellationRequested || _hWnd == HWND.Null)
				return;

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
