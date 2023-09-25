// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem AddCard_Delete(IEnumerable<IStorageItemWithPath> source, ReturnResult returnStatus, bool permanently, bool canceled, int itemsDeleted)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);

			if (canceled)
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteCanceled_Header",
						string.Empty,
						0,
						ReturnResult.Cancelled,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleCanceled_Header",
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
						"StatusCenter_DeleteInProgress_Header",
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
						"StatusCenter_RecycleInProgress_Header",
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
						"StatusCenter_DeleteComplete_Header",
						string.Empty,
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleComplete_Header",
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
						"StatusCenter_DeleteFailed_Header",
						string.Empty,
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleFailed_Header",
						string.Empty,
						0,
						ReturnResult.Failed,
						FileOperationType.Recycle);
				}
			}
		}

		public static StatusCenterItem AddCard_Copy(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCopied)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyCanceled_Header",
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress_Header",
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete_Header",
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed_Header",
					string.Empty,
					0,
					ReturnResult.Failed,
					FileOperationType.Copy);
			}
		}

		public static StatusCenterItem AddCard_Move(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsMoved)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header",
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header",
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header",
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header",
					string.Empty,
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static StatusCenterItem AddCard_Compress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsCompressed)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header",
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header",
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header",
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header",
					string.Empty,
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static StatusCenterItem AddCard_Decompress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsDecompressed)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header",
					string.Empty,
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header",
					string.Empty,
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header",
					string.Empty,
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header",
					string.Empty,
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static void UpdateCardStrings(StatusCenterItem card, IEnumerable<string> source, IEnumerable<string> destination, long processedItemCount, long totalItemCount)
		{
			if (string.IsNullOrWhiteSpace(card.HeaderStringResource))
				return;

			// Aren't used for now
			string sourceDir = string.Empty;
			string destinationDir = string.Empty;

			string fileName = string.Empty;

			if (source is not null && source.Count() != 0)
			{
				sourceDir = PathNormalization.GetParentDir(source.First());
				fileName = source.First().Split('\\').Last();
			}

			if (destination is not null && destination.Count() != 0)
				destinationDir = PathNormalization.GetParentDir(destination.First());

			// Update string resources
			switch (card.Operation)
			{
				case FileOperationType.Copy:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, totalItemCount),
						_ => string.Format(card.HeaderStringResource, totalItemCount),
					};
					break;
				case FileOperationType.Move:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, totalItemCount),
						_ => string.Format(card.HeaderStringResource, totalItemCount),
					};
					break;
				case FileOperationType.Delete:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, totalItemCount),
						_ => string.Format(card.HeaderStringResource, totalItemCount),
					};
					break;
				case FileOperationType.Recycle:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource, totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, totalItemCount),
						_ => string.Format(card.HeaderStringResource, totalItemCount),
					};
					break;
				case FileOperationType.Extract:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.Success => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.Failed => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, fileName),
						_ => string.Format(card.HeaderStringResource, fileName),
					};
					break;
				case FileOperationType.Compressed:
					card.HeaderBody = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.Success => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.Failed => string.Format(card.HeaderStringResource, fileName),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource, fileName),
						_ => string.Format(card.HeaderStringResource, fileName),
					};
					break;
			}
		}
	}
}
