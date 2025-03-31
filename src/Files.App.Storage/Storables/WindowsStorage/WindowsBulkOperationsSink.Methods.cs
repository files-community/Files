// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		private unsafe partial struct WindowsBulkOperationsSink : IFileOperationProgressSink.Interface
		{
			public HRESULT StartOperations()
			{
				return HRESULT.S_OK;
			}

			public HRESULT FinishOperations(HRESULT hrResult)
			{
				return HRESULT.S_OK;
			}

			public unsafe HRESULT PreRenameItem(uint dwFlags, IShellItem* psiItem, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemRenaming?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PostRenameItem(uint dwFlags, IShellItem* psiItem, PCWSTR pszNewName, HRESULT hrRename, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemRenamed?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PreMoveItem(uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemMoving?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PostMoveItem(uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrMove, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemMoved?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PreCopyItem(uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCopying?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PostCopyItem(uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrCopy, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCopied?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PreDeleteItem(uint dwFlags, IShellItem* psiItem)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemDeleting?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PostDeleteItem(uint dwFlags, IShellItem* psiItem, HRESULT hrDelete, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemDeleted?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PreNewItem(uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCreating?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public unsafe HRESULT PostNewItem(uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName, PCWSTR pszTemplateName, uint dwFileAttributes, HRESULT hrNew, IShellItem* psiNewItem)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCreated?.Invoke(operations, new());
					return HRESULT.S_OK;
				}

				return HRESULT.E_INVALIDARG;
			}

			public HRESULT UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
			{
				return HRESULT.S_OK;
			}

			public HRESULT ResetTimer()
			{
				return HRESULT.S_OK;
			}

			public HRESULT PauseTimer()
			{
				return HRESULT.S_OK;
			}

			public HRESULT ResumeTimer()
			{
				return HRESULT.S_OK;
			}
		}
	}
}
