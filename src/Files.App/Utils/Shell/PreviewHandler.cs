using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Credits: https://github.com/GeeLaw/PreviewHost/
	/// </summary>
	public sealed partial class PreviewHandler : IDisposable
	{
		[GeneratedComClass]
		public sealed partial class PreviewHandlerFrame : IPreviewHandlerFrame
		{
			nint hwnd;

			public PreviewHandlerFrame(nint frame)
			{
				hwnd = frame;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public unsafe HRESULT GetWindowContext(PREVIEWHANDLERFRAMEINFO* pinfo)
			{
				pinfo->haccel = HACCEL.Null;
				pinfo->cAccelEntries = 0;
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public unsafe HRESULT TranslateAccelerator(MSG* pmsg)
			{
				return HRESULT.S_FALSE;
			}
		}

		static COLORREF ColorRefFromColor(Color color)
		{
			return new COLORREF((((uint)color.B) << 16) | (((uint)color.G) << 8) | color.R);
		}

		bool disposed;
		bool init;
		bool shown;
		PreviewHandlerFrame? comSite;
		HWND hwnd;
		IPreviewHandler? previewHandler;
		IPreviewHandlerVisuals? visuals;

		public PreviewHandler(Guid clsid, nint frame)
		{
			disposed = true;
			init = false;
			shown = false;
			comSite = new PreviewHandlerFrame(frame);
			hwnd = (HWND)frame;
			try
			{
				SetupHandler(clsid);
				disposed = false;
			}
			catch
			{
				previewHandler = null;
				comSite = null;
				throw;
			}
		}

		void SetupHandler(Guid clsid)
		{
			var cannotCreate = "Cannot create class " + clsid.ToString() + " as IPreviewHandler.";
			var cannotCast = "Cannot cast class " + clsid.ToString() + " as IObjectWithSite.";
			var cannotSetSite = "Cannot set site to the preview handler object.";

			HRESULT hr = PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, out previewHandler);

			// See https://blogs.msdn.microsoft.com/adioltean/2005/06/24/when-cocreateinstance-returns-0x80080005-co_e_server_exec_failure/
			// CO_E_SERVER_EXEC_FAILURE also tends to happen when debugging in Visual Studio.
			// Moreover, to create the instance in a server at low integrity level, we need
			// to use another thread with low mandatory label. We keep it simple by creating
			// a same-integrity object.
			//if (hr.Value == E_SERVER_EXEC_FAILURE)
			//	hr = PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, out previewHandlerObject);

			if (hr.Failed)
				throw new COMException(cannotCreate, hr.Value);

			if (previewHandler is not IObjectWithSite objectWithSite)
				throw new COMException(cannotCast, HRESULT.E_NOINTERFACE.Value);

			hr = objectWithSite.SetSite(comSite!);
			if (hr.Failed)
				throw new COMException(cannotSetSite, hr.Value);

			visuals = previewHandler as IPreviewHandlerVisuals;
		}

		/// <summary>
		/// Tries to initialize the preview handler with an IStream.
		/// </summary>
		/// <exception cref="COMException">This exception is thrown if IInitializeWithStream.Initialize fails for reason other than E_NOTIMPL.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if mode is neither Read nor ReadWrite.</exception>
		/// <param name="stream">The IStream interface used to initialize the preview handler.</param>
		/// <param name="mode">The storage mode, must be Read or ReadWrite.</param>
		/// <returns>If the handler supports initialization with IStream, true; otherwise, false.</returns>
		public bool InitWithStream(IStream stream, STGM mode)
		{
			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");

			if (previewHandler is not IInitializeWithStream initializeWithStream)
				return false;

			HRESULT hr = initializeWithStream.Initialize(stream, (uint)mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if (hr.Failed)
				throw new COMException("IInitializeWithStream.Initialize failed.", hr.Value);

			init = true;

			return true;
		}

		/// <summary>
		/// Same as InitWithStream(IStream, STGM).
		/// </summary>
		/// <exception cref="COMException">See InitWithStream(IStream, STGM).</exception>
		/// <exception cref="ArgumentOutOfRangeException">See InitWithStream(IStream, STGM).</exception>
		/// <param name="psi">The IShellItem interface used to initialize the preview handler.</param>
		/// <param name="mode">The storage mode.</param>
		/// <returns>True or false, see InitWithStream(IStream, STGM).</returns>
		public bool InitWithItem(IShellItem psi, STGM mode)
		{
			EnsureNotDisposed();
			EnsureNotInitialized();

			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");

			if (previewHandler is not IInitializeWithItem initializeWithItem)
				return false;

			HRESULT hr = initializeWithItem.Initialize(psi, (uint)mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if (hr.Failed)
				throw new COMException("IInitializeWithItem.Initialize failed.", hr.Value);

			init = true;

			return true;
		}

		/// <summary>
		/// Same as InitWithStream(IStream, STGM).
		/// </summary>
		/// <exception cref="COMException">See InitWithStream(IStream, STGM).</exception>
		/// <exception cref="ArgumentOutOfRangeException">See InitWithStream(IStream, STGM).</exception>
		/// <param name="path">The path to the file.</param>
		/// <param name="mode">The storage mode.</param>
		/// <returns>True or false, see InitWithStream(IStream, STGM).</returns>
		public bool InitWithFile(string path, STGM mode)
		{
			EnsureNotDisposed();
			EnsureNotInitialized();

			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");

			if (previewHandler is not IInitializeWithFile initializeWithFile)
				return false;

			HRESULT hr = initializeWithFile.Initialize(path, (uint)mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if (hr.Failed)
				throw new COMException("IInitializeWithFile.Initialize failed.", hr.Value);

			init = true;

			return true;
		}

		/// <summary>
		/// Tries each way to initialize the object with a file.
		/// </summary>
		/// <param name="path">The file name.</param>
		/// <returns>If initialization was successful, true; otherwise, an exception is thrown.</returns>
		public bool InitWithFileWithEveryWay(string path)
		{
			var exceptions = new List<Exception>();
			// Why should we try IStream first?
			// Because that gives us the best security.
			// If we initialize with string or IShellItem,
			// we have no control over how the preview handler
			// opens the file, which might decide to open the
			// file for read/write exclusively.
			try
			{
				var stream = ItemStreamHelper.IStreamFromPath(path);
				if (stream is not null && InitWithStream(stream, STGM.STGM_READ))
					return true;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}

			// Next try file because that could save us some P/Invokes.
			try
			{
				if (InitWithFile(path, STGM.STGM_READ))
					return true;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
			try
			{
				var shellItem = ItemStreamHelper.IShellItemFromPath(path);
				if (shellItem is not null && InitWithItem(shellItem, STGM.STGM_READ))
					return true;
				if (exceptions.Count == 0)
					throw new NotSupportedException("The object cannot be initialized at all.");
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}

			throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Calls IPreviewHandler.SetWindow.
		/// </summary>
		public bool ResetWindow()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return false;
			var hr = previewHandler.SetWindow(hwnd, new RECT());
			return hr >= 0;
		}

		/// <summary>
		/// Calls IPreviewHandler.SetRect.
		/// </summary>
		public bool ResetBounds(RECT previewerBounds)
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return false;
			var hr = previewHandler.SetRect(previewerBounds);
			return hr >= 0;
		}

		/// <summary>
		/// Sets the background if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The background color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetBackground(Color color)
		{
			var hr = visuals?.SetBackgroundColor(ColorRefFromColor(color));
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Sets the text color if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The text color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetForeground(Color color)
		{
			var hr = visuals?.SetTextColor(ColorRefFromColor(color));
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Sets the font if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="font">The LogFontW reference.</param>
		/// <returns>Whether the call succeeds.</returns>
		public unsafe bool SetFont(nint font)
		{
			var hr = visuals?.SetFont(*(LOGFONTW*)font);
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Shows the preview if the object has been successfully initialized.
		/// </summary>
		public void DoPreview()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return;
			EnsureNotShown();
			ResetWindow();
			previewHandler?.DoPreview();
			shown = true;
		}

		/// <summary>
		/// Tells the preview handler to set focus to itself.
		/// </summary>
		public void Focus()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return;
			EnsureShown();
			previewHandler?.SetFocus();
		}

		/// <summary>
		/// Tells the preview handler to query focus.
		/// </summary>
		/// <returns>The focused window.</returns>
		public nint QueryFocus()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return IntPtr.Zero;
			EnsureShown();

			var hr = previewHandler.QueryFocus(out var result);
			if (hr < 0)
				return IntPtr.Zero;

			return (nint)result;
		}

		void EnsureNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("PreviewHandler");
		}

		void EnsureInitialized()
		{
			if (!init)
				throw new InvalidOperationException("Object must be initialized before calling this method.");
		}

		void EnsureNotInitialized()
		{
			if (init)
				throw new InvalidOperationException("Object is already initialized and cannot be initialized again.");
		}

		void EnsureShown()
		{
			if (!shown)
				throw new InvalidOperationException("The preview handler must be shown to call this method.");
		}

		void EnsureNotShown()
		{
			if (shown)
				throw new InvalidOperationException("The preview handler must not be shown to call this method.");
		}

		#region IDisposable pattern

		~PreviewHandler()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			init = false;

			previewHandler?.Unload();
			comSite = null;

			GC.SuppressFinalize(this);
		}

		#endregion

	}
}
