// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public static class PostBannerHelpers
	{
		private static OngoingTasksViewModel OngoingTasksViewModel { get; } = Ioc.Default.GetRequiredService<OngoingTasksViewModel>();

		public static PostedStatusBanner PostBanner_Delete(IEnumerable<IStorageItemWithPath> source, ReturnResult returnStatus, bool permanently, bool canceled, int itemsDeleted)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);

			if (canceled)
			{
				if (permanently)
				{
					return OngoingTasksViewModel.PostBanner(
						"StatusDeletionCancelled".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? itemsDeleted > 1
									? "StatusDeleteCanceledDetails_Plural".GetLocalizedResource()
									: "StatusDeleteCanceledDetails_Plural2".GetLocalizedResource()
								: "StatusDeleteCanceledDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							null,
							itemsDeleted),
						0,
						ReturnResult.Cancelled,
						FileOperationType.Delete);
				}
				else
				{
					return OngoingTasksViewModel.PostBanner(
						"StatusRecycleCancelled".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? itemsDeleted > 1
									? "StatusMoveCanceledDetails_Plural".GetLocalizedResource()
									: "StatusMoveCanceledDetails_Plural2".GetLocalizedResource()
								: "StatusMoveCanceledDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource(),
							itemsDeleted),
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
					return OngoingTasksViewModel.PostOperationBanner(string.Empty,
						string.Format(
							source.Count() > 1
								? "StatusDeletingItemsDetails_Plural".GetLocalizedResource()
								: "StatusDeletingItemsDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir),
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete,
						new CancellationTokenSource());
				}
				else
				{
					// "Moving items from <x> to recycle bin"
					return OngoingTasksViewModel.PostOperationBanner(string.Empty,
						string.Format(
							source.Count() > 1
								? "StatusMovingItemsDetails_Plural".GetLocalizedResource()
								: "StatusMovingItemsDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource()),
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
					return OngoingTasksViewModel.PostBanner(
						"StatusDeletionComplete".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusDeletedItemsDetails_Plural".GetLocalizedResource()
								: "StatusDeletedItemsDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							itemsDeleted),
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					return OngoingTasksViewModel.PostBanner(
						"StatusRecycleComplete".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusMovedItemsDetails_Plural".GetLocalizedResource()
								: "StatusMovedItemsDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource()),
						0,
						ReturnResult.Success,
						FileOperationType.Recycle);
				}
			}
			else
			{
				if (permanently)
				{
					return OngoingTasksViewModel.PostBanner(
						"StatusDeletionFailed".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusDeletionFailedDetails_Plural".GetLocalizedResource()
								: "StatusDeletionFailedDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir),
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					return OngoingTasksViewModel.PostBanner(
						"StatusRecycleFailed".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusMoveFailedDetails_Plural".GetLocalizedResource()
								: "StatusMoveFailedDetails_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource()),
						0,
						ReturnResult.Failed,
						FileOperationType.Recycle);
				}
			}
		}

		public static PostedStatusBanner PostBanner_Copy(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCopied)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusCopyCanceled".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsCopied > 1
								? "StatusCopyCanceledDetails_Plural".GetLocalizedResource()
								: "StatusCopyCanceledDetails_Plural2".GetLocalizedResource()
							: "StatusCopyCanceledDetails_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir,
						itemsCopied),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return OngoingTasksViewModel.PostOperationBanner(
					string.Empty,
					string.Format(
						source.Count() > 1
							? "StatusCopyingItemsDetails_Plural".GetLocalizedResource()
							: "StatusCopyingItemsDetails_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusCopyComplete".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCopiedItemsDetails_Plural".GetLocalizedResource()
							: "StatusCopiedItemsDetails_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir,
						itemsCopied),
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusCopyFailed".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCopyFailedDetails_Plural".GetLocalizedResource()
							: "StatusCopyFailedDetails_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Copy);
			}
		}

		public static PostedStatusBanner PostBanner_Move(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsMoved)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusMoveCanceled".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsMoved > 1
								? "StatusMoveCanceledDetails_Plural".GetLocalizedResource()
								: "StatusMoveCanceledDetails_Plural2".GetLocalizedResource()
							: "StatusMoveCanceledDetails_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir,
						itemsMoved),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return OngoingTasksViewModel.PostOperationBanner(
					string.Empty,
					string.Format(
						source.Count() > 1
							? "StatusMovingItemsDetails_Plural".GetLocalizedResource()
							: "StatusMovingItemsDetails_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusMoveComplete".GetLocalizedResource(),
					string.Format(source.Count() > 1
						? "StatusMovedItemsDetails_Plural".GetLocalizedResource()
						: "StatusMovedItemsDetails_Singular".GetLocalizedResource(),
						source.Count(), sourceDir, destinationDir, itemsMoved),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return OngoingTasksViewModel.PostBanner(
					"StatusMoveFailed".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusMoveFailedDetails_Plural".GetLocalizedResource()
							: "StatusMoveFailedDetails_Singular".GetLocalizedResource(),
						source.Count(), sourceDir, destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}
	}
}
