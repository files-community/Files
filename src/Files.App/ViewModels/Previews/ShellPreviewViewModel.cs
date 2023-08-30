using DirectN;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Content.Private;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using WinRT;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;

namespace Files.App.ViewModels.Previews
{
	public class ShellPreviewViewModel : BasePreviewModel
	{
		public ShellPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> new List<FileProperty>();

		private const string IPreviewHandlerIid = "{8895b1c6-b41f-4c1c-a562-0d564250836f}";
		private static readonly Guid QueryAssociationsClsid = new Guid(0xa07034fd, 0x6caa, 0x4954, 0xac, 0x3f, 0x97, 0xa2, 0x72, 0x16, 0xf9, 0x8a);
		private static readonly Guid IQueryAssociationsIid = Guid.ParseExact("c46ca590-3c3f-11d2-bee6-0000f805ca57", "d");

		PreviewHandler? currentHandler;
		ContentExternalOutputLink? outputLink;
		WindowClass? wCls;
		HWND hwnd = HWND.NULL;
		bool isOfficePreview = false;

		public static Guid? FindPreviewHandlerFor(string extension, IntPtr hwnd)
		{
			if (string.IsNullOrEmpty(extension))
				return null;
			var hr = AssocCreate(QueryAssociationsClsid, IQueryAssociationsIid, out var queryAssoc);
			if (!hr.Succeeded)
				return null;
			try
			{
				if (queryAssoc == null)
					return null;
				queryAssoc.Init(ASSOCF.ASSOCF_INIT_DEFAULTTOSTAR, extension, IntPtr.Zero, hwnd);
				var sb = new StringBuilder(128);
				uint cch = 64;
				queryAssoc.GetString(ASSOCF.ASSOCF_NOTRUNCATE, ASSOCSTR.ASSOCSTR_SHELLEXTENSION, IPreviewHandlerIid, sb, ref cch);
				Debug.WriteLine($"Preview handler for {extension}: {sb}");
				return Guid.Parse(sb.ToString());
			}
			catch
			{
				return null;
			}
			finally
			{
				Marshal.ReleaseComObject(queryAssoc);
			}
		}

		public void SizeChanged(RECT size)
		{
			if (hwnd != HWND.NULL)
				SetWindowPos(hwnd, HWND.HWND_TOP, size.Left, size.Top, size.Width, size.Height, SetWindowPosFlags.SWP_NOACTIVATE);
			if (currentHandler != null)
				currentHandler.ResetBounds(new(0, 0, size.Width, size.Height));
			if (outputLink is not null)
				outputLink.PlacementVisual.Size = new(size.Width, size.Height);
		}

		private IntPtr WndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == (uint)WindowMessage.WM_CREATE)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd.DangerousGetHandle());
				isOfficePreview = new Guid?[] {
					Guid.Parse("84F66100-FF7C-4fb4-B0C0-02CD7FB668FE"),
					Guid.Parse("65235197-874B-4A07-BDC5-E65EA825B718"),
					Guid.Parse("00020827-0000-0000-C000-000000000046") }.Contains(clsid);
				try
				{
					currentHandler = new PreviewHandler(clsid.Value, hwnd.DangerousGetHandle());
					currentHandler.InitWithFileWithEveryWay(Item.ItemPath);
					currentHandler.DoPreview();
				}
				catch (Exception ex)
				{
					UnloadPreview();
				}
			}
			else if (msg == (uint)WindowMessage.WM_DESTROY)
			{
				if (currentHandler is not null)
				{
					currentHandler.UnloadPreview();
					currentHandler = null;
				}
			}
			return DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public void LoadPreview(UIElement presenter)
		{
			var parent = MainWindow.Instance.WindowHandle;

			HINSTANCE hInst = Kernel32.GetModuleHandle();
			wCls = new WindowClass($"{GetType().Name}{Guid.NewGuid()}", hInst, WndProc);
			hwnd = CreateWindowEx(WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_COMPOSITED, wCls.ClassName, "Preview", WindowStyles.WS_CHILD | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_VISIBLE, 0, 0, 0, 0, hWndParent: parent, hInstance: hInst);

			_ = ChildWindowToXaml(parent, presenter);
		}

		private bool ChildWindowToXaml(IntPtr parent, UIElement presenter)
		{
			D3D_DRIVER_TYPE[] driverTypes =
			{
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
			};

			ID3D11Device? d3d11Device = null;
			ID3D11DeviceContext? d3d11DeviceContext = null;
			D3D_FEATURE_LEVEL featureLevelSupported;

			foreach (var driveType in driverTypes)
			{
				var hr = D3D11Functions.D3D11CreateDevice(
					null,
					driveType,
					IntPtr.Zero,
					(uint)D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
					null,
					0,
					7,
					out d3d11Device,
					out featureLevelSupported,
					out d3d11DeviceContext);

				if (hr.IsSuccess)
					break;
			}

			if (d3d11Device is null)
				return false;
			IDXGIDevice dxgiDevice = (IDXGIDevice)d3d11Device;
			if (Functions.DCompositionCreateDevice(dxgiDevice, typeof(IDCompositionDevice).GUID, out var compDevicePtr).IsError)
				return false;
			IDCompositionDevice compDevice = (IDCompositionDevice)Marshal.GetObjectForIUnknown(compDevicePtr);

			if (compDevice.CreateVisual(out var childVisual).IsError ||
				compDevice.CreateSurfaceFromHwnd(hwnd.DangerousGetHandle(), out var controlSurface).IsError ||
				childVisual.SetContent(controlSurface).IsError)
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
			Marshal.Release(compDevicePtr);
			Marshal.ReleaseComObject(dxgiDevice);
			Marshal.ReleaseComObject(d3d11Device);
			Marshal.ReleaseComObject(d3d11DeviceContext);

			return DwmApi.DwmSetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAK, true).Succeeded;
		}

		public void UnloadPreview()
		{
			if (hwnd != HWND.NULL)
				DestroyWindow(hwnd);
			if (outputLink is not null)
				outputLink.Dispose();
			if (wCls is not null)
				UnregisterClass(wCls.ClassName, Kernel32.GetModuleHandle());
		}

		public void PointerEntered(bool onPreview)
		{
			if (onPreview)
			{
				DwmApi.DwmSetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAK, false);
				if (isOfficePreview)
					InteropHelpers.SetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE, 0);
			}
			else
			{
				InteropHelpers.SetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE,
					(nint)(WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_COMPOSITED));
				DwmApi.DwmSetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAK, true);
			}
		}
	}
}
