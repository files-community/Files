using Files.App.Helpers;
using Files.Shared.Enums;
using System;

namespace Files.App.Filesystem
{
	public class FileSystemProgress
	{
		private readonly IProgress<FileSystemProgress>? progress;
		private readonly IntervalSampler sampler;
		private FileSystemStatusCode? status;
		private bool criticalReport;
		private bool enumerationCompleted;

		public FileSystemStatusCode? Status
		{
			get => status;
			set
			{
				if (status != value)
				{
					criticalReport = true;
				}
				status = value;
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
			get => enumerationCompleted;
			set
			{
				if (enumerationCompleted != value)
				{
					criticalReport = true;
				}
				enumerationCompleted = value;
			}
		}
		/// <summary>
		/// Only used when detailed count isn't available.
		/// </summary>
		public int? Percentage { get; set; }

		public FileSystemProgress(
			IProgress<FileSystemProgress>? progress,
			bool enumerationCompleted = false,
			FileSystemStatusCode? status = null,
			long itemsCount = 0,
			long totalSize = 0,
			int samplerInterval = 100)
		{
			StartTime = DateTimeOffset.Now;
			this.progress = progress;
			sampler = new(samplerInterval);
			EnumerationCompleted = enumerationCompleted;
			Status = status;
			ItemsCount = itemsCount;
			TotalSize = totalSize;
		}

		public void Report(int? percentage = null)
		{
			Percentage = percentage;
			if (EnumerationCompleted &&
				ProcessedItemsCount == ItemsCount &&
				ProcessedSize == TotalSize &&
				status is FileSystemStatusCode.InProgress or null)
			{
				status = FileSystemStatusCode.Success;
				CompletedTime = DateTimeOffset.Now;
			}

			if (status is FileSystemStatusCode.Success)
				CompletedTime = DateTimeOffset.Now;

			if (progress is not null && (criticalReport || sampler.CheckNow()))
			{
				progress.Report(this);
				criticalReport = false;
			}
		}

		public void ReportStatus(FileSystemStatusCode status)
		{
			Status = status;
			Report();
		}
	}
}
