using Files.App.ViewModels.Properties;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
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

		public void SizeChanged(RECT result)
		{
			//if (currentHandler != null)
			//currentHandler.ResetBounds(result);
		}

		private IntPtr WndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == (uint)WindowMessage.WM_NCCREATE)
				return (IntPtr)1;
			else if (msg == (uint)WindowMessage.WM_CREATE)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd.DangerousGetHandle());
				IntPtr pobj = IntPtr.Zero;
				try
				{
					currentHandler = new PreviewHandler(clsid.Value, hwnd.DangerousGetHandle());
					currentHandler.InitWithFileWithEveryWay(Item.ItemPath);
				}
				catch (Exception ex)
				{
					UnloadPreview();
				}
				//currentHandler.SetBackground(((SolidColorBrush)Background).Color);
				//currentHandler.SetForeground(((SolidColorBrush)Foreground).Color);
				currentHandler.DoPreview();
			}
			else if (msg == (uint)WindowMessage.WM_SIZE)
			{
				if (currentHandler != null)
					currentHandler.ResetBounds(new(0, 0, Macros.LOWORD(lParam), Macros.HIWORD(lParam)));
			}
			return DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public void LoadPreview()
		{
			var th = new Thread(() =>
			{
				HINSTANCE hInst = Kernel32.GetModuleHandle();
				var wCls = new WindowClass($"{GetType().Name}+{Guid.NewGuid()}", hInst, WndProc);
				var hwnd = Win32Error.ThrowLastErrorIfInvalid(CreateWindowEx(0, wCls.ClassName, "Preview", WindowStyles.WS_OVERLAPPEDWINDOW, 0, 0, 500, 500, hWndParent: HWND.NULL, hInstance: hInst)).DangerousGetHandle();
				User32.ShowWindow(hwnd, ShowWindowCommand.SW_NORMAL);
				while (GetMessage(out MSG msg) > 0)
				{
					if (msg.message == (uint)WindowMessage.WM_QUIT)
					{
						break;
					}
					TranslateMessage(msg);
					DispatchMessage(msg);
				}
			});
			th.TrySetApartmentState(ApartmentState.STA);
			th.Start();
		}

		private void UnloadPreview()
		{
			if (currentHandler == null)
				return;
			currentHandler.UnloadPreview();
			currentHandler = null;
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
