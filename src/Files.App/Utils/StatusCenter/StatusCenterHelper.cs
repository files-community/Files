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
						"StatusCenter_DeleteCanceled".GetLocalizedResource(),
						string.Empty,
						0,
						ReturnResult.Cancelled,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleCanceled".GetLocalizedResource(),
						string.Empty,
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
						"StatusCenter_DeleteInProgress".GetLocalizedResource(),
						string.Empty,
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete,
						new CancellationTokenSource());
				}
				else
				{
					// "Moving items from <x> to recycle bin"
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleInProgress".GetLocalizedResource(),
						string.Empty,
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
						"StatusCenter_DeleteComplete".GetLocalizedResource(),
						string.Empty,
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleComplete".GetLocalizedResource(),
						string.Empty,
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
						"StatusCenter_DeleteFailed".GetLocalizedResource(),
						string.Empty,
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleFailed".GetLocalizedResource(),
						string.Empty,
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
					"StatusCenter_CopyCanceled".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed".GetLocalizedResource(),
					string.Empty,
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
					"StatusCenter_MoveCanceled".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed".GetLocalizedResource(),
					string.Empty,
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}
	}
}
