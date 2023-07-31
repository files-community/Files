using DirectN;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml.Controls;
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
		ContentExternalOutputLink? m_outputLink;
		HWND hwnd = HWND.NULL;

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
				User32.SetWindowPos(hwnd, HWND.HWND_TOP, size.Left, size.Top, size.Width, size.Height, SetWindowPosFlags.SWP_NOACTIVATE);
			if (currentHandler != null)
				currentHandler.ResetBounds(new(0, 0, size.Width, size.Height));
			if (m_outputLink is not null)
				m_outputLink.PlacementVisual.Size = new(size.Width, size.Height);
		}

		private IntPtr WndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == (uint)WindowMessage.WM_NCCREATE)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd.DangerousGetHandle());
				try
				{
					currentHandler = new PreviewHandler(clsid.Value, hwnd.DangerousGetHandle());
					currentHandler.InitWithFileWithEveryWay(Item.ItemPath);
					//currentHandler.SetBackground();
					//currentHandler.SetForeground();
					currentHandler.DoPreview();
				}
				catch (Exception ex)
				{
					UnloadPreview();
				}
			}
			else if (msg == (uint)WindowMessage.WM_CLOSE)
			{
				if (currentHandler is not null)
				{
					currentHandler.UnloadPreview();
					currentHandler = null;
				}
			}
			else if (msg == (uint)WindowMessage.WM_DESTROY)
			{
				User32.PostQuitMessage(0);
			}
			return DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public async Task LoadPreviewAsync(ContentPresenter presenter)
		{
			UnloadPreview();

			var parent = MainWindow.Instance.WindowHandle;

			var windowCreated = new TaskCompletionSource();

			var th = new Thread(() =>
			{
				HINSTANCE hInst = Kernel32.GetModuleHandle();
				var wCls = new WindowClass($"{GetType().Name}{Guid.NewGuid()}", hInst, WndProc);
				hwnd = CreateWindowEx(WindowStylesEx.WS_EX_LAYERED, wCls.ClassName, "Preview", WindowStyles.WS_CHILD | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_VISIBLE, 0, 0, 0, 0, hWndParent: parent, hInstance: hInst);
				windowCreated.TrySetResult();

				if (hwnd != HWND.NULL)
				{
					while (GetMessage(out Vanara.PInvoke.MSG msg) > 0)
					{
						TranslateMessage(msg);
						DispatchMessage(msg);
					}
				}

				User32.UnregisterClass(wCls.ClassName, hInst);
			});
			th.TrySetApartmentState(ApartmentState.STA);
			th.Start();

			await windowCreated.Task;

			var hr = ChildWindowToXaml(parent, presenter);
		}

		private bool ChildWindowToXaml(IntPtr parent, ContentPresenter presenter)
		{
			D3D_DRIVER_TYPE[] driverTypes =
			{
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
			};

			ID3D11Device? d3d11Device = null;
			D3D_FEATURE_LEVEL featureLevelSupported;
			ID3D11DeviceContext d3d11DeviceContext;

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
			if (Functions.DCompositionCreateDevice(dxgiDevice, typeof(IDCompositionDevice).GUID, out var dcompDev).IsError)
				return false;
			IDCompositionDevice m_pDevice = (IDCompositionDevice)Marshal.GetObjectForIUnknown(dcompDev);

			if (m_pDevice.CreateVisual(out var m_pControlChildVisual).IsError ||
				m_pDevice.CreateSurfaceFromHwnd(hwnd.DangerousGetHandle(), out var m_pControlsurfaceTile).IsError ||
				m_pControlChildVisual.SetContent(m_pControlsurfaceTile).IsError)
				return false;

			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			m_outputLink = ContentExternalOutputLink.Create(compositor);
			IDCompositionTarget target = m_outputLink.As<IDCompositionTarget>();
			target.SetRoot(m_pControlChildVisual);

			m_outputLink.PlacementVisual.Size = new(0, 0);
			m_outputLink.PlacementVisual.Scale = new(1/(float)presenter.XamlRoot.RasterizationScale);
			ElementCompositionPreview.SetElementChildVisual(presenter, m_outputLink.PlacementVisual);

			m_pDevice.Commit();

			return DwmApi.DwmSetWindowAttribute<bool>(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAK, true).Succeeded;
		}

		public void UnloadPreview()
		{
			var parent = MainWindow.Instance.WindowHandle;
			if (hwnd == HWND.NULL)
				hwnd = User32.EnumChildWindows(parent).FirstOrDefault(x =>
				{
					var sb = new StringBuilder(512);
					return User32.GetClassName(x, sb, 512) > 0 && sb.ToString().StartsWith(GetType().Name);
				});
			if (hwnd == HWND.NULL)
				return;
			User32.PostMessage(hwnd, (uint)WindowMessage.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
		}

		public void GotFocus(Action focusPresenter)
		{
			if (currentHandler != null)
			{
				currentHandler.Focus();
				if (currentHandler.QueryFocus() == IntPtr.Zero)
				{
					var old = currentHandler;
					currentHandler = null;
					focusPresenter();
					currentHandler = old;
				}
			}
		}
	}
}
