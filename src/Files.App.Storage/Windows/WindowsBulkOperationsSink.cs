// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		[GeneratedComClass]
		private partial class WindowsBulkOperationsSink : IFileOperationProgressSink
		{
			private WindowsBulkOperations _operations;

			public WindowsBulkOperationsSink(WindowsBulkOperations operations)
			{
				_operations = operations;
			}

			public HRESULT StartOperations()
			{
				_operations.OperationsStarted?.Invoke(_operations, EventArgs.Empty);

				return HRESULT.S_OK;
			}

			public HRESULT FinishOperations(HRESULT hrResult)
			{
				_operations.OperationsFinished?.Invoke(_operations, new(result: hrResult));

				return HRESULT.S_OK;
			}

			public HRESULT ResetTimer()
			{
				return HRESULT.E_NOTIMPL;
			}

			public HRESULT PauseTimer()
			{
				return HRESULT.E_NOTIMPL;
			}

			public HRESULT ResumeTimer()
			{
				return HRESULT.E_NOTIMPL;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PreRenameItem(uint dwFlags, IShellItem psiItem, PCWSTR pszNewName)
			{
				_operations.ItemRenaming?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), null, null, pszNewName.ToString(), null, default));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PostRenameItem(uint dwFlags, IShellItem psiItem, PCWSTR pszNewName, HRESULT hrRename, IShellItem psiNewlyCreated)
			{
				_operations.ItemRenamed?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), null, WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrRename));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, PCWSTR pszNewName)
			{
				_operations.ItemMoving?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, PCWSTR pszNewName, HRESULT hrMove, IShellItem psiNewlyCreated)
			{
				_operations.ItemMoved?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrMove));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, PCWSTR pszNewName)
			{
				_operations.ItemCopying?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, PCWSTR pszNewName, HRESULT hrCopy, IShellItem psiNewlyCreated)
			{
				_operations.ItemCopied?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrCopy));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PreDeleteItem(uint dwFlags, IShellItem psiItem)
			{
				_operations.ItemDeleting?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), null, null, null, null, default));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PostDeleteItem(uint dwFlags, IShellItem psiItem, HRESULT hrDelete, IShellItem psiNewlyCreated)
			{
				_operations.ItemDeleted?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(psiItem), null, WindowsStorable.TryParse(psiNewlyCreated), null, null, hrDelete));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, PCWSTR pszNewName)
			{
				_operations.ItemCreating?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, null, new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
				return HRESULT.S_OK;
			}

			[return: MarshalAs(UnmanagedType.Error)]
			public HRESULT PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, PCWSTR pszNewName, PCWSTR pszTemplateName, uint dwFileAttributes, HRESULT hrNew, IShellItem psiNewItem)
			{
				_operations.ItemCreated?.Invoke(_operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, null, new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewItem), pszNewName.ToString(), pszTemplateName.ToString(), hrNew));
				return HRESULT.S_OK;
			}

			public HRESULT UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
			{
				var percentage = iWorkTotal is 0 ? 0 : iWorkSoFar * 100.0 / iWorkTotal;
				_operations.ProgressUpdated?.Invoke(_operations, new((int)percentage, null));
				return HRESULT.S_OK;
			}

		}
	}
}
