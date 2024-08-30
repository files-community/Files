// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using LibGit2Sharp;
using System.Runtime.InteropServices;
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
	/// Provides a set of functionalities to interact with Windows preview handlers.
	/// </summary>
	/// <remarks>
	/// Credit: <a href="https://github.com/GeeLaw/PreviewHost"/>
	/// </remarks>
	public unsafe sealed class PreviewHandler : IDisposable
	{
		// Fields

		private readonly IPreviewHandlerFrame.Interface _previewHandlerFrame;
		private readonly IPreviewHandler* _pPreviewHandler;
		private readonly IPreviewHandlerVisuals* _previewHandlerVisuals;
		private readonly HWND _hWnd;
		private bool _disposed;
		private bool _initialized;
		private bool _shown;

		// Initializer

		/// <summary>
		/// Initializes an instance of <see cref="PreviewHandler"/> class.
		/// </summary>
		/// <param name="clsid"></param>
		/// <param name="frame"></param>
		public PreviewHandler(Guid clsid, HWND frame)
		{
			_disposed = true;
			_initialized = false;
			_shown = false;
			_hWnd = frame;

			// Initialize preview handler's frame
			_previewHandlerFrame = new CPreviewHandlerFrame(frame);

			try
			{
				HRESULT hr = PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IPreviewHandler* pPreviewHandler);
				if (hr.Value < 0)
					throw new COMException("Cannot create class " + clsid.ToString() + " as IPreviewHandler.", hr.Value);
				else if (pPreviewHandler is null)
					throw new COMException("Cannot create class " + clsid.ToString() + " as IPreviewHandler.");

				_pPreviewHandler = pPreviewHandler;

				Debug.WriteLine($"IPreviewHandler was successfully initialized from {clsid:B}.");

				// Get IObjectWithSite
				ComPtr<IObjectWithSite> pObjectWithSite = default;
				_pPreviewHandler->QueryInterface(typeof(IObjectWithSite).GUID, out *(void**)pObjectWithSite.GetAddressOf());
				if (pObjectWithSite.IsNull)
					throw new COMException("Cannot cast class " + clsid.ToString() + " as IObjectWithSite.");

				// Set site
				var pPreviewHandlerFrame = Marshal.GetIUnknownForObject(_previewHandlerFrame);
				hr = pObjectWithSite.Get()->SetSite((IUnknown*)pPreviewHandlerFrame);
				if (hr.Value < 0)
					throw new COMException("Cannot set site to the preview handler object.", hr.Value);

				Debug.WriteLine($"Site IPreviewHandlerFrame was successfully set to IPreviewHandler.");

				// Get IPreviewHandlerVisuals
				IPreviewHandlerVisuals* previewHandlerVisuals = default;
				_pPreviewHandler->QueryInterface(typeof(IPreviewHandlerVisuals).GUID, out *(void**)&previewHandlerVisuals);
				if (previewHandlerVisuals == null)
					throw new COMException("Cannot cast class " + clsid.ToString() + " as IPreviewHandlerVisuals.");

				_previewHandlerVisuals = previewHandlerVisuals;

				Debug.WriteLine($"IPreviewHandlerVisuals was successfully queried from IPreviewHandler.");

				_disposed = false;
			}
			catch
			{
				if (_pPreviewHandler is not null)
				{
					_pPreviewHandler->Release();
					_pPreviewHandler = null;
				}

				throw;
			}
		}

		// Methods

		/// <summary>
		/// Initializes the preview handler with file.
		/// </summary>
		/// <param name="path">The file name to use to initialize the preview handler.</param>
		/// <returns>True If succeeded, otherwise, false.</returns>
		public bool Initialize(string path)
		{
			List<Exception> exceptions = [];

			// We try IStream first because this gives us the best security.
			// If we initialize with string or IShellItem, we have no control over
			// how the preview handler opens the file, which might decide to open the file for read/write exclusively.
			try
			{
				using ComPtr<IStream> pStream = default;
				HRESULT hr = PInvoke.SHCreateStreamOnFileEx(path, (uint)(STGM.STGM_READ | STGM.STGM_FAILIFTHERE | STGM.STGM_SHARE_DENY_NONE), 0, false, null, pStream.GetAddressOf());
				if (hr.Value < 0)
					throw new InvalidComObjectException($"SHCreateItemFromParsingName failed to get IShellItem for preview handling with the error {hr.Value:X}.");

				if (!pStream.IsNull)
				{
					ObjectDisposedException.ThrowIf(_disposed, this);

					if (_initialized)
						throw new InvalidOperationException("Preview handler is already initialized and cannot be initialized again.");

					using ComPtr<IInitializeWithStream> pInitializeWithStream = default;
					_pPreviewHandler->QueryInterface(typeof(IInitializeWithStream).GUID, out *(void**)pInitializeWithStream.GetAddressOf());
					if (pInitializeWithStream.IsNull)
						throw new COMException($"{nameof(IInitializeWithStream)} could not queried from IPreviewHandler.");

					hr = pInitializeWithStream.Get()->Initialize(pStream.Get(), (uint)STGM.STGM_READ);
					if (hr == HRESULT.E_NOTIMPL)
						throw new NotImplementedException($"{nameof(IInitializeWithStream)}.Initialize() is not implemented.");
					else if ((int)hr < 0)
						throw new COMException($"{nameof(IInitializeWithStream)}.Initialize() failed.", (int)hr);

					_initialized = true;

					Debug.WriteLine($"Preview handler was successfully initialized with {nameof(IInitializeWithStream)}.");

					return true;
				}
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}

			try
			{
				ObjectDisposedException.ThrowIf(_disposed, this);

				if (_initialized)
					throw new InvalidOperationException("Preview handler is already initialized and cannot be initialized again.");

				using ComPtr<IInitializeWithFile> pInitializeWithFile = default;
				_pPreviewHandler->QueryInterface(typeof(IInitializeWithFile).GUID, out *(void**)pInitializeWithFile.GetAddressOf());
				if (pInitializeWithFile.IsNull)
					throw new COMException($"{nameof(IInitializeWithFile)} could not queried from IPreviewHandler.");

				HRESULT hr = pInitializeWithFile.Get()->Initialize(path, (uint)STGM.STGM_READ);
				if (hr == HRESULT.E_NOTIMPL)
					throw new NotImplementedException($"{nameof(IInitializeWithFile)}.Initialize() is not implemented.");
				else if ((int)hr < 0)
					throw new COMException($"{nameof(IInitializeWithFile)}.Initialize() failed.", (int)hr);

				_initialized = true;

				Debug.WriteLine($"Preview handler was successfully initialized with {nameof(IInitializeWithFile)}.");

				return true;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}

			try
			{
				using ComPtr<IShellItem> pShellItem = default;
				HRESULT hr = PInvoke.SHCreateItemFromParsingName(path, null, typeof(IShellItem).GUID, out *(void**)&pShellItem);
				if (hr.Value < 0)
					throw new InvalidComObjectException($"SHCreateItemFromParsingName failed to get IShellItem for preview handling with the error {hr.Value:X}.");

				if (!pShellItem.IsNull)
				{
					ObjectDisposedException.ThrowIf(_disposed, this);

					if (_initialized)
						throw new InvalidOperationException("Preview handler is already initialized and cannot be initialized again.");

					using ComPtr<IInitializeWithItem> pInitializeWithItem = default;
					_pPreviewHandler->QueryInterface(typeof(IInitializeWithItem).GUID, out *(void**)pInitializeWithItem.GetAddressOf());
					if (pInitializeWithItem.IsNull)
						throw new COMException($"{nameof(IInitializeWithItem)} could not queried from IPreviewHandler.");

					hr = pInitializeWithItem.Get()->Initialize(pShellItem.Get(), (uint)STGM.STGM_READ);
					if (hr == HRESULT.E_NOTIMPL)
						throw new NotImplementedException($"{nameof(IInitializeWithItem)}.Initialize() is not implemented.");
					else if ((int)hr < 0)
						throw new COMException($"{nameof(IInitializeWithItem)}.Initialize() failed.", (int)hr);

					_initialized = true;

					Debug.WriteLine($"Preview handler was successfully initialized with {nameof(IInitializeWithItem)}.");

					return true;
				}

				if (exceptions.Count is 0)
					throw new NotSupportedException("Preview handler could not be initialized at all.");
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}

			Debug.WriteLine($"Preview handler could not be initialized at all.");

			throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Loads the preview data and renders the preview.
		/// </summary>
		public void DoPreview()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if (!_initialized)
			//	throw new InvalidOperationException("Object must be initialized before calling this method.");
				return;

			if (_shown)
				throw new InvalidOperationException("The preview handler must not be shown to call this method.");

			bool res = ResetWindow();

			Debug.WriteLine($"Window of the preview handler" + (res ? "was successfully reset." : "failed to be reset."));

			_pPreviewHandler->DoPreview();

			_shown = true;

			Debug.WriteLine($"IPreviewHandler.DoPreview was successfully done.");
		}

		/// <summary>
		/// Unloads the preview handler and disposes this instance.
		/// </summary>
		public void UnloadPreview()
		{
			Dispose(true);
		}

		/// <summary>
		/// Calls IPreviewHandler.SetWindow.
		/// </summary>
		public bool ResetWindow()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if (!_initialized)
			//	throw new InvalidOperationException("Object must be initialized before calling this method.");
				return false;

			HRESULT hr = _pPreviewHandler->SetWindow(_hWnd, new RECT());
			return hr.Value >= 0;
		}

		/// <summary>
		/// Calls IPreviewHandler.SetRect.
		/// </summary>
		public bool ResetBounds(RECT previewerBounds)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			//if (!_initialized)
			//	throw new InvalidOperationException("Object must be initialized before calling this method.");

			if (!_initialized)
				return false;

			HRESULT hr = _pPreviewHandler->SetRect(previewerBounds);
			return (int)hr >= 0;
		}

		/// <summary>
		/// Sets the background if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The background color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetBackground(Color color)
		{
			HRESULT hr = _previewHandlerVisuals->SetBackgroundColor(new(ConvertColorToColorRef(color)));
			return hr.Value >= 0;
		}

		/// <summary>
		/// Sets the text color if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The text color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetForeground(Color color)
		{
			HRESULT hr = _previewHandlerVisuals->SetTextColor(new(ConvertColorToColorRef(color)));
			return hr.Value >= 0;
		}

		/// <summary>
		/// Sets the font if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="font">The LogFontW reference.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetFont(ref LOGFONTW font)
		{
			HRESULT hr = _previewHandlerVisuals->SetFont(font);
			return hr.Value >= 0;
		}

		/// <summary>
		/// Tells the preview handler to set focus to itself.
		/// </summary>
		public void Focus()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if (!_initialized)
			//	throw new InvalidOperationException("Object must be initialized before calling this method.");
				return;

			if (!_shown)
				throw new InvalidOperationException("The preview handler must be shown to call this method.");

			_pPreviewHandler->SetFocus();
		}

		/// <summary>
		/// Tells the preview handler to query focus.
		/// </summary>
		/// <returns>The focused window.</returns>
		public nint QueryFocus()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if (!_initialized)
			//	throw new InvalidOperationException("Object must be initialized before calling this method.");
				return nint.Zero;

			if (!_shown)
				throw new InvalidOperationException("The preview handler must be shown to call this method.");

			HRESULT hr = _pPreviewHandler->QueryFocus(out HWND hWnd);
			if (hr.Value < 0)
				return nint.Zero;

			return hWnd.Value;
		}

		private uint ConvertColorToColorRef(Color color)
		{
			return (((uint)color.B) << 16) | (((uint)color.G) << 8) | ((uint)color.R);
		}

		// Disposers

		~PreviewHandler()
		{
			Dispose(false);
		}

		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;
			_initialized = false;

			if (disposing)
			{
				_pPreviewHandler->Unload();
				_pPreviewHandler->Release();
			}
			else
			{
				// We're in the finalizer.
				// Field previewHandler might have been finalized at this point.
				// Get a new RCW.

				//var phObject = Marshal.GetUniqueObjectForIUnknown(_pPreviewHandler);
				//var ph = phObject as IPreviewHandler;
				//if (ph != null)
				//	ph.Unload();

				//Marshal.ReleaseComObject(phObject);
			}

			_pPreviewHandler->Release();

			Debug.WriteLine($"Preview handler was successfully disposed.");
		}

		// Private class

		private unsafe class CPreviewHandlerFrame : IPreviewHandlerFrame.Interface
		{
			private bool _disposed = false;
			private readonly HWND _hWnd = default;

			public CPreviewHandlerFrame(HWND frame)
			{
				_hWnd = frame;
			}

			public void Dispose()
			{
				_disposed = true;
			}

			public HRESULT GetWindowContext(PREVIEWHANDLERFRAMEINFO* pInfo)
			{
				pInfo->haccel = HACCEL.Null;
				pInfo->cAccelEntries = 0u;

				if (_disposed)
					return HRESULT.E_FAIL; // Disposed already

				return HRESULT.S_OK;
			}

			public HRESULT TranslateAccelerator(MSG* pMsg)
			{
				if (_disposed)
					return HRESULT.E_FAIL; // Disposed already

				return HRESULT.S_FALSE;
			}
		}
	}
}
