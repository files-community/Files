// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	public class StatusCenterPostedItem
	{
		private readonly IStatusCenterViewModel _statusCenterViewModel;

		private readonly StatusCenterItem __statusCenterItem;

		private readonly CancellationTokenSource _cancellationTokenSource;

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken
			=> _cancellationTokenSource?.Token ?? default;

		public StatusCenterPostedItem(StatusCenterItem banner, IStatusCenterViewModel OngoingTasksActions)
		{
			__statusCenterItem = banner;
			this._statusCenterViewModel = OngoingTasksActions;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public StatusCenterPostedItem(StatusCenterItem banner, IStatusCenterViewModel OngoingTasksActions, CancellationTokenSource cancellationTokenSource)
		{
			__statusCenterItem = banner;
			this._statusCenterViewModel = OngoingTasksActions;
			this._cancellationTokenSource = cancellationTokenSource;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		private void ReportProgressToBanner(FileSystemProgress value)
		{
			// File operation has been cancelled, so don't update the progress text
			if (CancellationToken.IsCancellationRequested)
				return;

			if (value.Status is FileSystemStatusCode status)
				__statusCenterItem.Status = status.ToStatus();

			__statusCenterItem.IsProgressing = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				__statusCenterItem.Progress = p;
				__statusCenterItem.FullTitle = $"{__statusCenterItem.Title} ({__statusCenterItem.Progress}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						__statusCenterItem.Progress = (int)(value.ProcessedSize * 100f / value.TotalSize);
						__statusCenterItem.FullTitle = $"{__statusCenterItem.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {__statusCenterItem.Progress}%)";
						break;

					case (not 0, _):
						__statusCenterItem.Progress = (int)(value.ProcessedSize * 100 / value.TotalSize);
						__statusCenterItem.FullTitle = $"{__statusCenterItem.Title} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {__statusCenterItem.Progress}%)";
						break;

					case (_, not 0):
						__statusCenterItem.Progress = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						__statusCenterItem.FullTitle = $"{__statusCenterItem.Title} ({value.ProcessedItemsCount} / {value.ItemsCount}: {__statusCenterItem.Progress}%)";
						break;

					default:
						__statusCenterItem.FullTitle = $"{__statusCenterItem.Title} (...)";
						break;
				}
			}
			else
			{
				__statusCenterItem.FullTitle = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{__statusCenterItem.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{__statusCenterItem.Title} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{__statusCenterItem.Title} ({value.ProcessedItemsCount} / ...)",
					_ => $"{__statusCenterItem.Title} (...)",
				};
			}

			_statusCenterViewModel.UpdateBanner(__statusCenterItem);
			_statusCenterViewModel.UpdateMedianProgress();
		}

		public void Remove()
		{
			_statusCenterViewModel.CloseBanner(__statusCenterItem);
		}

		public void RequestCancellation()
		{
			_cancellationTokenSource?.Cancel();
		}
	}
}
