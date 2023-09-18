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
					// Cancel permanent deletion
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteCancel_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? itemsDeleted > 1
									? "StatusCenter_DeleteCancel_SubHeader_Plural".GetLocalizedResource()
									: "StatusCenter_DeleteCancel_SubHeader_Plural2".GetLocalizedResource()
								: "StatusCenter_DeleteCancel_SubHeader_Singular".GetLocalizedResource(),
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
					// Cancel recycling
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteCancel_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? itemsDeleted > 1
									? "StatusCenter_MoveCancel_SubHeader_Plural".GetLocalizedResource()
									: "StatusCenter_MoveCancel_SubHeader_Plural2".GetLocalizedResource()
								: "StatusCenter_MoveCancel_SubHeader_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource(), itemsDeleted),
						0,
						ReturnResult.Cancelled,
						FileOperationType.Recycle);
				}
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				if (permanently)
				{
					// Permanent deletion in progress
					return _statusCenterViewModel.AddItem(string.Empty,
						string.Format(
							source.Count() > 1
								? "StatusCenter_DeleteInProgress_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_DeleteInProgress_SubHeader_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir),
						0,
						ReturnResult.InProgress,
						FileOperationType.Delete,
						new CancellationTokenSource());
				}
				else
				{
					// Recycling in progress
					return _statusCenterViewModel.AddItem(string.Empty,
						string.Format(
							source.Count() > 1
								? "StatusCenter_MoveInProgress_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_MoveInProgress_SubHeader_Singular".GetLocalizedResource(),
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
					// Done done permanent deletion successfully
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteSuccessful_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusCenter_DeleteSuccessful_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_DeleteSuccessful_SubHeader_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							itemsDeleted),
						0,
						ReturnResult.Success,
						FileOperationType.Delete);
				}
				else
				{
					// Done done recycling successfully
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleSuccessful_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusCenter_DeleteSuccessful_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_DeleteSuccessful_SubHeader_Singular".GetLocalizedResource(),
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
					// Done permanent deletion with error
					return _statusCenterViewModel.AddItem(
						"StatusCenter_DeleteError_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusCenter_DeleteError_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_DeleteError_SubHeader_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir
							),
						0,
						ReturnResult.Failed,
						FileOperationType.Delete);
				}
				else
				{
					// Done recycling with error
					return _statusCenterViewModel.AddItem(
						"StatusCenter_RecycleError_Header".GetLocalizedResource(),
						string.Format(
							source.Count() > 1
								? "StatusCenter_MoveError_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_MoveError_SubHeader_Singular".GetLocalizedResource(),
							source.Count(),
							sourceDir,
							"TheRecycleBin".GetLocalizedResource()),
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
				// Cancel
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyCancel_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsCopied > 1
								? "StatusCenter_CopyCancel_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_CopyCancel_SubHeader_Plural2".GetLocalizedResource()
							: "StatusCenter_CopyCancel_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir,
						itemsCopied),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Copy);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				// In progress
				return _statusCenterViewModel.AddItem(
					string.Empty,
					string.Format(
						source.Count() > 1
							? "StatusCenter_CopyInProgress_SubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_CopyInProgress_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Copy, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				// Done successfully
				return _statusCenterViewModel.AddItem(
					"StatusCopySuccessful".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCenter_CopySuccessfulSubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_CopySuccessfulSubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						destinationDir,
						itemsCopied),
					0,
					ReturnResult.Success,
					FileOperationType.Copy);
			}
			else
			{
				// Done with error
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyError_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCenter_CopyError_SubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_CopyError_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir),
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
					"StatusCenter_MoveCancel_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsMoved > 1
								? "StatusCenter_MoveCancel_SubHeader_Plural".GetLocalizedResource()
								: "StatusCenter_MoveCancel_SubHeader_Plural2".GetLocalizedResource()
							: "StatusCenter_MoveCancel_SubHeader_Singular".GetLocalizedResource(),
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
				return _statusCenterViewModel.AddItem(
					string.Empty,
					string.Format(
						source.Count() > 1
							? "StatusCenter_MoveInProgress_SubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_MoveInProgress_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir),
					0,
					ReturnResult.InProgress,
					FileOperationType.Move, new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveSuccessful_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCenter_MoveSuccessful_SubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_MoveSuccessful_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir, destinationDir, itemsMoved),
					0,
					ReturnResult.Success,
					FileOperationType.Move);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveError_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? "StatusCenter_MoveError_SubHeader_Plural".GetLocalizedResource()
							: "StatusCenter_MoveError_SubHeader_Singular".GetLocalizedResource(),
						source.Count(),
						sourceDir, destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Move);
			}
		}

		public static StatusCenterItem PostBanner_Compress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsProcessedSuccessfully)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressCancel_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsProcessedSuccessfully > 1
								? "StatusCenter_CompressCancel_SubHeader".GetLocalizedResource()
								: "StatusCenter_CompressCancel_SubHeader".GetLocalizedResource()
							: "StatusCenter_CompressCancel_SubHeader".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir,
						itemsProcessedSuccessfully),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressInProgress_Header".GetLocalizedResource(),
					destinationDir,
					initialProgress: 0,
					ReturnResult.InProgress,
					FileOperationType.Compressed,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressSuccessful_Header".GetLocalizedResource(),
					string.Format(
						"StatusCenter_CompressSuccessful_SubHeader".GetLocalizedResource(),
						destinationDir),
					0,
					ReturnResult.Success,
					FileOperationType.Compressed);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressError_Header".GetLocalizedResource(),
					string.Format(
						"StatusCenter_CompressError_SubHeader".GetLocalizedResource(),
						destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Compressed
				);
			}
		}

		public static StatusCenterItem PostBanner_Decompress(IEnumerable<string> source, IEnumerable<string> destination, ReturnResult returnStatus, bool canceled, int itemsProcessedSuccessfully)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (canceled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressCancel_Header".GetLocalizedResource(),
					string.Format(
						source.Count() > 1
							? itemsProcessedSuccessfully > 1
								? "StatusCenter_DecompressCancel_SubHeader".GetLocalizedResource()
								: "StatusCenter_DecompressCancel_SubHeader".GetLocalizedResource()
							: "StatusCenter_DecompressCancel_SubHeader".GetLocalizedResource(),
						source.Count(),
						sourceDir,
						destinationDir,
						itemsProcessedSuccessfully),
					0,
					ReturnResult.Cancelled,
					FileOperationType.Move);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressInProgress_Header".GetLocalizedResource(),
					destinationDir,
					initialProgress: 0,
					ReturnResult.InProgress,
					FileOperationType.Compressed,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressSuccessful_Header".GetLocalizedResource(),
					string.Format(
						"StatusCenter_DecompressSuccessful_SubHeader".GetLocalizedResource(),
						destinationDir),
					0,
					ReturnResult.Success,
					FileOperationType.Compressed);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressError_Header".GetLocalizedResource(),
					string.Format(
						"StatusCenter_DecompressError_SubHeader".GetLocalizedResource(),
						destinationDir),
					0,
					ReturnResult.Failed,
					FileOperationType.Compressed
				);
			}
		}
	}
}
