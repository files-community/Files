// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents a model for file system operation progress.
	/// </summary>
	public class StatusCenterItemProgressModel : INotifyPropertyChanged
	{
		private readonly IProgress<StatusCenterItemProgressModel>? _progress;

		private readonly ConcurrentDictionary<string, bool> _dirtyTracker;

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

		private string? _FileName;
		public string? FileName
		{
			get => _FileName;
			set => SetProperty(ref _FileName, value);
		}

		private long _TotalSize;
		public long TotalSize
		{
			get => _TotalSize;
			set => SetProperty(ref _TotalSize, value);
		}

		private long _ProcessedSize;
		public long ProcessedSize
		{
			get => _ProcessedSize;
			set
			{
				SetProperty(ref _ProcessedSize, value);
				HistoricalSize.Add((DateTimeOffset.Now, value));
			}
		}

		private long _ItemsCount;
		public long ItemsCount
		{
			get => _ItemsCount;
			set => SetProperty(ref _ItemsCount, value);
		}

		private long _ProcessedItemsCount;
		public long ProcessedItemsCount
		{
			get => _ProcessedItemsCount;
			set => SetProperty(ref _ProcessedItemsCount, value);
		}

		private DateTimeOffset _StartTime;
		public DateTimeOffset StartTime
		{
			get => _StartTime;
			set => SetProperty(ref _StartTime, value);
		}

		private DateTimeOffset _CompletedTime;

		public DateTimeOffset CompletedTime
		{
			get => _CompletedTime;
			set => SetProperty(ref _CompletedTime, value);
		}

		public BulkConcurrentObservableCollection<(DateTimeOffset Time, long Size)> HistoricalSize { get; } = new();

		/// <summary>
		/// Only used when detailed count isn't available.
		/// You should NEVER set this property directly, instead, use <see cref="Report(int?)" /> to update the percentage.
		/// </summary>
		public int? Percentage { get; private set; }

		public event PropertyChangedEventHandler? PropertyChanged;

		public StatusCenterItemProgressModel(IProgress<StatusCenterItemProgressModel>? progress, bool enumerationCompleted = false, FileSystemStatusCode? status = null, long itemsCount = 0, long totalSize = 0, int samplerInterval = 100)
		{
			// Initialize
			_progress = progress;
			_sampler = new(samplerInterval);
			_dirtyTracker = new();
			EnumerationCompleted = enumerationCompleted;
			Status = status;
			ItemsCount = itemsCount;
			TotalSize = totalSize;
			StartTime = DateTimeOffset.Now;
			HistoricalSize.BeginBulkOperation();
		}

		private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			field = value;

			if (propertyName is not null)
			{
				_dirtyTracker[propertyName] = true;
			}
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

			if (_criticalReport || _sampler.CheckNow())
			{
				_criticalReport = false;
				foreach (var propertyName in _dirtyTracker.Keys)
				{
					if (_dirtyTracker[propertyName])
					{
						PropertyChanged?.Invoke(this, new(propertyName));
					}
				}
				_progress?.Report(this);
				HistoricalSize.EndBulkOperation();
				HistoricalSize.BeginBulkOperation();
			}
		}

		public void ReportStatus(FileSystemStatusCode status)
		{
			Status = status;

			Report();
		}
	}
}
