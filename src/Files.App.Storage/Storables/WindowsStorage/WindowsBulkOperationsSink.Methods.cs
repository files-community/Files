// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		private unsafe partial struct WindowsBulkOperationsSink : IFileOperationProgressSink.Interface
		{
			public HRESULT StartOperations()
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.OperationsStarted?.Invoke(operations, EventArgs.Empty);
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public HRESULT FinishOperations(HRESULT hrResult)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.OperationsFinished?.Invoke(operations, new(result: hrResult));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PreRenameItem(uint dwFlags, IShellItem* pSource, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemRenaming?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), null, null, pszNewName.ToString(), null, default));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PostRenameItem(uint dwFlags, IShellItem* pSource, PCWSTR pszNewName, HRESULT hrRename, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemRenamed?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), null, WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrRename));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PreMoveItem(uint dwFlags, IShellItem* pSource, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemMoving?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PostMoveItem(uint dwFlags, IShellItem* pSource, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrMove, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemMoved?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrMove));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PreCopyItem(uint dwFlags, IShellItem* pSource, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCopying?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PostCopyItem(uint dwFlags, IShellItem* pSource, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrCopy, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCopied?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewlyCreated), pszNewName.ToString(), null, hrCopy));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PreDeleteItem(uint dwFlags, IShellItem* pSource)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemDeleting?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), null, null, null, null, default));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PostDeleteItem(uint dwFlags, IShellItem* pSource, HRESULT hrDelete, IShellItem* psiNewlyCreated)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemDeleted?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, WindowsStorable.TryParse(pSource), null, WindowsStorable.TryParse(psiNewlyCreated), null, null, hrDelete));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PreNewItem(uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCreating?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, null, new WindowsFolder(psiDestinationFolder), null, pszNewName.ToString(), null, default));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public unsafe HRESULT PostNewItem(uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName, PCWSTR pszTemplateName, uint dwFileAttributes, HRESULT hrNew, IShellItem* psiNewItem)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					operations.ItemCreated?.Invoke(operations, new((_TRANSFER_SOURCE_FLAGS)dwFlags, null, new WindowsFolder(psiDestinationFolder), WindowsStorable.TryParse(psiNewItem), pszNewName.ToString(), pszTemplateName.ToString(), hrNew));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
			}

			public HRESULT UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
			{
				if (_operationsHandle.Target is WindowsBulkOperations operations)
				{
					var percentage = iWorkTotal is 0 ? 0 : iWorkSoFar * 100.0 / iWorkTotal;
					operations.ProgressUpdated?.Invoke(operations, new((int)percentage, null));
					return HRESULT.S_OK;
				}

				return HRESULT.E_FAIL;
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
		}
	}
}
