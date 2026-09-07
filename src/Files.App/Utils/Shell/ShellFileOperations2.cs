// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Queues Shell file operations and exposes their progress callbacks.
	/// </summary>
	public sealed unsafe partial class ShellFileOperations2 : IDisposable
	{
		private IFileOperation? operation;
		private readonly IFileOperationProgressSink sink;
		private readonly uint sinkCookie;
		private FILEOPERATION_FLAGS options = FILEOPERATION_FLAGS.FOF_ALLOWUNDO | FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR;
		private HWND ownerWindow;

		/// <summary>Initializes a new instance of the <see cref="ShellFileOperations2"/> class.</summary>
		/// <param name="owner">The window that owns the modal dialog. This value can be <see langword="null"/>.</param>
		public ShellFileOperations2(HWND owner = default)
		{
			// Use CLSCTX_LOCAL_SERVER to prevent the elevation dialog from appearing behind the app window.
			PInvoke.CoCreateInstance(typeof(FileOperation).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IFileOperation? fileOperation).ThrowOnFailure();
			operation = fileOperation;
			Operation.SetOperationFlags(options).ThrowOnFailure();
			if (!owner.IsNull)
				Operation.SetOwnerWindow(owner).ThrowOnFailure();

			sink = new OperationSink(this);
			Operation.Advise(sink, out sinkCookie).ThrowOnFailure();
		}

		private IFileOperation Operation => operation ?? throw new ObjectDisposedException(nameof(ShellFileOperations2));

		/// <summary>Occurs after the last queued operation is complete.</summary>
		public event EventHandler<ShellFileOpEventArgs>? FinishOperations;

		/// <summary>Occurs after an item has been copied.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PostCopyItem;

		/// <summary>Occurs after an item has been deleted.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PostDeleteItem;

		/// <summary>Occurs after an item has been moved.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PostMoveItem;

		/// <summary>Occurs after a new item has been created.</summary>
		public event EventHandler<ShellFileNewOpEventArgs>? PostNewItem;

		/// <summary>Occurs after an item has been renamed.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PostRenameItem;

		/// <summary>Occurs before an item is copied.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PreCopyItem;

		/// <summary>Occurs before an item is deleted.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PreDeleteItem;

		/// <summary>Occurs before an item is moved.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PreMoveItem;

		/// <summary>Occurs before a new item is created.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PreNewItem;

		/// <summary>Occurs before an item is renamed.</summary>
		public event EventHandler<ShellFileOpEventArgs>? PreRenameItem;

		/// <summary>Occurs before any queued file operation is performed.</summary>
		public event EventHandler? StartOperations;

		/// <summary>Occurs when the operation reports progress.</summary>
		public event ProgressChangedEventHandler? UpdateProgress;

		/// <summary>Gets or sets options that control file operations.</summary>
		public FILEOPERATION_FLAGS Options
		{
			get => options;
			set
			{
				if (value == options)
					return;
				Operation.SetOperationFlags(value).ThrowOnFailure();
				options = value;
			}
		}

		/// <summary>Gets or sets the parent window for progress and error dialogs.</summary>
		public HWND OwnerWindow
		{
			get => ownerWindow;
			set
			{
				Operation.SetOwnerWindow(value).ThrowOnFailure();
				ownerWindow = value;
			}
		}

		/// <summary>Gets the number of queued operations.</summary>
		public int QueuedOperations { get; private set; }

		/// <summary>Queues an item to be copied to a destination folder.</summary>
		public void QueueCopyOperation(ShellItem source, ShellFolder destination, string? newName = null)
		{
			fixed (char* name = newName)
				Operation.CopyItem(source.IShellItem, destination.IShellItem, name, null!).ThrowOnFailure();
			QueuedOperations++;
		}

		/// <summary>Queues an item for deletion.</summary>
		public void QueueDeleteOperation(ShellItem item)
		{
			Operation.DeleteItem(item.IShellItem, null!).ThrowOnFailure();
			QueuedOperations++;
		}

		/// <summary>Queues an item to be moved to a destination folder.</summary>
		public void QueueMoveOperation(ShellItem source, ShellFolder destination, string? newName = null)
		{
			fixed (char* name = newName)
				Operation.MoveItem(source.IShellItem, destination.IShellItem, name, null!).ThrowOnFailure();
			QueuedOperations++;
		}

		/// <summary>Queues a new item to be created in a destination folder.</summary>
		public void QueueNewItemOperation(ShellFolder destination, string name, FileAttributes attributes = FileAttributes.Normal, string? template = null)
		{
			fixed (char* itemName = name, templateName = template)
				Operation.NewItem(destination.IShellItem, (uint)attributes, itemName, templateName, null!).ThrowOnFailure();
			QueuedOperations++;
		}

		/// <summary>Queues an item to be given a new display name.</summary>
		public void QueueRenameOperation(ShellItem source, string newName)
		{
			fixed (char* name = newName)
				Operation.RenameItem(source.IShellItem, name, null!).ThrowOnFailure();
			QueuedOperations++;
		}

		/// <summary>Executes all queued operations.</summary>
		public void PerformOperations()
		{
			Operation.PerformOperations().ThrowOnFailure();
			QueuedOperations = 0;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (operation is null)
				return;

			operation.Unadvise(sinkCookie);
			operation = null;
			GC.SuppressFinalize(this);
		}

		[GeneratedComClass]
		private sealed partial class OperationSink : IFileOperationProgressSink
		{
			private readonly WeakReference<ShellFileOperations2> operationsReference;

			public OperationSink(ShellFileOperations2 operations)
			{
				operationsReference = new(operations);
			}

			public HRESULT StartOperations() => Raise(operations => operations.StartOperations?.Invoke(operations, EventArgs.Empty));

			public HRESULT FinishOperations(HRESULT result) => Raise(operations => operations.FinishOperations?.Invoke(operations, new(result: result)));

			public HRESULT PreRenameItem(uint flags, IShellItem source, PCWSTR newName) => Raise(operations => operations.PreRenameItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, name: newName.ToString())));

			public HRESULT PostRenameItem(uint flags, IShellItem source, PCWSTR newName, HRESULT result, IShellItem newlyCreated) => Raise(operations => operations.PostRenameItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destination: newlyCreated, name: newName.ToString(), result: result)));

			public HRESULT PreMoveItem(uint flags, IShellItem source, IShellItem destinationFolder, PCWSTR newName) => Raise(operations => operations.PreMoveItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destinationFolder, name: newName.ToString())));

			public HRESULT PostMoveItem(uint flags, IShellItem source, IShellItem destinationFolder, PCWSTR newName, HRESULT result, IShellItem newlyCreated) => Raise(operations => operations.PostMoveItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destinationFolder, newlyCreated, newName.ToString(), result)));

			public HRESULT PreCopyItem(uint flags, IShellItem source, IShellItem destinationFolder, PCWSTR newName) => Raise(operations => operations.PreCopyItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destinationFolder, name: newName.ToString())));

			public HRESULT PostCopyItem(uint flags, IShellItem source, IShellItem destinationFolder, PCWSTR newName, HRESULT result, IShellItem newlyCreated) => Raise(operations => operations.PostCopyItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destinationFolder, newlyCreated, newName.ToString(), result)));

			public HRESULT PreDeleteItem(uint flags, IShellItem source) => Raise(operations => operations.PreDeleteItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source)));

			public HRESULT PostDeleteItem(uint flags, IShellItem source, HRESULT result, IShellItem newlyCreated) => Raise(operations => operations.PostDeleteItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, source, destination: newlyCreated, result: result)));

			public HRESULT PreNewItem(uint flags, IShellItem destinationFolder, PCWSTR newName) => Raise(operations => operations.PreNewItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, destinationFolder: destinationFolder, name: newName.ToString())));

			public HRESULT PostNewItem(uint flags, IShellItem destinationFolder, PCWSTR newName, PCWSTR templateName, uint attributes, HRESULT result, IShellItem newItem) => Raise(operations => operations.PostNewItem?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)flags, destinationFolder, newItem, newName.ToString(), result, templateName.ToString(), attributes)));

			public HRESULT UpdateProgress(uint workTotal, uint workCompleted) => Raise(operations => operations.UpdateProgress?.Invoke(operations, new(workTotal is 0 ? 0 : workCompleted * 100.0 / workTotal, null)));

			public HRESULT ResetTimer() => HRESULT.E_NOTIMPL;
			public HRESULT PauseTimer() => HRESULT.E_NOTIMPL;
			public HRESULT ResumeTimer() => HRESULT.E_NOTIMPL;

			private HRESULT Raise(Action<ShellFileOperations2> action)
			{
				if (!operationsReference.TryGetTarget(out ShellFileOperations2? operations))
					return HRESULT.E_FAIL;

				try
				{
					action(operations);
					return HRESULT.S_OK;
				}
				catch (COMException exception)
				{
					return new(exception.ErrorCode);
				}
				catch (Win32Exception exception)
				{
					int error = exception.NativeErrorCode;
					return new(error <= 0 ? error : unchecked((int)(0x80070000u | ((uint)error & 0xFFFFu))));
				}
				catch (Exception exception)
				{
					return new(exception.HResult);
				}
			}
		}

		/// <summary>Arguments supplied to Shell file-operation events.</summary>
		public class ShellFileOpEventArgs : EventArgs
		{
			internal ShellFileOpEventArgs(_TRANSFER_SOURCE_FLAGS flags = _TRANSFER_SOURCE_FLAGS.TSF_NORMAL, IShellItem? source = null, IShellItem? destinationFolder = null, IShellItem? destination = null, string? name = null, HRESULT result = default)
			{
				Flags = flags;
				if (source is not null)
					SourceItem = ShellItem.Open(source);
				if (destinationFolder is not null)
					DestFolder = ShellItem.Open(destinationFolder);
				if (destination is not null)
					DestItem = ShellItem.Open(destination);
				Name = name;
				Result = result;
			}

			/// <summary>Gets the destination folder.</summary>
			public ShellItem? DestFolder { get; }

			/// <summary>Gets the destination item.</summary>
			public ShellItem? DestItem { get; }

			/// <summary>Gets the transfer flags.</summary>
			public _TRANSFER_SOURCE_FLAGS Flags { get; }

			/// <summary>Gets the item name.</summary>
			public string? Name { get; }

			/// <summary>Gets the operation result.</summary>
			public HRESULT Result { get; }

			/// <summary>Gets the source item.</summary>
			public ShellItem? SourceItem { get; }
		}

		/// <summary>Arguments supplied to the <see cref="PostNewItem"/> event.</summary>
		public sealed class ShellFileNewOpEventArgs : ShellFileOpEventArgs
		{
			internal ShellFileNewOpEventArgs(_TRANSFER_SOURCE_FLAGS flags, IShellItem destinationFolder, IShellItem destination, string name, HRESULT result, string? template, uint attributes) : base(flags, null, destinationFolder, destination, name, result)
			{
				TemplateName = template;
				FileAttributes = (FileAttributes)attributes;
			}

			/// <summary>Gets the template name.</summary>
			public string? TemplateName { get; }

			/// <summary>Gets the attributes of the new item.</summary>
			public FileAttributes FileAttributes { get; }
		}

		public delegate void ProgressChangedEventHandler(object? sender, ProgressChangedEventArgs e);

		/// <summary>Provides the percentage and state for a progress update.</summary>
		public sealed class ProgressChangedEventArgs(double progressPercentage, object? userState) : EventArgs
		{
			public double ProgressPercentage { get; } = progressPercentage;
			public object? UserState { get; } = userState;
		}
	}
}
