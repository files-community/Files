// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		// Fields

		private readonly ComPtr<IFileOperation> _pFileOperation;
		private readonly ComPtr<IFileOperationProgressSink> _pProgressSink;
		private readonly uint _progressSinkCookie;

		private int _disposedValue = 0;

		// Events

		public event EventHandler<WindowsBulkOperationsEventArgs>? OperationsFinished;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCopied;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemDeleted;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemMoved;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCreated;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemRenamed;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCopying;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemDeleting;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemMoving;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCreating;
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemRenaming;
		public event EventHandler? OperationsStarted;
		public event ProgressChangedEventHandler? ProgressUpdated;

		// Constructor

		public unsafe WindowsBulkOperations(HWND owner = default, FILEOPERATION_FLAGS flags = FILEOPERATION_FLAGS.FOF_ALLOWUNDO | FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR)
		{
			var clsid = typeof(FileOperation).GUID;
			var iid = typeof(IFileOperation).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&clsid,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&iid,
				(void**)_pFileOperation.GetAddressOf());

			if (owner != default)
				hr = _pFileOperation.Get()->SetOwnerWindow(owner);

			hr = _pFileOperation.Get()->SetOperationFlags(flags);

			_pProgressSink.Attach((IFileOperationProgressSink*)WindowsBulkOperationsSink.Create(this));
			hr = _pFileOperation.Get()->Advise(_pProgressSink.Get(), out var progressSinkCookie);
			_progressSinkCookie = progressSinkCookie;
		}

		public unsafe HRESULT QueueCopyOperation(ComPtr<IShellItem> psiItem, ComPtr<IShellItem> psiDestinationFolder, PCWSTR pszCopyName)
		{
			HRESULT hr = default;

			hr = _pFileOperation.Get()->CopyItem(psiItem.Get(), psiDestinationFolder.Get(), pszCopyName, _pProgressSink.Get());
			return hr;
		}

		public unsafe HRESULT QueueDeleteOperation(ComPtr<IShellItem> psiItem)
		{
			HRESULT hr = default;

			hr = _pFileOperation.Get()->DeleteItem(psiItem.Get(), _pProgressSink.Get());
			return hr;
		}

		public unsafe HRESULT QueueMoveOperation(WindowsStorable targetItem, WindowsFolder destinationFolder, string? newName)
		{
			HRESULT hr = default;

			fixed (char* pszNewName = newName)
				hr = _pFileOperation.Get()->MoveItem(targetItem.ThisPtr.Get(), destinationFolder.ThisPtr.Get(), pszNewName, null);

			return hr;
		}

		public unsafe HRESULT QueueCreateOperation(ComPtr<IShellItem> psiDestinationFolder, uint dwFileAttributes, PCWSTR pszName, PCWSTR pszTemplateName)
		{
			HRESULT hr = default;

			hr = _pFileOperation.Get()->NewItem(psiDestinationFolder.Get(), dwFileAttributes, pszName, pszTemplateName, _pProgressSink.Get());
			return hr;
		}

		public unsafe HRESULT QueueRenameOperation(ComPtr<IShellItem> psiItem, PCWSTR pszNewName)
		{
			HRESULT hr = default;

			hr = _pFileOperation.Get()->RenameItem(psiItem.Get(), pszNewName, _pProgressSink.Get());
			return hr;
		}

		public unsafe HRESULT PerformAllOperations()
		{
			HRESULT hr = default;

			hr = _pFileOperation.Get()->PerformOperations();
			return hr;
		}

		public unsafe void Dispose()
		{
			_pFileOperation.Get()->Unadvise(_progressSinkCookie);
			_pFileOperation.Dispose();
			_pProgressSink.Dispose();
		}
	}
}
