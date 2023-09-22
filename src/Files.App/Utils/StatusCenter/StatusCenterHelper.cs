// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem PostBanner_Delete(IEnumerable<IStorageItemWithPath> source, ReturnResult returnStatus, bool permanently, bool canceled, int itemsDeleted)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);

			if (canceled)
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteCanceled_Header".GetLocalizedResource(),
						"StatusCenter_DeleteCanceled_SubHeader".GetLocalizedResource(),
						0,
						ReturnResult.Cancelled,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleCanceled_Header".GetLocalizedResource(),
						"StatusCenter_RecycleCanceled_SubHeader".GetLocalizedResource(),
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
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteInProgress_Header".GetLocalizedResource(),
						"StatusCenter_DeleteInProgress_SubHeader".GetLocalizedResource(),
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete,
						new CancellationTokenSource());
				}
				else
				{
					// "Moving items from <x> to recycle bin"
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleInProgress_Header".GetLocalizedResource(),
						"StatusCenter_RecycleInProgress_SubHeader".GetLocalizedResource(),
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
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteComplete_Header".GetLocalizedResource(),
						"StatusCenter_DeleteComplete_SubHeader".GetLocalizedResource(),
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleComplete_Header".GetLocalizedResource(),
						"StatusCenter_RecycleComplete_SubHeader".GetLocalizedResource(),
						0,
						ReturnResult.Success,
						FileOperationType.Recycle);
				}
			}
			else
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteFailed_Header".GetLocalizedResource(),
						"StatusCenter_DeleteFailed_SubHeader".GetLocalizedResource(),
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleFailed_Header".GetLocalizedResource(),
						"StatusCenter_RecycleFailed_SubHeader".GetLocalizedResource(),
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
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyCanceled_Header".GetLocalizedResource(),
					"StatusCenter_CopyCanceled_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress_Header".GetLocalizedResource(),
					"StatusCenter_CopyInProgress_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete_Header".GetLocalizedResource(),
					"StatusCenter_CopyComplete_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed_Header".GetLocalizedResource(),
					"StatusCenter_CopyFailed_SubHeader".GetLocalizedResource(),
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
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header".GetLocalizedResource(),
					"StatusCenter_MoveCanceled_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header".GetLocalizedResource(),
					"StatusCenter_MoveInProgress_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header".GetLocalizedResource(),
					"StatusCenter_MoveComplete_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header".GetLocalizedResource(),
					"StatusCenter_MoveFailed_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static StatusCenterItem PostBanner_Compress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCompressed)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header".GetLocalizedResource(),
					"StatusCenter_MoveCanceled_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header".GetLocalizedResource(),
					"StatusCenter_MoveInProgress_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header".GetLocalizedResource(),
					"StatusCenter_MoveComplete_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header".GetLocalizedResource(),
					"StatusCenter_MoveFailed_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static StatusCenterItem PostBanner_Decompress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsDecompressed)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header".GetLocalizedResource(),
					"StatusCenter_MoveCanceled_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header".GetLocalizedResource(),
					"StatusCenter_MoveInProgress_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header".GetLocalizedResource(),
					"StatusCenter_MoveComplete_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header".GetLocalizedResource(),
					"StatusCenter_MoveFailed_SubHeader".GetLocalizedResource(),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}
	}
}
