// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	/// <summary>
	/// Handles bulk file operations in Windows, such as copy, move, delete, create, and rename, supporting progress tracking and event notifications.
	/// </summary>
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		// Fields

		private readonly ComPtr<IFileOperation> _pFileOperation;
		private readonly ComPtr<IFileOperationProgressSink> _pProgressSink;
		private readonly uint _progressSinkCookie;

		// Events

		/// <summary>An event that is triggered when bulk operations are completed.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? OperationsFinished;

		/// <summary>An event that is triggered when an item is copied during bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCopied;

		/// <summary>An event that is triggered when an item is deleted during bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemDeleted;

		/// <summary>An event that is triggered when an item is moved during bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemMoved;

		/// <summary>An event that is triggered when a new item is created.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCreated;

		/// <summary>An event that is triggered when an item is renamed.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemRenamed;

		/// <summary>An event that occurs when an item is being copied during bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCopying;

		/// <summary>An event that is triggered when an item is being deleted.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemDeleting;

		/// <summary>An event that is triggered when an item is being moved in bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemMoving;

		/// <summary>An event that is triggered when an item is being created in bulk operations.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemCreating;

		/// <summary>An event that is triggered when an item is being renamed.</summary>
		public event EventHandler<WindowsBulkOperationsEventArgs>? ItemRenaming;

		/// <summary>An event that is triggered when operations start.</summary>
		public event EventHandler? OperationsStarted;

		/// <summary>An event that is triggered to indicate progress updates.</summary>
		public event ProgressChangedEventHandler? ProgressUpdated;

		// Constructor

		/// <summary>
		/// Initializes a <see cref="WindowsBulkOperations"/> instance for file operations on Windows.
		/// </summary>
		/// <param name="owner">Specifies the window handle that will own the file operation dialogs.</param>
		/// <param name="flags">Defines the behavior of the file operation, such as allowing undo and suppressing directory confirmation.</param>
		public unsafe WindowsBulkOperations(HWND ownerHWnd = default, FILEOPERATION_FLAGS flags = FILEOPERATION_FLAGS.FOF_ALLOWUNDO | FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR)
		{
			var clsid = typeof(FileOperation).GUID;
			var iid = typeof(IFileOperation).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&clsid,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&iid,
				(void**)_pFileOperation.GetAddressOf())
				.ThrowIfFailedOnDebug();

			if (ownerHWnd != default)
				hr = _pFileOperation.Get()->SetOwnerWindow(ownerHWnd).ThrowIfFailedOnDebug();

			hr = _pFileOperation.Get()->SetOperationFlags(flags).ThrowIfFailedOnDebug();

			_pProgressSink.Attach((IFileOperationProgressSink*)WindowsBulkOperationsSink.Create(this));
			hr = _pFileOperation.Get()->Advise(_pProgressSink.Get(), out var progressSinkCookie).ThrowIfFailedOnDebug();
			_progressSinkCookie = progressSinkCookie;
		}

		/// <summary>
		/// Queues a copy operation.
		/// </summary>
		/// <param name="targetItem"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="copyName"></param>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT QueueCopyOperation(WindowsStorable targetItem, WindowsFolder destinationFolder, string? copyName)
		{
			fixed (char* pszCopyName = copyName)
				return _pFileOperation.Get()->CopyItem(targetItem.ThisPtr.Get(), destinationFolder.ThisPtr.Get(), pszCopyName, _pProgressSink.Get());
		}

		/// <summary>
		/// Queues a delete operation.
		/// </summary>
		/// <param name="targetItem"></param>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT QueueDeleteOperation(WindowsStorable targetItem)
		{
			return _pFileOperation.Get()->DeleteItem(targetItem.ThisPtr.Get(), _pProgressSink.Get());
		}

		/// <summary>
		/// Queues a move operation.
		/// </summary>
		/// <param name="targetItem"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="newName"></param>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT QueueMoveOperation(WindowsStorable targetItem, WindowsFolder destinationFolder, string? newName)
		{
			fixed (char* pszNewName = newName)
				return _pFileOperation.Get()->MoveItem(targetItem.ThisPtr.Get(), destinationFolder.ThisPtr.Get(), pszNewName, null);
		}

		/// <summary>
		/// Queues a create operation.
		/// </summary>
		/// <param name="destinationFolder"></param>
		/// <param name="fileAttributes"></param>
		/// <param name="name"></param>
		/// <param name="templateName"></param>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT QueueCreateOperation(WindowsFolder destinationFolder, FILE_FLAGS_AND_ATTRIBUTES fileAttributes, string name, string? templateName)
		{
			fixed (char* pszName = name, pszTemplateName = templateName)
				return _pFileOperation.Get()->NewItem(destinationFolder.ThisPtr.Get(), (uint)fileAttributes, pszName, pszTemplateName, _pProgressSink.Get());
		}

		/// <summary>
		/// Queues a rename operation.
		/// </summary>
		/// <param name="targetItem"></param>
		/// <param name="newName"></param>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT QueueRenameOperation(WindowsStorable targetItem, string newName)
		{
			fixed (char* pszNewName = newName)
				return _pFileOperation.Get()->RenameItem(targetItem.ThisPtr.Get(), pszNewName, _pProgressSink.Get());
		}

		/// <summary>
		/// Performs the all queued operations.
		/// </summary>
		/// <returns>If this method succeeds, it returns <see cref="HRESULT.S_OK"/>. Otherwise, it returns an <see cref="HRESULT"/> error code.</returns>
		public unsafe HRESULT PerformAllOperations()
		{
			return _pFileOperation.Get()->PerformOperations();
		}

		// Disposer

		/// <inheritdoc/>
		public unsafe void Dispose()
		{
			if (!_pProgressSink.IsNull)
				_pFileOperation.Get()->Unadvise(_progressSinkCookie);

			_pFileOperation.Dispose();
			_pProgressSink.Dispose();
		}
	}
}
