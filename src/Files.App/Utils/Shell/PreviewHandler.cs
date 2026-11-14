using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.System;
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
	public sealed partial class PreviewHandler : IAsyncDisposable
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
		readonly DispatcherQueueController controller;
		bool init;
		bool shown;
		PreviewHandlerFrame? comSite;
		HWND hwnd;
		IPreviewHandler? previewHandler;
		IPreviewHandlerVisuals? visuals;

		private PreviewHandler(DispatcherQueueController controller, Guid clsid, nint frame)
		{
			this.controller = controller;
			init = false;
			shown = false;
			comSite = new PreviewHandlerFrame(frame);
			hwnd = (HWND)frame;
			try
			{
				SetupHandler(clsid);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public static async Task<PreviewHandler> CreateAsync(Guid clsid, nint frame, string path, CancellationToken cancellationToken)
		{
			var controller = DispatcherQueueController.CreateOnDedicatedThread();
			PreviewHandler? handler = null;
			try
			{
				await InvokeAsync(controller.DispatcherQueue, () =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					handler = new PreviewHandler(controller, clsid, frame);
					cancellationToken.ThrowIfCancellationRequested();
					handler.InitWithFileWithEveryWay(path);
					cancellationToken.ThrowIfCancellationRequested();
					handler.DoPreview();
				});
				return handler!;
			}
			catch
			{
				try
				{
					if (handler is not null)
						await InvokeAsync(controller.DispatcherQueue, handler.Dispose);
				}
				finally
				{
					await controller.ShutdownQueueAsync();
				}
				throw;
			}
		}

		public Task ResizeAsync(RECT bounds)
			=> InvokeAsync(controller.DispatcherQueue, () => ResetBounds(bounds));

		private static Task InvokeAsync(DispatcherQueue dispatcher, Action action)
		{
			var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			if (!dispatcher.TryEnqueue(() =>
			{
				try
				{
					action();
					completion.SetResult();
				}
				catch (Exception ex)
				{
					completion.SetException(ex);
				}
			}))
				completion.SetException(new InvalidOperationException("The preview dispatcher is shutting down."));
			return completion.Task;
		}

		unsafe void SetupHandler(Guid clsid)
		{
			var cannotCreate = "Cannot create class " + clsid.ToString() + " as IPreviewHandler.";
			var cannotCast = "Cannot cast class " + clsid.ToString() + " as IObjectWithSite.";
			var cannotSetSite = "Cannot set site to the preview handler object.";

			// A unique wrapper can release its COM references before the owning STA shuts down.
			Guid interfaceId = typeof(IPreviewHandler).GUID;
			MULTI_QI[] results = [new() { pIID = &interfaceId }];
			HRESULT hr = PInvoke.CoCreateInstanceEx(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, null, results);
			try
			{
				if (hr.Failed)
					throw new COMException(cannotCreate, hr.Value);
				if (results[0].hr.Failed)
					throw new COMException(cannotCreate, results[0].hr.Value);

				previewHandler = UniqueComInterfaceMarshaller<IPreviewHandler>.ConvertToManaged(results[0].pItf);
			}
			finally
			{
				UniqueComInterfaceMarshaller<IPreviewHandler>.Free(results[0].pItf);
			}

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
		private bool InitWithStream(IStream stream, STGM mode)
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
		private bool InitWithItem(IShellItem psi, STGM mode)
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
		private bool InitWithFile(string path, STGM mode)
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
		private bool InitWithFileWithEveryWay(string path)
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
		private HRESULT ResetWindow()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return HRESULT.E_FAIL;
			return previewHandler!.SetWindow(hwnd, new RECT());
		}

		/// <summary>
		/// Calls IPreviewHandler.SetRect.
		/// </summary>
		private bool ResetBounds(RECT previewerBounds)
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
		private bool SetBackground(Color color)
		{
			var hr = visuals?.SetBackgroundColor(ColorRefFromColor(color));
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Sets the text color if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The text color.</param>
		/// <returns>Whether the call succeeds.</returns>
		private bool SetForeground(Color color)
		{
			var hr = visuals?.SetTextColor(ColorRefFromColor(color));
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Sets the font if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="font">The LogFontW reference.</param>
		/// <returns>Whether the call succeeds.</returns>
		private unsafe bool SetFont(nint font)
		{
			var hr = visuals?.SetFont(*(LOGFONTW*)font);
			return hr.HasValue && hr.Value >= 0;
		}

		/// <summary>
		/// Shows the preview if the object has been successfully initialized.
		/// </summary>
		private void DoPreview()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return;
			EnsureNotShown();
			ResetWindow().ThrowOnFailure();
			previewHandler!.DoPreview().ThrowOnFailure();
			shown = true;
		}

		/// <summary>
		/// Tells the preview handler to set focus to itself.
		/// </summary>
		private void Focus()
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
		private nint QueryFocus()
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

		#region Disposal

		public async ValueTask DisposeAsync()
		{
			try
			{
				await InvokeAsync(controller.DispatcherQueue, Dispose);
			}
			finally
			{
				await controller.ShutdownQueueAsync();
			}
		}

		private void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			try
			{
				if (init)
					previewHandler?.Unload();

				// Break the site connection so the handler releases the PreviewHandlerFrame wrapper deterministically
				if (previewHandler is IObjectWithSite objectWithSite)
					objectWithSite.SetSite(null!);
			}
			catch (COMException)
			{
				// RPC errors (e.g. RPC_S_SERVER_UNAVAILABLE) when the prevhost process already exited
			}

			if ((object?)previewHandler is ComObject comObject)
				comObject.FinalRelease();

			previewHandler = null;
			visuals = null;
			comSite = null;
			init = false;
		}

		#endregion

	}
}
