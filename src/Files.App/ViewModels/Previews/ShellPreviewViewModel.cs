using Files.App.ViewModels.Properties;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
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

		const string IPreviewHandlerIid = "{8895b1c6-b41f-4c1c-a562-0d564250836f}";
		static readonly Guid QueryAssociationsClsid = new Guid(0xa07034fd, 0x6caa, 0x4954, 0xac, 0x3f, 0x97, 0xa2, 0x72, 0x16, 0xf9, 0x8a);
		static readonly Guid IQueryAssociationsIid = Guid.ParseExact("c46ca590-3c3f-11d2-bee6-0000f805ca57", "d");

		PreviewHandler? currentHandler;
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
					//currentHandler.SetBackground(((SolidColorBrush)Background).Color);
					//currentHandler.SetForeground(((SolidColorBrush)Foreground).Color);
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

		public void LoadPreview()
		{
			UnloadPreview();

			var parent = MainWindow.Instance.WindowHandle;

			foreach (var wnd in User32.EnumChildWindows(parent))
			{
				var styleChild = (WindowStyles)User32.GetWindowLong(wnd, WindowLongFlags.GWL_STYLE);
				if (!styleChild.HasFlag(WindowStyles.WS_CLIPSIBLINGS))
					User32.SetWindowLong(wnd, WindowLongFlags.GWL_STYLE, (nint)(styleChild | WindowStyles.WS_CLIPSIBLINGS));
			}
			var styleParent = (WindowStyles)User32.GetWindowLong(parent, WindowLongFlags.GWL_STYLE);
			User32.SetWindowLong(parent, WindowLongFlags.GWL_STYLE, (nint)(styleParent | WindowStyles.WS_CLIPCHILDREN));

			var th = new Thread(() =>
			{
				HINSTANCE hInst = Kernel32.GetModuleHandle();
				var wCls = new WindowClass($"{GetType().Name}{Guid.NewGuid()}", hInst, WndProc);
				hwnd = Win32Error.ThrowLastErrorIfInvalid(CreateWindowEx(0, wCls.ClassName, "Preview", WindowStyles.WS_CHILD | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_VISIBLE, 0, 0, 0, 0, hWndParent: parent, hInstance: hInst));

				while (GetMessage(out MSG msg) > 0)
				{
					TranslateMessage(msg);
					DispatchMessage(msg);
				}

				User32.UnregisterClass(wCls.ClassName, hInst);
			});
			th.TrySetApartmentState(ApartmentState.STA);
			th.Start();
		}

		private void UnloadPreview()
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
			User32.PostMessage(hwnd, (uint)WindowMessage.WM_CLOSE, 0, 0);
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
