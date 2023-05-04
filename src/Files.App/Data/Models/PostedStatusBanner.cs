namespace Files.App.Data.Models
{
	public class PostedStatusBanner
	{
		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		private readonly IOngoingTasksActions _ongoingTasksActions;

		private readonly StatusBanner _banner;

		private readonly CancellationTokenSource? _cancellationTokenSource;

		public CancellationToken CancellationToken
			=> _cancellationTokenSource?.Token ?? default;

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions ongoingTasksActions)
		{
			_banner = banner;
			_ongoingTasksActions = ongoingTasksActions;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions ongoingTasksActions, CancellationTokenSource cancellationTokenSource)
		{
			_banner = banner;
			_ongoingTasksActions = ongoingTasksActions;
			_cancellationTokenSource = cancellationTokenSource;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		private void ReportProgressToBanner(FileSystemProgress value)
		{
			// File operation has been cancelled, so don't update the progress text
			if (CancellationToken.IsCancellationRequested)
				return;

			if (value.Status is FileSystemStatusCode status)
				_banner.Status = status.ToStatus();

			_banner.IsProgressing = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				_banner.Progress = p;
				_banner.FullTitle = $"{_banner.Title} ({_banner.Progress}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						_banner.Progress = (int)(value.ProcessedSize * 100f / value.TotalSize);
						_banner.FullTitle = $"{_banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {_banner.Progress}%)";
						break;

					case (not 0, _):
						_banner.Progress = (int)(value.ProcessedSize * 100 / value.TotalSize);
						_banner.FullTitle = $"{_banner.Title} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {_banner.Progress}%)";
						break;

					case (_, not 0):
						_banner.Progress = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						_banner.FullTitle = $"{_banner.Title} ({value.ProcessedItemsCount} / {value.ItemsCount}: {_banner.Progress}%)";
						break;

					default:
						_banner.FullTitle = $"{_banner.Title} (...)";
						break;
				}
			}
			else
			{
				_banner.FullTitle = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{_banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{_banner.Title} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{_banner.Title} ({value.ProcessedItemsCount} / ...)",
					_ => $"{_banner.Title} (...)",
				};
			}

			_ongoingTasksActions.UpdateBanner(_banner);
			_ongoingTasksActions.UpdateMedianProgress();
		}

		public void Remove()
		{
			_ongoingTasksActions.CloseBanner(_banner);
		}

		public void RequestCancellation()
		{
			_cancellationTokenSource?.Cancel();
		}
	}
}
