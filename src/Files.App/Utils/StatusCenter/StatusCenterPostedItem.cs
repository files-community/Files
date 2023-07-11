// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents an item for StatusCenter to handle progress, such as cancelling.
	/// </summary>
	public class StatusCenterPostedItem
	{
		private readonly StatusCenterViewModel _statusCenterViewModel;

		private readonly StatusCenterItem _statusCenterItem;

		private readonly CancellationTokenSource? _cancellationTokenSource;

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken
			=> _cancellationTokenSource?.Token ?? default;

		public StatusCenterPostedItem(StatusCenterItem item)
		{
			_statusCenterItem = item;
			_statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public StatusCenterPostedItem(StatusCenterItem item, CancellationTokenSource cancellationTokenSource)
		{
			_statusCenterItem = item;
			_statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
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
				_statusCenterItem.ReturnResult = status.ToStatus();

			_statusCenterItem.IsProgressing = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				_statusCenterItem.Progress = p;
				_statusCenterItem.FullTitle = $"{_statusCenterItem.Title} ({_statusCenterItem.Progress}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						_statusCenterItem.Progress = (int)(value.ProcessedSize * 100f / value.TotalSize);
						_statusCenterItem.FullTitle = $"{_statusCenterItem.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {_statusCenterItem.Progress}%)";
						break;

					case (not 0, _):
						_statusCenterItem.Progress = (int)(value.ProcessedSize * 100 / value.TotalSize);
						_statusCenterItem.FullTitle = $"{_statusCenterItem.Title} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {_statusCenterItem.Progress}%)";
						break;

					case (_, not 0):
						_statusCenterItem.Progress = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						_statusCenterItem.FullTitle = $"{_statusCenterItem.Title} ({value.ProcessedItemsCount} / {value.ItemsCount}: {_statusCenterItem.Progress}%)";
						break;

					default:
						_statusCenterItem.FullTitle = $"{_statusCenterItem.Title} (...)";
						break;
				}
			}
			else
			{
				_statusCenterItem.FullTitle = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{_statusCenterItem.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{_statusCenterItem.Title} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{_statusCenterItem.Title} ({value.ProcessedItemsCount} / ...)",
					_ => $"{_statusCenterItem.Title} (...)",
				};
			}

			_statusCenterViewModel.NotifyPropertyChanges();
			_statusCenterViewModel.UpdateMedianProgress();
		}

		public void Remove()
		{
			_statusCenterViewModel.RemoveItem(_statusCenterItem);
		}

		public void RequestCancellation()
		{
			_cancellationTokenSource?.Cancel();
		}
	}
}
