// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public class StatusCenterPostItem
	{
		private readonly StatusCenterViewModel _statusCenterViewModel;

		private readonly StatusCenterItem Banner;

		private readonly CancellationTokenSource cancellationTokenSource;

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken
			=> cancellationTokenSource?.Token ?? default;

		public StatusCenterPostItem(StatusCenterItem banner, StatusCenterViewModel OngoingTasksActions)
		{
			Banner = banner;
			this._statusCenterViewModel = OngoingTasksActions;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public StatusCenterPostItem(StatusCenterItem banner, StatusCenterViewModel OngoingTasksActions, CancellationTokenSource cancellationTokenSource)
		{
			Banner = banner;
			this._statusCenterViewModel = OngoingTasksActions;
			this.cancellationTokenSource = cancellationTokenSource;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		private void ReportProgressToBanner(FileSystemProgress value)
		{
			// File operation has been cancelled, so don't update the progress text
			if (CancellationToken.IsCancellationRequested)
				return;

			if (value.Status is FileSystemStatusCode status)
				Banner.FileSystemOperationReturnResult = status.ToStatus();

			Banner.IsInProgress = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				Banner.ProgressPercentage = p;
				Banner.Header = $"{Banner.HeaderBody} ({Banner.ProgressPercentage}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						Banner.ProgressPercentage = (int)(value.ProcessedSize * 100f / value.TotalSize);
						Banner.Header = $"{Banner.HeaderBody} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {Banner.ProgressPercentage}%)";
						break;

					case (not 0, _):
						Banner.ProgressPercentage = (int)(value.ProcessedSize * 100 / value.TotalSize);
						Banner.Header = $"{Banner.HeaderBody} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {Banner.ProgressPercentage}%)";
						break;

					case (_, not 0):
						Banner.ProgressPercentage = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						Banner.Header = $"{Banner.HeaderBody} ({value.ProcessedItemsCount} / {value.ItemsCount}: {Banner.ProgressPercentage}%)";
						break;

					default:
						Banner.Header = $"{Banner.HeaderBody}";
						break;
				}
			}
			else
			{
				Banner.Header = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{Banner.HeaderBody} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{Banner.HeaderBody} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{Banner.HeaderBody} ({value.ProcessedItemsCount} / ...)",
					_ => $"{Banner.HeaderBody}",
				};
			}

			_statusCenterViewModel.UpdateBanner(Banner);
			_statusCenterViewModel.UpdateMedianProgress();
		}

		public void Remove()
		{
			_statusCenterViewModel.CloseBanner(Banner);
		}

		public void RequestCancellation()
		{
			cancellationTokenSource?.Cancel();
		}
	}
}
