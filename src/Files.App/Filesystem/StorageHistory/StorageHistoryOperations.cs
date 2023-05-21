// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Storage;

namespace Files.App.Filesystem.FilesystemHistory
{
	public class StorageHistoryOperations : IStorageHistoryOperations
	{
		private IFilesystemHelpers _helpers;

		private IFilesystemOperations _operations;

		private readonly CancellationToken _cancellationToken;

		public StorageHistoryOperations(IShellPage associatedInstance, CancellationToken cancellationToken)
		{
			// Initialize
			_cancellationToken = cancellationToken;
			_helpers = associatedInstance.FilesystemHelpers;
			_operations = new ShellFilesystemOperations(associatedInstance);
		}

		public async Task<ReturnResult> Undo(IStorageHistory history)
		{
			ReturnResult returnStatus = ReturnResult.InProgress;
			Progress<FileSystemProgress> progress = new();

			progress.ProgressChanged += (s, e) => returnStatus = e.Status!.Value.ToStatus();

			switch (history.OperationType)
			{
				// Opposite: Delete created items
				case FileOperationType.CreateNew:
					if (!IsHistoryNull(history.Source))
					{
						// Show a dialog regardless of the setting to prevent unexpected deletion
						return await _helpers.DeleteItemsAsync(history.Source, DeleteConfirmationPolicies.Always, true, false);
					}
					break;
				// Opposite: Delete created items
				case FileOperationType.CreateLink:
					if (!IsHistoryNull(history.Destination))
					{
						// Show a dialog regardless of the setting to prevent unexpected deletion
						return await _helpers.DeleteItemsAsync(history.Destination, DeleteConfirmationPolicies.Always, true, false);
					}
					break;
				// Opposite: Restore original item names
				case FileOperationType.Rename:
					if (!IsHistoryNull(history))
					{
						NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;

						for (int i = 0; i < history.Destination.Count; i++)
						{
							string name = Path.GetFileName(history.Source[i].Path);

							await _operations.RenameAsync(history.Destination[i], name, collision, progress, _cancellationToken);
						}
					}
					break;
				// Opposite: Delete copied items
				case FileOperationType.Copy:
					if (!IsHistoryNull(history.Destination))
					{
						// Show a dialog regardless of the setting to prevent unexpected deletion
						return await _helpers.DeleteItemsAsync(history.Destination, DeleteConfirmationPolicies.Always, true, false);
					}
					break;
				// Opposite: Move the items to original directory
				case FileOperationType.Move:
					if (!IsHistoryNull(history))
					{
						return await _helpers.MoveItemsAsync(history.Destination, history.Source.Select(item => item.Path), false, false);
					}
					break;
				// Opposite: No opposite for archive extraction
				case FileOperationType.Extract:
					returnStatus = ReturnResult.Success;
					Debugger.Break();
					break;
				// Opposite: Restore recycled items
				case FileOperationType.Recycle:
					if (!IsHistoryNull(history))
					{
						returnStatus = await _helpers.RestoreItemsFromTrashAsync(history.Destination, history.Source.Select(item => item.Path), false);
						if (returnStatus is ReturnResult.IntegrityCheckFailed) // Not found, corrupted
						{
							App.HistoryWrapper.RemoveHistory(history, false);
						}
					}
					break;
				// Opposite: Move restored items to Recycle Bin
				case FileOperationType.Restore:
					if (!IsHistoryNull(history.Destination))
					{
						var newHistory = await _operations.DeleteItemsAsync(history.Destination, progress, false, _cancellationToken);
						if (newHistory is null)
						{
							App.HistoryWrapper.RemoveHistory(history, false);
						}
						else
						{
							// We need to change the recycled item paths (since IDs are different) - for Redo() to work
							App.HistoryWrapper.ModifyCurrentHistory(newHistory);
						}
					}
					break;
				// Opposite: No opposite for permanent deletion
				case FileOperationType.Delete:
					returnStatus = ReturnResult.Success;
					break;
			}

			return returnStatus;
		}

		public async Task<ReturnResult> Redo(IStorageHistory history)
		{
			ReturnResult returnStatus = ReturnResult.InProgress;
			Progress<FileSystemProgress> progress = new();

			progress.ProgressChanged += (s, e) =>
			{
				returnStatus = e.Status!.Value.ToStatus();
			};

			switch (history.OperationType)
			{
				case FileOperationType.CreateNew:
					if (IsHistoryNull(history))
					{
						foreach (var source in history.Source)
							await _operations.CreateAsync(source, progress, _cancellationToken);
					}
					break;
				case FileOperationType.CreateLink:
					if (!IsHistoryNull(history))
					{
						await _operations.CreateShortcutItemsAsync(
							history.Source,
							await history.Destination.Select(item => item.Path).ToListAsync(),
							progress,
							_cancellationToken);
					}
					break;
				case FileOperationType.Rename:
					if (!IsHistoryNull(history))
					{
						NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;

						for (int i = 0; i < history.Source.Count; i++)
						{
							string name = Path.GetFileName(history.Destination[i].Path);

							await _operations.RenameAsync(history.Source[i], name, collision, progress, _cancellationToken);
						}
					}
					break;
				case FileOperationType.Copy:
					if (!IsHistoryNull(history))
						return await _helpers.CopyItemsAsync(history.Source, history.Destination.Select(item => item.Path), false, false);

					break;
				case FileOperationType.Move:
					if (!IsHistoryNull(history))
						return await _helpers.MoveItemsAsync(history.Source, history.Destination.Select(item => item.Path), false, false);

					break;
				case FileOperationType.Extract:
					returnStatus = ReturnResult.Success;
					Debugger.Break();
					break;
				case FileOperationType.Recycle: // Recycle PASS
					if (!IsHistoryNull(history.Destination))
					{
						var newHistory = await _operations.DeleteItemsAsync(history.Source, progress, false, _cancellationToken);

						if (newHistory is null)
						{
							App.HistoryWrapper.RemoveHistory(history, true);
						}
						else
						{
							// We need to change the recycled item paths (since IDs are different) - for Undo() to work
							App.HistoryWrapper.ModifyCurrentHistory(newHistory);
						}
					}
					break;
				case FileOperationType.Restore:
					if (!IsHistoryNull(history))
						await _helpers.RestoreItemsFromTrashAsync(history.Source, history.Destination.Select(item => item.Path), false);

					break;
				case FileOperationType.Delete:
					returnStatus = ReturnResult.Success;

					break;
			}

			return returnStatus;
		}

		private static bool IsHistoryNull(IStorageHistory history)
		{
			// history.Destination is null with CreateNew

			return IsHistoryNull(history.Source) || (history.Destination is not null && IsHistoryNull(history.Destination));
		}

		private static bool IsHistoryNull(IEnumerable<IStorageItemWithPath> source)
		{
			return !source.All(HasPath);
		}

		private static bool HasPath(IStorageItemWithPath item)
		{
			return item is not null && !string.IsNullOrWhiteSpace(item.Path);
		}

		public void Dispose()
		{
			_helpers?.Dispose();
			_helpers = null;

			_operations?.Dispose();
			_operations = null;
		}
	}
}
