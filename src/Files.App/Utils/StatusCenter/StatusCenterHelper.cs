// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem PostBanner_Delete(IEnumerable<IStorageItemWithPath> source, ReturnResult returnStatus, bool permanently, bool canceled, int itemsDeleted)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);

			if (canceled)
			{
				if (permanently)
				{
					return StatusCenterViewModel.AddItem(
						"StatusDeletionCancelled".GetLocalizedResource(),
						string.Format(source.Count() > 1 ?
							itemsDeleted > 1 ? "StatusDeleteCanceledDetails_Plural".GetLocalizedResource() : "StatusDeleteCanceledDetails_Plural2".GetLocalizedResource()
							: "StatusDeleteCanceledDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, null, itemsDeleted),
						0,
						ReturnResult.Cancelled,
						FileOperationType.Delete);
				}
				else
				{
					return StatusCenterViewModel.AddItem(
						"StatusRecycleCancelled".GetLocalizedResource(),
						string.Format(source.Count() > 1 ?
							itemsDeleted > 1 ? "StatusMoveCanceledDetails_Plural".GetLocalizedResource() : "StatusMoveCanceledDetails_Plural2".GetLocalizedResource()
							: "StatusMoveCanceledDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, "TheRecycleBin".GetLocalizedResource(), itemsDeleted),
						0,
						ReturnResult.Cancelled,
						FileOperationType.Recycle);
				}
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				if (permanently)
				{
					// deleting items from <x>
					return StatusCenterViewModel.AddItem(string.Empty,
						string.Format(source.Count() > 1 ? "StatusDeletingItemsDetails_Plural".GetLocalizedResource() : "StatusDeletingItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir),
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete,
						new CancellationTokenSource());
				}
				else
				{
					// "Moving items from <x> to recycle bin"
					return StatusCenterViewModel.AddItem(string.Empty,
						string.Format(source.Count() > 1 ? "StatusMovingItemsDetails_Plural".GetLocalizedResource() : "StatusMovingItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, "TheRecycleBin".GetLocalizedResource()),
						0,
						ReturnResult.InProgress,
						FileOperationType.Recycle,
						new CancellationTokenSource());
				}
			}
			else if (returnStatus == ReturnResult.Success)
			{
				if (permanently)
				{
					return StatusCenterViewModel.AddItem(
						"StatusDeletionComplete".GetLocalizedResource(),
						string.Format(source.Count() > 1 ? "StatusDeletedItemsDetails_Plural".GetLocalizedResource() : "StatusDeletedItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, itemsDeleted),
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					return StatusCenterViewModel.AddItem(
						"StatusRecycleComplete".GetLocalizedResource(),
						string.Format(source.Count() > 1 ? "StatusMovedItemsDetails_Plural".GetLocalizedResource() : "StatusMovedItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, "TheRecycleBin".GetLocalizedResource()),
						0,
						ReturnResult.Success,
						FileOperationType.Recycle);
				}
			}
			else
			{
				if (permanently)
				{
					return StatusCenterViewModel.AddItem(
						"StatusDeletionFailed".GetLocalizedResource(),
						string.Format(source.Count() > 1 ? "StatusDeletionFailedDetails_Plural".GetLocalizedResource() : "StatusDeletionFailedDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir),
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					return StatusCenterViewModel.AddItem(
						"StatusRecycleFailed".GetLocalizedResource(),
						string.Format(source.Count() > 1 ? "StatusMoveFailedDetails_Plural".GetLocalizedResource() : "StatusMoveFailedDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, "TheRecycleBin".GetLocalizedResource()),
						0,
						ReturnResult.Failed,
						FileOperationType.Recycle);
				}
			}
		}

		public static StatusCenterItem PostBanner_Copy(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCopied)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return StatusCenterViewModel.AddItem(
					"StatusCopyCanceled".GetLocalizedResource(),
					string.Format(source.Count() > 1 ?
						itemsCopied > 1 ? "StatusCopyCanceledDetails_Plural".GetLocalizedResource() : "StatusCopyCanceledDetails_Plural2".GetLocalizedResource() :
						"StatusCopyCanceledDetails_Singular".GetLocalizedResource(), source.Count(), destinationDir, itemsCopied),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return StatusCenterViewModel.AddItem(
					string.Empty,
					string.Format(source.Count() > 1 ? "StatusCopyingItemsDetails_Plural".GetLocalizedResource() : "StatusCopyingItemsDetails_Singular".GetLocalizedResource(), source.Count(), destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return StatusCenterViewModel.AddItem(
					"StatusCopyComplete".GetLocalizedResource(),
					string.Format(source.Count() > 1 ? "StatusCopiedItemsDetails_Plural".GetLocalizedResource() : "StatusCopiedItemsDetails_Singular".GetLocalizedResource(), source.Count(), destinationDir, itemsCopied),
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				return StatusCenterViewModel.AddItem(
					"StatusCopyFailed".GetLocalizedResource(),
					string.Format(source.Count() > 1 ? "StatusCopyFailedDetails_Plural".GetLocalizedResource() : "StatusCopyFailedDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Copy);
			}
		}

		public static StatusCenterItem PostBanner_Move(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsMoved)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return StatusCenterViewModel.AddItem(
					"StatusMoveCanceled".GetLocalizedResource(),
					string.Format(source.Count() > 1 ?
						itemsMoved > 1 ? "StatusMoveCanceledDetails_Plural".GetLocalizedResource() : "StatusMoveCanceledDetails_Plural2".GetLocalizedResource()
						: "StatusMoveCanceledDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, destinationDir, itemsMoved),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return StatusCenterViewModel.AddItem(
					string.Empty,
					string.Format(source.Count() > 1 ? "StatusMovingItemsDetails_Plural".GetLocalizedResource() : "StatusMovingItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return StatusCenterViewModel.AddItem(
					"StatusMoveComplete".GetLocalizedResource(),
					string.Format(source.Count() > 1 ? "StatusMovedItemsDetails_Plural".GetLocalizedResource() : "StatusMovedItemsDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, destinationDir, itemsMoved),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return StatusCenterViewModel.AddItem(
					"StatusMoveFailed".GetLocalizedResource(),
					string.Format(source.Count() > 1 ? "StatusMoveFailedDetails_Plural".GetLocalizedResource() : "StatusMoveFailedDetails_Singular".GetLocalizedResource(), source.Count(), sourceDir, destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}
	}
}
