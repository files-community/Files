// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents a model for file system operation progress.
	/// </summary>
	public class StatusCenterItemProgressModel : ObservableObject
	{
		private readonly IProgress<StatusCenterItemProgressModel>? _progress;

		private readonly IntervalSampler _sampler;

		private bool _criticalReport;

		private FileSystemStatusCode? _Status;
		public FileSystemStatusCode? Status
		{
			get => _Status;
			set
			{
				if (_Status != value)
					_criticalReport = true;

				SetProperty(ref _Status, value);
			}
		}

		private bool _EnumerationCompleted;
		public bool EnumerationCompleted
		{
			get => _EnumerationCompleted;
			set
			{
				if (_EnumerationCompleted != value)
					_criticalReport = true;

				SetProperty(ref _EnumerationCompleted, value);
			}
		}

		public string? _FileName;
		public string? FileName
		{
			get => _FileName;
			set => SetProperty(ref _FileName, value);
		}

		public long _TotalSize;
		public long TotalSize
		{
			get => _TotalSize;
			set => SetProperty(ref _TotalSize, value);
		}

		public long _ProcessedSize;
		public long ProcessedSize
		{
			get => _ProcessedSize;
			set => SetProperty(ref _ProcessedSize, value);
		}

		public long _ItemsCount;
		public long ItemsCount
		{
			get => _ItemsCount;
			set => SetProperty(ref _ItemsCount, value);
		}

		public long _ProcessedItemsCount;
		public long ProcessedItemsCount
		{
			get => _ProcessedItemsCount;
			set => SetProperty(ref _ProcessedItemsCount, value);
		}

		public DateTimeOffset _StartTime;
		public DateTimeOffset StartTime
		{
			get => _StartTime;
			set => SetProperty(ref _StartTime, value);
		}

		public DateTimeOffset _CompletedTime;
		public DateTimeOffset CompletedTime
		{
			get => _CompletedTime;
			set => SetProperty(ref _CompletedTime, value);
		}

		// Only used when detailed count isn't available.
		public int? Percentage { get; set; }

		public StatusCenterItemProgressModel(IProgress<StatusCenterItemProgressModel>? progress, bool enumerationCompleted = false, FileSystemStatusCode? status = null, long itemsCount = 0, long totalSize = 0, int samplerInterval = 100)
		{
			// Initialize
			_progress = progress;
			_sampler = new(samplerInterval);
			EnumerationCompleted = enumerationCompleted;
			Status = status;
			ItemsCount = itemsCount;
			TotalSize = totalSize;
			StartTime = DateTimeOffset.Now;
		}

		public void Report(int? percentage = null)
		{
			Percentage = percentage;

			if ((EnumerationCompleted &&
				ProcessedItemsCount == ItemsCount &&
				ProcessedSize == TotalSize &&
				TotalSize is not 0 ||
				percentage is 100) &&
				_Status is FileSystemStatusCode.InProgress or null)
			{
				_Status = FileSystemStatusCode.Success;
			}

			if (_Status is FileSystemStatusCode.Success)
				CompletedTime = DateTimeOffset.Now;

			if (_progress is not null && (_criticalReport || _sampler.CheckNow()))
			{
				_progress.Report(this);
				_criticalReport = false;
			}
		}

		public void ReportStatus(FileSystemStatusCode status)
		{
			Status = status;

			Report();
		}
	}
}
