// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Provide static helper for the StatusCenter.
	/// </summary>
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem AddCard_Copy(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath> source,
			IEnumerable<string> destination,
			long itemsCount = 0,
			long totalSize = 0)
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
					"StatusCenter_CopyCanceled_Header",
					"StatusCenter_CopyCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress_Header",
					"StatusCenter_CopyInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete_Header",
					"StatusCenter_CopyComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed_Header",
					"StatusCenter_CopyFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Move(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath> source,
			IEnumerable<string> destination,
			long itemsCount = 0,
			long totalSize = 0)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header",
					"StatusCenter_MoveCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header",
					"StatusCenter_MoveInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header",
					"StatusCenter_MoveComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header",
					"StatusCenter_MoveFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Recycle(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath>? source,
			long itemsCount = 0,
			long totalSize = 0)
		{
			string? sourceDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteCanceled_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteFailed_Header",
					string.Empty,
					ReturnResult.Failed,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Delete(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath>? source,
			long itemsCount = 0,
			long totalSize = 0)
		{
			string? sourceDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteCanceled_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Delete,
					source?.Select(x => x.Path) ?? string.Empty.CreateEnumerable(),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteFailed_Header",
					"StatusCenter_DeleteFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Compress(
			IEnumerable<string> source,
			IEnumerable<string> destination,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressCanceled_Header",
					"StatusCenter_CompressCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressInProgress_Header",
					"StatusCenter_CompressInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Compressed,
					source,
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressComplete_Header",
					"StatusCenter_CompressComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressFailed_Header",
					"StatusCenter_CompressFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Decompress(
			IEnumerable<string> source,
			IEnumerable<string> destination,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressCanceled_Header",
					"StatusCenter_DecompressCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressInProgress_Header",
					"StatusCenter_DecompressInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Extract,
					source,
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressComplete_Header",
					"StatusCenter_DecompressComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressFailed_Header",
					"StatusCenter_DecompressFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_EmptyRecycleBin(
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinCancel_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinFailed_Header",
					"StatusCenter_EmptyRecycleBinFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_GitClone(
			IEnumerable<string> repoName,
			IEnumerable<string> destination,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitCloneCanceled_Header",
					"StatusCenter_GitCloneCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.GitClone,
					repoName,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitCloneInProgress_Header",
					"StatusCenter_GitCloneInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.GitClone,
					repoName,
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitCloneComplete_Header",
					"StatusCenter_GitCloneComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.GitClone,
					repoName,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitCloneFailed_Header",
					"StatusCenter_GitCloneFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.GitClone,
					repoName,
					destination,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_GitPush(
			string destination,
			string branchName,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPushCanceled_Header",
					"StatusCenter_GitPushCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.GitPush,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPushInProgress_Header",
					"StatusCenter_GitPushInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.GitPush,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPushComplete_Header",
					"StatusCenter_GitPushComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.GitPush,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPushFailed_Header",
					"StatusCenter_GitPushFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.GitPush,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_GitFetch(
			string remoteName,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitFetchCanceled_Header",
					"StatusCenter_GitFetchCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.GitFetch,
					remoteName.CreateEnumerable(),
					null,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitFetchInProgress_Header",
					"StatusCenter_GitFetchInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.GitFetch,
					remoteName.CreateEnumerable(),
					null,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitFetchComplete_Header",
					"StatusCenter_GitFetchComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.GitFetch,
					remoteName.CreateEnumerable(),
					null,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitFetchFailed_Header",
					"StatusCenter_GitFetchFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.GitFetch,
					remoteName.CreateEnumerable(),
					null,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_GitPull(
			string destination,
			string branchName,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPullCanceled_Header",
					"StatusCenter_GitPullCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.GitPull,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPullInProgress_Header",
					"StatusCenter_GitPullInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.GitPull,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPullComplete_Header",
					"StatusCenter_GitPullComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.GitPull,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_GitPullFailed_Header",
					"StatusCenter_GitPullFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.GitPull,
					branchName.CreateEnumerable(),
					destination.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_InstallFont(
			IEnumerable<string> source,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_InstallFontCanceled_Header",
					"StatusCenter_InstallFontCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.InstallFont,
					source,
					string.Empty.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_InstallFontInProgress_Header",
					"StatusCenter_InstallFontInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.InstallFont,
					source,
					string.Empty.CreateEnumerable(),
					false,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_InstallFontComplete_Header",
					"StatusCenter_InstallFontComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.InstallFont,
					source,
					string.Empty.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_InstallFontFailed_Header",
					"StatusCenter_InstallFontFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.InstallFont,
					source,
					string.Empty.CreateEnumerable(),
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Prepare()
		{
			return _statusCenterViewModel.AddItem(
				"StatusCenter_Prepare_Header",
				string.Empty,
				ReturnResult.InProgress,
				FileOperationType.Prepare,
				null,
				null,
				false);
		}

		public static void UpdateCardStrings(StatusCenterItem card, StatusCenterItemProgressModel? progressValue = null)
		{
			// Aren't used for now
			string sourcePath = string.Empty;
			string destinationPath = string.Empty;

			string sourceFileName = string.Empty;
			string sourceDirName = string.Empty;
			string destinationDirName = string.Empty;

			if (card.Source is not null && card.Source.Any())
			{
				// Include null check for items that don't have a parent dir
				// This can happen when dragging an image from the browser
				// https://github.com/files-community/Files/issues/13590
				if (card.Source.First() != null)
				{
					sourcePath = PathNormalization.GetParentDir(card.Source.First());
					sourceDirName = sourcePath.Split('\\').Last();
					sourceFileName = card.Source.First().Split('\\').Last();
				}
			}

			if (card.Destination is not null && card.Destination.Any())
			{
				destinationPath = PathNormalization.GetParentDir(card.Destination.First());
				destinationDirName = destinationPath.Split('\\').Last();
			}

			// Update string resources
			switch (card.Operation)
			{
				case FileOperationType.Copy:
					{
						string headerResource = card.HeaderStringResource;

						if (card.IsDiscovering && card.TotalItemsCount > 0 && card.IsInProgress)
							headerResource = "StatusCenter_CopyDiscovery_Header";

						if (string.IsNullOrWhiteSpace(headerResource))
							card.Header = string.Empty;
						else if (headerResource == "StatusCenter_CopyDiscovery_Header")
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount);
						else
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount, destinationDirName);

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath, destinationPath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.Move:
					{
						string headerResource = card.HeaderStringResource;

						if (card.IsDiscovering && card.TotalItemsCount > 0 && card.IsInProgress)
							headerResource = "StatusCenter_CopyDiscovery_Header";

						if (string.IsNullOrWhiteSpace(headerResource))
							card.Header = string.Empty;
						else if (headerResource == "StatusCenter_CopyDiscovery_Header")
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount);
						else
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount, destinationDirName);

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath, destinationPath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.Delete:
					{
						string headerResource = card.HeaderStringResource;

						if (card.IsDiscovering && card.TotalItemsCount > 0 && card.IsInProgress)
							headerResource = "StatusCenter_CopyDiscovery_Header";

						if (string.IsNullOrWhiteSpace(headerResource))
							card.Header = string.Empty;
						else if (headerResource == "StatusCenter_CopyDiscovery_Header")
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount);
						else
							card.Header = headerResource.GetLocalizedFormatResource(card.TotalItemsCount, sourceDirName);

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.Recycle:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourceDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.Extract:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(sourceFileName, destinationDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(sourceFileName, sourcePath, destinationPath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.Compressed:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, destinationDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath, destinationPath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.GitClone:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(sourcePath, destinationDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath, destinationPath);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.InstallFont:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(card.TotalItemsCount, sourcePath);
						card.SubHeader = subHeaderString;
						break;
					}

				case FileOperationType.GitPush:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(destinationDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(sourceFileName, destinationDirName);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.GitFetch:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(sourceFileName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(sourceFileName);
						card.SubHeader = subHeaderString;
						break;
					}
				case FileOperationType.GitPull:
					{
						string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty
							: card.HeaderStringResource.GetLocalizedFormatResource(destinationDirName);
						card.Header = headerString;

						string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty
							: card.SubHeaderStringResource.GetLocalizedFormatResource(sourceFileName, destinationDirName);
						card.SubHeader = subHeaderString;
						break;
					}
			}
		}
	}
}
