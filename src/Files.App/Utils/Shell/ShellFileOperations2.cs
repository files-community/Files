using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.Windows.Shell.ShellFileOperations;

namespace Vanara.Windows.Shell;

public partial class ShellFileOperations2 : IDisposable
{
	private const OperationFlags defaultOptions = OperationFlags.AllowUndo | OperationFlags.NoConfirmMkDir;
	private int disposedValue = 0;
	private IFileOperation op;
	private OperationFlags opFlags = defaultOptions;
	private HWND owner;
	private readonly IFileOperationProgressSink sink;
	private readonly uint sinkCookie;

	public ShellFileOperations2(HWND owner = default)
	{
		// EDIT: use CoCreateInstance to explicitly create object with CLSCTX_LOCAL_SERVER (fixes #13229, hides UAC)
		Ole32.CoCreateInstance(typeof(CFileOperations).GUID, null, Ole32.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IFileOperation).GUID, out var opObj);
		op = (IFileOperation)opObj;
		//op = new IFileOperation();
		if (owner != default)
		{
			op.SetOwnerWindow(owner);
		}

		sink = new OpSink(this);
		sinkCookie = op.Advise(sink);
	}

	public ShellFileOperations2(IFileOperation operation)
	{
		op = operation;
		sink = new OpSink(this);
		sinkCookie = op.Advise(sink);
	}

	~ShellFileOperations2()
	{
		Dispose(false);
	}

	public event EventHandler<ShellFileOpEventArgs> FinishOperations;
	public event EventHandler<ShellFileOpEventArgs> PostCopyItem;
	public event EventHandler<ShellFileOpEventArgs> PostDeleteItem;
	public event EventHandler<ShellFileOpEventArgs> PostMoveItem;
	public event EventHandler<ShellFileNewOpEventArgs> PostNewItem;
	public event EventHandler<ShellFileOpEventArgs> PostRenameItem;
	public event EventHandler<ShellFileOpEventArgs> PreCopyItem;
	public event EventHandler<ShellFileOpEventArgs> PreDeleteItem;
	public event EventHandler<ShellFileOpEventArgs> PreMoveItem;
	public event EventHandler<ShellFileOpEventArgs> PreNewItem;
	public event EventHandler<ShellFileOpEventArgs> PreRenameItem;
	public event EventHandler StartOperations;

	public event ProgressChangedEventHandler UpdateProgress;

	public bool AnyOperationsAborted => op.GetAnyOperationsAborted();

	public OperationFlags Options
	{
		get => opFlags;
		set { if (value == opFlags) { return; } op.SetOperationFlags((FILEOP_FLAGS)(opFlags = value)); }
	}

	public HWND OwnerWindow
	{
		get => owner;
		set => op.SetOwnerWindow(owner = value);
	}

	public int QueuedOperations { get; protected set; }

	public static void Copy(string source, string dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		using ShellFolder shfld = new(dest);
		Copy(shfile, shfld, newName, options);
	}

	public static void Copy(ShellItem source, ShellFolder dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostCopyItem += OnPost;
		try
		{
			sop.QueueCopyOperation(source, dest, newName);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostCopyItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	public static void Delete(string source, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		Delete(shfile, options);
	}

	public static void Delete(ShellItem source, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostDeleteItem += OnPost;
		try
		{
			sop.QueueDeleteOperation(source);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostDeleteItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	public static void Move(string source, string dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		using ShellFolder shfld = new(dest);
		Move(shfile, shfld, newName, options);
	}

	public static void Move(ShellItem source, ShellFolder dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new() { Options = options };
		HRESULT hr = HRESULT.S_OK;
		sop.PostMoveItem += OnPost;
		try
		{
			sop.QueueMoveOperation(source, dest, newName);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostMoveItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	public static void NewItem(ShellFolder dest, string name, System.IO.FileAttributes attr = System.IO.FileAttributes.Normal, string template = null, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostNewItem += OnPost;
		try
		{
			sop.QueueNewItemOperation(dest, name, attr, template);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostRenameItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	public static void Rename(ShellItem source, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostRenameItem += OnPost;
		try
		{
			sop.QueueRenameOperation(source, newName);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostRenameItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void PerformOperations()
	{
		op.PerformOperations();
		QueuedOperations = 0;
	}

	public void QueueCopyOperation(ShellItem source, ShellFolder dest, string newName = null)
	{
		op.CopyItem(source.IShellItem, dest.IShellItem, newName, null);
		QueuedOperations++;
	}

	public void QueueCopyOperation(IEnumerable<ShellItem> sourceItems, ShellFolder dest)
	{
		op.CopyItems(GetSHArray(sourceItems).IShellItemArray, dest.IShellItem);
		QueuedOperations++;
	}

	public void QueueDeleteOperation(ShellItem item)
	{
		op.DeleteItem(item.IShellItem, null);
		QueuedOperations++;
	}

	public void QueueDeleteOperation(IEnumerable<ShellItem> items)
	{
		op.DeleteItems(GetSHArray(items).IShellItemArray);
		QueuedOperations++;
	}

	public void QueueMoveOperation(ShellItem source, ShellFolder dest, string newName = null)
	{
		op.MoveItem(source.IShellItem, dest.IShellItem, newName, null);
		QueuedOperations++;
	}

	public void QueueMoveOperation(IEnumerable<ShellItem> sourceItems, ShellFolder dest)
	{
		op.MoveItems(GetSHArray(sourceItems).IShellItemArray, dest.IShellItem);
		QueuedOperations++;
	}

	public void QueueNewItemOperation(ShellFolder dest, string name, System.IO.FileAttributes attr = System.IO.FileAttributes.Normal, string template = null)
	{
		op.NewItem(dest.IShellItem, attr, name, template, null);
		QueuedOperations++;
	}

	public void QueueRenameOperation(ShellItem source, string newName)
	{
		op.RenameItem(source.IShellItem, newName, null);
		QueuedOperations++;
	}

	public void QueueRenameOperation(IEnumerable<ShellItem> sourceItems, string newName)
	{
		op.RenameItems(GetSHArray(sourceItems).IShellItemArray, newName);
		QueuedOperations++;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (Interlocked.CompareExchange(ref disposedValue, 1, 0) == 0)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects).
			}

			if (sink != null)
			{
				op.Unadvise(sinkCookie);
			}

			op = null;
		}
	}

	private ShellItemArray GetSHArray(IEnumerable<ShellItem> items) => items is ShellItemArray a ? a : new ShellItemArray(items);

	private sealed class OpSink : IFileOperationProgressSink
	{
		private readonly ShellFileOperations2 parent;

		public OpSink(ShellFileOperations2 ops) => parent = ops;

		public HRESULT FinishOperations(HRESULT hrResult) => CallChkErr(() => parent.FinishOperations?.Invoke(parent, new ShellFileOpEventArgs(0, null, null, null, null, hrResult)));

		public HRESULT PauseTimer() => HRESULT.E_NOTIMPL;

		public HRESULT PostCopyItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, HRESULT hrCopy, IShellItem psiNewlyCreated) =>
			CallChkErr(() => parent.PostCopyItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, psiDestinationFolder, psiNewlyCreated, pszNewName, hrCopy)));

		public HRESULT PostDeleteItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, HRESULT hrDelete, IShellItem psiNewlyCreated) =>
			CallChkErr(() => parent.PostDeleteItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, null, psiNewlyCreated, null, hrDelete)));

		public HRESULT PostMoveItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, HRESULT hrMove, IShellItem psiNewlyCreated) =>
			CallChkErr(() => parent.PostMoveItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, psiDestinationFolder, psiNewlyCreated, pszNewName, hrMove)));

		public HRESULT PostNewItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint dwFileAttributes, HRESULT hrNew, IShellItem psiNewItem) =>
			CallChkErr(() => parent.PostNewItem?.Invoke(parent, new ShellFileNewOpEventArgs(dwFlags, null, psiDestinationFolder, psiNewItem, pszNewName, hrNew, pszTemplateName, dwFileAttributes)));

		public HRESULT PostRenameItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, HRESULT hrRename, IShellItem psiNewlyCreated) =>
			CallChkErr(() => parent.PostRenameItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, null, psiNewlyCreated, pszNewName, hrRename)));

		public HRESULT PreCopyItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName) =>
			CallChkErr(() => parent.PreCopyItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, psiDestinationFolder, null, pszNewName)));

		public HRESULT PreDeleteItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem) =>
			CallChkErr(() => parent.PreDeleteItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem)));

		public HRESULT PreMoveItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName) =>
			CallChkErr(() => parent.PreMoveItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, psiDestinationFolder, null, pszNewName)));

		public HRESULT PreNewItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName) =>
			CallChkErr(() => parent.PreNewItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, null, psiDestinationFolder, null, pszNewName)));

		public HRESULT PreRenameItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName) => CallChkErr(() => parent.PreRenameItem?.Invoke(parent, new ShellFileOpEventArgs(dwFlags, psiItem, null, null, pszNewName)));

		public HRESULT ResetTimer() => HRESULT.E_NOTIMPL;

		public HRESULT ResumeTimer() => HRESULT.E_NOTIMPL;

		public HRESULT StartOperations() => CallChkErr(() => parent.StartOperations?.Invoke(parent, EventArgs.Empty));

		public HRESULT UpdateProgress(uint iWorkTotal, uint iWorkSoFar) => CallChkErr(() => parent.UpdateProgress?.Invoke(parent, new ProgressChangedEventArgs(iWorkTotal == 0 ? 0 : iWorkSoFar * 100.0 / iWorkTotal, null)));

		private HRESULT CallChkErr(Action action)
		{
			try { action(); }
			catch (COMException comex) { return comex.ErrorCode; }
			catch (Win32Exception w32ex) { return new Win32Error(unchecked((uint)w32ex.NativeErrorCode)).ToHRESULT(); }
			catch (Exception e)
			{
				return e.HResult;
			}
			return HRESULT.S_OK;
		}
	}

	public sealed class ShellFileNewOpEventArgs : ShellFileOpEventArgs
	{
		internal ShellFileNewOpEventArgs(TRANSFER_SOURCE_FLAGS flags, IShellItem source, IShellItem folder, IShellItem dest, string name, HRESULT hr, string templ, uint attr) :
			base(flags, source, folder, dest, name, hr)
		{
			TemplateName = templ;
			FileAttributes = (System.IO.FileAttributes)attr;
		}

		/// <summary>Gets the name of the template.</summary>
		/// <value>The name of the template.</value>
		public string TemplateName { get; protected set; }

		/// <summary>Gets the file attributes.</summary>
		/// <value>The file attributes.</value>
		public System.IO.FileAttributes FileAttributes { get; protected set; }
	}

	public class ShellFileOpEventArgs : EventArgs
	{
		internal ShellFileOpEventArgs(TRANSFER_SOURCE_FLAGS flags, IShellItem source, IShellItem folder = null, IShellItem dest = null, string name = null, HRESULT hr = default)
		{
			Flags = (TransferFlags)flags;
			if (source != null) try { SourceItem = ShellItem.Open(source); } catch { }
			if (folder != null) try { DestFolder = ShellItem.Open(folder); } catch { }
			if (dest != null) try { DestItem = ShellItem.Open(dest); } catch { }
			Name = name;
			Result = hr;
		}

		/// <summary>Gets the destination folder.</summary>
		/// <value>The destination folder.</value>
		public ShellItem DestFolder { get; protected set; }

		/// <summary>Gets the destination item.</summary>
		/// <value>The destination item.</value>
		public ShellItem DestItem { get; protected set; }

		/// <summary>Gets the tranfer flag values.</summary>
		/// <value>The flags.</value>
		public TransferFlags Flags { get; protected set; }

		/// <summary>Gets the name of the item.</summary>
		/// <value>The item name.</value>
		public string Name { get; protected set; }

		/// <summary>Gets the result of the operation.</summary>
		/// <value>The result.</value>
		public HRESULT Result { get; protected set; }

		/// <summary>Gets the source item.</summary>
		/// <value>The source item.</value>
		public ShellItem SourceItem { get; protected set; }

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString() => $"HR:{Result};Src:{SourceItem};DFld:{DestFolder};Dst:{DestItem};Name:{Name}";
	}

	public delegate void ProgressChangedEventHandler(object? sender, ProgressChangedEventArgs e);

	// From System.ComponentModel.ProgressChangedEventArgs but with double percentage
	public sealed class ProgressChangedEventArgs : EventArgs
	{
		private readonly double _progressPercentage;
		private readonly object? _userState;

		public ProgressChangedEventArgs(double progressPercentage, object? userState)
		{
			_progressPercentage = progressPercentage;
			_userState = userState;
		}

		public double ProgressPercentage
		{
			get
			{
				return _progressPercentage;
			}
		}

		public object? UserState
		{
			get
			{
				return _userState;
			}
		}
	}
}
