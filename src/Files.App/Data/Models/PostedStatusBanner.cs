namespace Files.App.Data.Models
{
	public class PostedStatusBanner
	{
		private readonly IOngoingTasksActions OngoingTasksActions;

		private readonly StatusBanner Banner;

		private readonly CancellationTokenSource cancellationTokenSource;

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken => cancellationTokenSource?.Token ?? default;

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions OngoingTasksActions)
		{
			Banner = banner;
			this.OngoingTasksActions = OngoingTasksActions;

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions OngoingTasksActions, CancellationTokenSource cancellationTokenSource)
		{
			Banner = banner;
			this.OngoingTasksActions = OngoingTasksActions;
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
				Banner.Status = status.ToStatus();

			Banner.IsProgressing = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				Banner.Progress = p;
				Banner.FullTitle = $"{Banner.Title} ({Banner.Progress}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						Banner.Progress = (int)(value.ProcessedSize * 100f / value.TotalSize);
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {Banner.Progress}%)";
						break;

					case (not 0, _):
						Banner.Progress = (int)(value.ProcessedSize * 100 / value.TotalSize);
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {Banner.Progress}%)";
						break;

					case (_, not 0):
						Banner.Progress = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} / {value.ItemsCount}: {Banner.Progress}%)";
						break;

					default:
						Banner.FullTitle = $"{Banner.Title} (...)";
						break;
				}
			}
			else
			{
				Banner.FullTitle = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{Banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{Banner.Title} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{Banner.Title} ({value.ProcessedItemsCount} / ...)",
					_ => $"{Banner.Title} (...)",
				};
			}

			OngoingTasksActions.UpdateBanner(Banner);
			OngoingTasksActions.UpdateMedianProgress();
		}

		public void Remove()
		{
			OngoingTasksActions.CloseBanner(Banner);
		}

		public void RequestCancellation()
		{
			cancellationTokenSource?.Cancel();
		}
	}
}
