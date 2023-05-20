// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem
{
	/// <summary>
	/// Represents a model for file system operation progress.
	/// </summary>
	public class FileSystemProgress
	{
		private readonly IProgress<FileSystemProgress>? _progress;

		private readonly IntervalSampler _sampler;

		private FileSystemStatusCode? _status;

		private bool _criticalReport;

		private bool _enumerationCompleted;

		public FileSystemStatusCode? Status
		{
			get => _status;
			set
			{
				if (_status != value)
					_criticalReport = true;

				_status = value;
			}
		}

		public string? FileName { get; set; }

		public long TotalSize { get; set; }

		public long ProcessedSize { get; set; }

		public long ItemsCount { get; set; }

		public long ProcessedItemsCount { get; set; }

		public DateTimeOffset StartTime { get; }

		public DateTimeOffset CompletedTime { get; private set; }

		public bool EnumerationCompleted
		{
			get => _enumerationCompleted;
			set
			{
				if (_enumerationCompleted != value)
					_criticalReport = true;

				_enumerationCompleted = value;
			}
		}

		// Only used when detailed count isn't available.
		public int? Percentage { get; set; }

		public FileSystemProgress(IProgress<FileSystemProgress>? progress, bool enumerationCompleted = false, FileSystemStatusCode? status = null, long itemsCount = 0, long totalSize = 0, int samplerInterval = 100)
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

			if (((EnumerationCompleted &&
				ProcessedItemsCount == ItemsCount &&
				ProcessedSize == TotalSize &&
				TotalSize is not 0) ||
				percentage is 100) &&
				_status is FileSystemStatusCode.InProgress or null)
			{
				_status = FileSystemStatusCode.Success;
			}

			if (_status is FileSystemStatusCode.Success)
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
