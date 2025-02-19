using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.Windows.Shell.ShellFileOperations;

namespace Vanara.Windows.Shell;

/// <summary>Queued and static file operations using the Shell.</summary>
/// <seealso cref="IDisposable"/>
/// https://github.com/dahall/Vanara/blob/master/Windows.Shell.Common/ShellFileOperations/ShellFileOperations.cs
public partial class ShellFileOperations2 : IDisposable
{
	private const OperationFlags defaultOptions = OperationFlags.AllowUndo | OperationFlags.NoConfirmMkDir;
	private int disposedValue = 0;
	private IFileOperation op;
	private OperationFlags opFlags = defaultOptions;
	private HWND owner;
	private readonly IFileOperationProgressSink sink;
	private readonly uint sinkCookie;

	/// <summary>Initializes a new instance of the <see cref="ShellFileOperations"/> class.</summary>
	/// <param name="owner">The window that owns the modal dialog. This value can be <see langword="null"/>.</param>
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

	/// <summary>Initializes a new instance of the <see cref="ShellFileOperations"/> class.</summary>
	/// <param name="operation">An existing <see cref="IFileOperation"/> instance.</param>
	public ShellFileOperations2(IFileOperation operation)
	{
		op = operation;
		sink = new OpSink(this);
		sinkCookie = op.Advise(sink);
	}

	/// <summary>Finalizes an instance of the <see cref="ShellFileOperations"/> class.</summary>
	~ShellFileOperations2()
	{
		Dispose(false);
	}

	/// <summary>Performs caller-implemented actions after the last operation performed by the call to IFileOperation is complete.</summary>
	public event EventHandler<ShellFileOpEventArgs> FinishOperations;

	/// <summary>Performs caller-implemented actions after the copy process for each item is complete.</summary>
	public event EventHandler<ShellFileOpEventArgs> PostCopyItem;

	/// <summary>Performs caller-implemented actions after the delete process for each item is complete.</summary>
	public event EventHandler<ShellFileOpEventArgs> PostDeleteItem;

	/// <summary>Performs caller-implemented actions after the move process for each item is complete.</summary>
	public event EventHandler<ShellFileOpEventArgs> PostMoveItem;

	/// <summary>Performs caller-implemented actions after the new item is created.</summary>
	public event EventHandler<ShellFileNewOpEventArgs> PostNewItem;

	/// <summary>Performs caller-implemented actions after the rename process for each item is complete.</summary>
	public event EventHandler<ShellFileOpEventArgs> PostRenameItem;

	/// <summary>Performs caller-implemented actions before the copy process for each item begins.</summary>
	public event EventHandler<ShellFileOpEventArgs> PreCopyItem;

	/// <summary>Performs caller-implemented actions before the delete process for each item begins.</summary>
	public event EventHandler<ShellFileOpEventArgs> PreDeleteItem;

	/// <summary>Performs caller-implemented actions before the move process for each item begins.</summary>
	public event EventHandler<ShellFileOpEventArgs> PreMoveItem;

	/// <summary>Performs caller-implemented actions before the process to create a new item begins.</summary>
	public event EventHandler<ShellFileOpEventArgs> PreNewItem;

	/// <summary>Performs caller-implemented actions before the rename process for each item begins.</summary>
	public event EventHandler<ShellFileOpEventArgs> PreRenameItem;

	/// <summary>Performs caller-implemented actions before any specific file operations are performed.</summary>
	public event EventHandler StartOperations;

	/// <summary>Occurs when a progress update is received.</summary>
	public event ProgressChangedEventHandler UpdateProgress;

	/// <summary>
	/// Gets a value that states whether any file operations initiated by a call to <see cref="PerformOperations"/> were stopped before they
	/// were complete. The operations could be stopped either by user action or silently by the system.
	/// </summary>
	/// <value><see langword="true"/> if any file operations were aborted before they were complete; otherwise, <see langword="false"/>.</value>
	public bool AnyOperationsAborted => op.GetAnyOperationsAborted();

	/// <summary>Gets or sets options that control file operations.</summary>
	public OperationFlags Options
	{
		get => opFlags;
		set { if (value == opFlags) { return; } op.SetOperationFlags((FILEOP_FLAGS)(opFlags = value)); }
	}

