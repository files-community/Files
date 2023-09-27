// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem AddCard_Delete(ReturnResult returnStatus, bool permanently, IEnumerable<IStorageItemWithPath>? source)
		{
			string? sourceDir = string.Empty;
			
			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (returnStatus == ReturnResult.Cancelled)
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteCanceled_Header".GetLocalizedResource(),
						"StatusCenter_DeleteCanceled_Header",
						ReturnResult.Cancelled,
						FileOperationType.Delete,
						source?.Select(x => x.Path) ?? string.Empty.CreateEnumerable(),
						null,
						true);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleCanceled_Header".GetLocalizedResource(),
						"StatusCenter_RecycleCanceled_Header",
						ReturnResult.Cancelled,
						FileOperationType.Recycle,
						source?.Select(x => x.Path),
						null,
						true);
				}
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteInProgress_Header".GetLocalizedResource(),
						"StatusCenter_DeleteInProgress_Header",
						ReturnResult.InProgress,
						FileOperationType.Delete,
						source?.Select(x => x.Path),
						null,
						true,
						new CancellationTokenSource());
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleInProgress_Header".GetLocalizedResource(),
						"StatusCenter_RecycleInProgress_Header",
						ReturnResult.InProgress,
						FileOperationType.Recycle,
						source?.Select(x => x.Path),
						null,
						true,
						new CancellationTokenSource());
				}
			}
			else if (returnStatus == ReturnResult.Success)
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteComplete_Header".GetLocalizedResource(),
						"StatusCenter_DeleteComplete_Header",
						ReturnResult.Success,
						FileOperationType.Delete,
						source?.Select(x => x.Path),
						null,
						true);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleComplete_Header".GetLocalizedResource(),
						"StatusCenter_RecycleComplete_Header",
						ReturnResult.Success,
						FileOperationType.Recycle,
						source?.Select(x => x.Path),
						null,
						true);
				}
			}
			else
			{
				if (permanently)
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteFailed_Header".GetLocalizedResource(),
						"StatusCenter_DeleteFailed_Header",
						ReturnResult.Failed,
						FileOperationType.Delete,
						source?.Select(x => x.Path),
						null,
						true);
				}
				else
				{
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleFailed_Header",
						"StatusCenter_RecycleFailed_Header",
						ReturnResult.Failed,
						FileOperationType.Recycle,
						source?.Select(x => x.Path),
						null,
						true);
				}
			}
		}

		public static StatusCenterItem AddCard_Copy(ReturnResult returnStatus, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination)
		{
			string? sourceDir = string.Empty;
			string? destinationDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (destination is not null && destination.Any())
				destinationDir = PathNormalization.GetParentDir(destination.First());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyCanceled_Header".GetLocalizedResource(),
					"StatusCenter_CopyCanceled_Header",
					ReturnResult.Cancelled,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress_Header".GetLocalizedResource(),
					"StatusCenter_CopyInProgress_Header",
					ReturnResult.InProgress,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete_Header".GetLocalizedResource(),
					"StatusCenter_CopyComplete_Header",
					ReturnResult.Success,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed_Header".GetLocalizedResource(),
					"StatusCenter_CopyFailed_Header",
					ReturnResult.Failed,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true);
			}
		}

		public static StatusCenterItem AddCard_Move(ReturnResult returnStatus, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header".GetLocalizedResource(),
					"StatusCenter_MoveCanceled_Header",
					ReturnResult.Cancelled,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header".GetLocalizedResource(),
					"StatusCenter_MoveInProgress_Header",
					ReturnResult.InProgress,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header".GetLocalizedResource(),
					"StatusCenter_MoveComplete_Header",
					ReturnResult.Success,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header".GetLocalizedResource(),
					"StatusCenter_MoveFailed_Header",
					ReturnResult.Failed,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true);
			}
		}

		public static StatusCenterItem AddCard_Compress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressCanceled_Header".GetLocalizedResource(),
					"StatusCenter_CompressCanceled_Header",
					ReturnResult.Cancelled,
					FileOperationType.Compressed,
					source,
					destination,
					false);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressInProgress_Header".GetLocalizedResource(),
					"StatusCenter_CompressInProgress_Header",
					ReturnResult.InProgress,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressComplete_Header".GetLocalizedResource(),
					"StatusCenter_CompressComplete_Header",
					ReturnResult.Success,
					FileOperationType.Compressed,
					source,
					destination,
					false);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressFailed_Header".GetLocalizedResource(),
					"StatusCenter_CompressFailed_Header",
					ReturnResult.Failed,
					FileOperationType.Compressed,
					source,
					destination,
					false);
			}
		}

		public static StatusCenterItem AddCard_Decompress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressCanceled_Header".GetLocalizedResource(),
					"StatusCenter_DecompressCanceled_Header",
					ReturnResult.Cancelled,
					FileOperationType.Extract,
					source,
					destination,
					false);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressInProgress_Header".GetLocalizedResource(),
					"StatusCenter_DecompressInProgress_Header",
					ReturnResult.InProgress,
					FileOperationType.Extract,
					source,
					destination,
					false,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressComplete_Header".GetLocalizedResource(),
					"StatusCenter_DecompressComplete_Header",
					ReturnResult.Success,
					FileOperationType.Extract,
					source,
					destination,
					false);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressFailed_Header".GetLocalizedResource(),
					"StatusCenter_DecompressFailed_Header",
					ReturnResult.Failed,
					FileOperationType.Extract,
					source,
					destination,
					false);
			}
		}

		public static StatusCenterItem AddCard_EmptyRecycleBin(ReturnResult returnStatus)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinCancel_Header".GetLocalizedResource(),
					"StatusCenter_EmptyRecycleBinCancel_Header",
					ReturnResult.Cancelled,
					FileOperationType.Delete,
					null,
					null,
					false);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinInProgress_Header".GetLocalizedResource(),
					"StatusCenter_EmptyRecycleBinInProgress_Header",
					ReturnResult.InProgress,
					FileOperationType.Delete,
					null,
					null,
					false);
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinComplete_Header".GetLocalizedResource(),
					"StatusCenter_EmptyRecycleBinComplete_Header",
					ReturnResult.Success,
					FileOperationType.Delete,
					null,
					null,
					false);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinFailed_Header".GetLocalizedResource(),
					"StatusCenter_EmptyRecycleBinFailed_Header",
					ReturnResult.Failed,
					FileOperationType.Delete,
					null,
					null,
					false);
			}
		}

		public static StatusCenterItem AddCard_Prepare()
		{
			return _statusCenterViewModel.AddItem(
				"StatusCenter_Prepare_Header".GetLocalizedResource(),
				"StatusCenter_Prepare_Header",
				ReturnResult.InProgress,
				FileOperationType.Prepare,
				null,
				null,
				false);
		}

		public static void UpdateCardStrings(StatusCenterItem card, IEnumerable<string>? source, IEnumerable<string>? destination, long totalItemCount)
		{
			if (string.IsNullOrWhiteSpace(card.HeaderStringResource))
				return;

			// Aren't used for now
			string sourceDir = string.Empty;
			string destinationDir = string.Empty;

			string fileName = string.Empty;

			if (source is not null && source.Any())
			{
				sourceDir = PathNormalization.GetParentDir(source.First());
				fileName = source.First().Split('\\').Last();
			}

			if (destination is not null && destination.Any())
				destinationDir = PathNormalization.GetParentDir(destination.First());

			// Update string resources
			switch (card.Operation)
			{
				case FileOperationType.Copy:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource()),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
					};
					break;
				case FileOperationType.Move:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource()),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
					};
					break;
				case FileOperationType.Delete:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource()),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
					};
					break;
				case FileOperationType.Recycle:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource()),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), totalItemCount),
					};
					break;
				case FileOperationType.Extract:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
					};
					break;
				case FileOperationType.Compressed:
					card.Header = card.FileSystemOperationReturnResult switch
					{
						ReturnResult.Cancelled => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.Success => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.Failed => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						ReturnResult.InProgress => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
						_ => string.Format(card.HeaderStringResource.GetLocalizedResource(), fileName),
					};
					break;
			}
		}
	}
}