	/// <summary>Gets or sets the parent or owner window for progress and dialog windows.</summary>
	/// <value>The owner window of the operation. This window will receive error messages.</value>
	public HWND OwnerWindow
	{
		get => owner;
		set => op.SetOwnerWindow(owner = value);
	}

	/// <summary>Gets the number of queued operations.</summary>
	public int QueuedOperations { get; protected set; }

	/// <summary>Copies a single item to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A string that specifies the source item's full file path.</param>
	/// <param name="dest">A string that specifies the full path of the destination folder to contain the copy of the item.</param>
	/// <param name="newName">
	/// An optional new name for the item after it has been copied. This can be <see langword="null"/>. If <see langword="null"/>, the name
	/// of the destination item is the same as the source.
	/// </param>
	/// <param name="options">Options that control file operations.</param>
	public static void Copy(string source, string dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		using ShellFolder shfld = new(dest);
		Copy(shfile, shfld, newName, options);
	}

	/// <summary>Copies a single item to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the copy of the item.</param>
	/// <param name="newName">
	/// An optional new name for the item after it has been copied. This can be <see langword="null"/>. If <see langword="null"/>, the name
	/// of the destination item is the same as the source.
	/// </param>
	/// <param name="options">Options that control file operations.</param>
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

	/// <summary>Copies a set of items to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances that represent the group of items to be copied.
	/// </param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the copy of the items.</param>
	/// <param name="options">Options that control file operations.</param>
	public static void Copy(IEnumerable<ShellItem> sourceItems, ShellFolder dest, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostCopyItem += OnPost;
		try
		{
			sop.QueueCopyOperation(sourceItems, dest);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostCopyItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	/// <summary>Deletes a single item using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A string that specifies the full path of the item to be deleted.</param>
	/// <param name="options">Options that control file operations.</param>
	public static void Delete(string source, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		Delete(shfile, options);
	}

	/// <summary>Deletes a single item using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the item to be deleted.</param>
	/// <param name="options">Options that control file operations.</param>
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

	/// <summary>Deletes a set of items using the Shell to provide progress and error dialogs.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be deleted.
	/// </param>
	/// <param name="options">Options that control file operations.</param>
	public static void Delete(IEnumerable<ShellItem> sourceItems, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostDeleteItem += OnPost;
		try
		{
			sop.QueueDeleteOperation(sourceItems);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostDeleteItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	/// <summary>Moves a single item to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A string that specifies the source item's full file path.</param>
	/// <param name="dest">A string that specifies the full path of the destination folder to contain the copy of the item.</param>
	/// <param name="newName">
	/// An optional new name for the item in its new location. This can be <see langword="null"/>. If <see langword="null"/>, the name of the
	/// destination item is the same as the source.
	/// </param>
	/// <param name="options">Options that control file operations.</param>
	public static void Move(string source, string dest, string newName = null, OperationFlags options = defaultOptions)
	{
		using ShellItem shfile = new(source);
		using ShellFolder shfld = new(dest);
		Move(shfile, shfld, newName, options);
	}

	/// <summary>Moves a single item to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the moved item.</param>
	/// <param name="newName">
	/// An optional new name for the item in its new location. This can be <see langword="null"/>. If <see langword="null"/>, the name of the
	/// destination item is the same as the source.
	/// </param>
	/// <param name="options">Options that control file operations.</param>
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

	/// <summary>Moves a set of items to a specified destination using the Shell to provide progress and error dialogs.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be moved.
	/// </param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the moved items.</param>
	/// <param name="options">Options that control file operations.</param>
	public static void Move(IEnumerable<ShellItem> sourceItems, ShellFolder dest, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostMoveItem += OnPost;
		try
		{
			sop.QueueMoveOperation(sourceItems, dest);
			sop.PerformOperations();
			hr.ThrowIfFailed();
		}
		finally
		{
			sop.PostMoveItem -= OnPost;
		}

		void OnPost(object sender, ShellFileOpEventArgs e) => hr = e.Result;
	}

	/// <summary>Creates a new item in a specified location using the Shell to provide progress and error dialogs.</summary>
	/// <param name="dest">A <see cref="ShellItem"/> that specifies the destination folder that will contain the new item.</param>
	/// <param name="name">The file name of the new item, for instance Newfile.txt.</param>
	/// <param name="attr">A value that specifies the file system attributes for the file or folder.</param>
	/// <param name="template">
	/// The name of the template file (for example Excel9.xls) that the new item is based on, stored in one of the following locations:
	/// <list type="bullet">
	/// <item>
	/// <description>CSIDL_COMMON_TEMPLATES. The default path for this folder is %ALLUSERSPROFILE%\Templates.</description>
	/// </item>
	/// <item>
	/// <description>CSIDL_TEMPLATES. The default path for this folder is %USERPROFILE%\Templates.</description>
	/// </item>
	/// <item>
	/// <description>%SystemRoot%\shellnew</description>
	/// </item>
	/// </list>
	/// <para>
	/// This is a string used to specify an existing file of the same type as the new file, containing the minimal content that an
	/// application wants to include in any new file.
	/// </para>
	/// <para>This parameter is normally <see langword="null"/> to specify a new, blank file.</para>
	/// </param>
	/// <param name="options">Options that control file operations.</param>
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

	/// <summary>Renames a single item to a new display name using the Shell to provide progress and error dialogs.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="newName">The new display name of the item.</param>
	/// <param name="options">Options that control file operations.</param>
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

	/// <summary>
	/// Renames a set of items that are to be given a new display name using the Shell to provide progress and error dialogs. All items are
	/// given the same name.
	/// </summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be renamed.
	/// </param>
	/// <param name="newName">The new display name of the items.</param>
	/// <param name="options">Options that control file operations.</param>
	public static void Rename(IEnumerable<ShellItem> sourceItems, string newName, OperationFlags options = defaultOptions)
	{
		using ShellFileOperations2 sop = new();
		sop.Options = options;
		HRESULT hr = HRESULT.S_OK;
		sop.PostRenameItem += OnPost;
		try
		{
			sop.QueueRenameOperation(sourceItems, newName);
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

	/// <summary>Executes all selected operations.</summary>
	/// <remarks>
	/// This method is called last to execute those actions that have been specified earlier by calling their individual methods. For
	/// instance, <see cref="QueueRenameOperation(ShellItem, string)"/> does not rename the item, it simply sets the parameters. The actual
	/// renaming is done when you call PerformOperations.
	/// </remarks>
	public void PerformOperations()
	{
		op.PerformOperations();
		QueuedOperations = 0;
	}

	/// <summary>Declares a set of properties and values to be set on an item.</summary>
	/// <param name="item">The item to receive the new property values.</param>
	/// <param name="props">
	/// An <see cref="ShellItemPropertyUpdates"/>, which contains a dictionary of objects that specify the properties to be set and their new values.
	/// </param>
	public void QueueApplyPropertiesOperation(ShellItem item, ShellItemPropertyUpdates props)
	{
		op.SetProperties(props.IPropertyChangeArray);
		op.ApplyPropertiesToItem(item.IShellItem);
		QueuedOperations++;
	}

	/// <summary>Declares a set of properties and values to be set on a set of items.</summary>
	/// <param name="items">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances that represent the group of items to which to apply the properties.
	/// </param>
	/// <param name="props">
	/// An <see cref="ShellItemPropertyUpdates"/>, which contains a dictionary of objects that specify the properties to be set and their new values.
	/// </param>
	public void QueueApplyPropertiesOperation(IEnumerable<ShellItem> items, ShellItemPropertyUpdates props)
	{
		op.SetProperties(props.IPropertyChangeArray);
		op.ApplyPropertiesToItems(GetSHArray(items).IShellItemArray);
		QueuedOperations++;
	}

	/// <summary>Declares a single item that is to be copied to a specified destination.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the copy of the item.</param>
	/// <param name="newName">
	/// An optional new name for the item after it has been copied. This can be <see langword="null"/>. If <see langword="null"/>, the name
	/// of the destination item is the same as the source.
	/// </param>
	public void QueueCopyOperation(ShellItem source, ShellFolder dest, string newName = null)
	{
		op.CopyItem(source.IShellItem, dest.IShellItem, newName, null);
		QueuedOperations++;
	}

	/// <summary>Declares a set of items that are to be copied to a specified destination.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances that represent the group of items to be copied.
	/// </param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the copy of the items.</param>
	public void QueueCopyOperation(IEnumerable<ShellItem> sourceItems, ShellFolder dest)
	{
		op.CopyItems(GetSHArray(sourceItems).IShellItemArray, dest.IShellItem);
		QueuedOperations++;
	}

	/// <summary>Declares a single item that is to be deleted.</summary>
	/// <param name="item">&gt;A <see cref="ShellItem"/> that specifies the item to be deleted.</param>
	public void QueueDeleteOperation(ShellItem item)
	{
		op.DeleteItem(item.IShellItem, null);
		QueuedOperations++;
	}

	/// <summary>Declares a set of items that are to be deleted.</summary>
	/// <param name="items">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be deleted.
	/// </param>
	public void QueueDeleteOperation(IEnumerable<ShellItem> items)
	{
		op.DeleteItems(GetSHArray(items).IShellItemArray);
		QueuedOperations++;
	}

	/// <summary>Declares a single item that is to be moved to a specified destination.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the moved item.</param>
	/// <param name="newName">
	/// An optional new name for the item in its new location. This can be <see langword="null"/>. If <see langword="null"/>, the name of the
	/// destination item is the same as the source.
	/// </param>
	public void QueueMoveOperation(ShellItem source, ShellFolder dest, string newName = null)
	{
		op.MoveItem(source.IShellItem, dest.IShellItem, newName, null);
		QueuedOperations++;
	}

	/// <summary>Declares a set of items that are to be moved to a specified destination.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be moved.
	/// </param>
	/// <param name="dest">A <see cref="ShellFolder"/> that specifies the destination folder to contain the moved items.</param>
	public void QueueMoveOperation(IEnumerable<ShellItem> sourceItems, ShellFolder dest)
	{
		op.MoveItems(GetSHArray(sourceItems).IShellItemArray, dest.IShellItem);
		QueuedOperations++;
	}

	/// <summary>Declares a new item that is to be created in a specified location.</summary>
	/// <param name="dest">A <see cref="ShellItem"/> that specifies the destination folder that will contain the new item.</param>
	/// <param name="name">The file name of the new item, for instance Newfile.txt.</param>
	/// <param name="attr">A value that specifies the file system attributes for the file or folder.</param>
	/// <param name="template">
	/// The name of the template file (for example Excel9.xls) that the new item is based on, stored in one of the following locations:
	/// <list type="bullet">
	/// <item>
	/// <description>CSIDL_COMMON_TEMPLATES. The default path for this folder is %ALLUSERSPROFILE%\Templates.</description>
	/// </item>
	/// <item>
	/// <description>CSIDL_TEMPLATES. The default path for this folder is %USERPROFILE%\Templates.</description>
	/// </item>
	/// <item>
	/// <description>%SystemRoot%\shellnew</description>
	/// </item>
	/// </list>
	/// <para>
	/// This is a string used to specify an existing file of the same type as the new file, containing the minimal content that an
	/// application wants to include in any new file.
	/// </para>
	/// <para>This parameter is normally <see langword="null"/> to specify a new, blank file.</para>
	/// </param>
	public void QueueNewItemOperation(ShellFolder dest, string name, System.IO.FileAttributes attr = System.IO.FileAttributes.Normal, string template = null)
	{
		op.NewItem(dest.IShellItem, attr, name, template, null);
		QueuedOperations++;
	}

	/// <summary>Declares a single item that is to be given a new display name.</summary>
	/// <param name="source">A <see cref="ShellItem"/> that specifies the source item.</param>
	/// <param name="newName">The new display name of the item.</param>
	public void QueueRenameOperation(ShellItem source, string newName)
	{
		op.RenameItem(source.IShellItem, newName, null);
		QueuedOperations++;
	}

	/// <summary>Declares a set of items that are to be given a new display name. All items are given the same name.</summary>
	/// <param name="sourceItems">
	/// An <see cref="IEnumerable{T}"/> of <see cref="ShellItem"/> instances which represents the group of items to be renamed.
	/// </param>
	/// <param name="newName">The new display name of the items.</param>
	public void QueueRenameOperation(IEnumerable<ShellItem> sourceItems, string newName)
	{
		op.RenameItems(GetSHArray(sourceItems).IShellItemArray, newName);
		QueuedOperations++;
	}

	/// <summary>Releases unmanaged and - optionally - managed resources.</summary>
	/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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

	/// <summary>Arguments supplied to the <see cref="PostNewItem"/> event.</summary>
	/// <seealso cref="ShellFileOpEventArgs"/>
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

	/// <summary>
	/// Arguments supplied to events from <see cref="ShellFileOperations"/>. Depending on the event, some properties may not be set.
	/// </summary>
	/// <seealso cref="EventArgs"/>
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
