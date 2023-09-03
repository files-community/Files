// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows.Input;

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents an item for Status Center operation tasks.
	/// </summary>
	public sealed class StatusCenterItem : ObservableObject
	{
		private StatusCenterViewModel _viewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		private string? _SubHeader;
		public string? SubHeader
		{
			get => _SubHeader;
			set => SetProperty(ref _SubHeader, value);
		}

		private int _ProgressPercentage;
		public int ProgressPercentage
		{
			get => _ProgressPercentage;
			set => SetProperty(ref _ProgressPercentage, value);
		}

		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set
			{
				SetProperty(ref _IsExpanded, value);

				if (value)
					AnimatedIconState = "NormalOn";
				else
					AnimatedIconState = "NormalOff";
			}
		}

		private string _AnimatedIconState = "NormalOff";
		public string AnimatedIconState
		{
			get => _AnimatedIconState;
			set => SetProperty(ref _AnimatedIconState, value);
		}

		private bool _IsInProgress; // Item type is InProgress && is the operation in progress
		public bool IsInProgress
		{
			get => _IsInProgress;
			set
			{
				if (SetProperty(ref _IsInProgress, value))
					OnPropertyChanged(nameof(SubHeader));
			}
		}

		private bool _IsCancelled;
		public bool IsCancelled
		{
			get => _IsCancelled;
			set => SetProperty(ref _IsCancelled, value);
		}

		public CancellationToken CancellationToken
			=> _operationCancellationToken?.Token ?? default;

		public bool IsCancelable
			=> _operationCancellationToken is not null;

		public string HeaderBody { get; set; }

		public ReturnResult FileSystemOperationReturnResult { get; set; }

		public FileOperationType Operation { get; private set; }

		public StatusCenterItemKind ItemKind { get; private set; }

		public StatusCenterItemIconKind ItemIconKind { get; private set; }

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		private readonly CancellationTokenSource? _operationCancellationToken;

		public ICommand CancelCommand { get; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation, CancellationTokenSource operationCancellationToken = null)
		{
			_operationCancellationToken = operationCancellationToken;
			SubHeader = message;
			HeaderBody = title;
			Header = title;
			FileSystemOperationReturnResult = status;
			Operation = operation;
			ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);

			CancelCommand = new RelayCommand(ExecuteCancelCommand);

			switch (FileSystemOperationReturnResult)
			{
				case ReturnResult.InProgress:
					{
						IsInProgress = true;

						HeaderBody = Operation switch
						{
							FileOperationType.Extract => "ExtractInProgress/Title".GetLocalizedResource(),
							FileOperationType.Copy => "CopyInProgress/Title".GetLocalizedResource(),
							FileOperationType.Move => "MoveInProgress".GetLocalizedResource(),
							FileOperationType.Delete => "DeleteInProgress/Title".GetLocalizedResource(),
							FileOperationType.Recycle => "RecycleInProgress/Title".GetLocalizedResource(),
							FileOperationType.Prepare => "PrepareInProgress".GetLocalizedResource(),
							_ => "PrepareInProgress".GetLocalizedResource(),
						};

						Header = $"{HeaderBody} ({progress}%)";
						ItemKind = StatusCenterItemKind.InProgress;

						ItemIconKind = Operation switch
						{
							FileOperationType.Extract => StatusCenterItemIconKind.Extract,
							FileOperationType.Copy => StatusCenterItemIconKind.Copy,
							FileOperationType.Move => StatusCenterItemIconKind.Move,
							FileOperationType.Delete => StatusCenterItemIconKind.Delete,
							FileOperationType.Recycle => StatusCenterItemIconKind.Recycle,
							_ => StatusCenterItemIconKind.Delete,
						};

						break;
					}
				case ReturnResult.Success:
					{
						if (string.IsNullOrWhiteSpace(HeaderBody) || string.IsNullOrWhiteSpace(SubHeader))
							throw new NotImplementedException();

						Header = HeaderBody;
						ItemKind = StatusCenterItemKind.Successful;
						ItemIconKind = StatusCenterItemIconKind.Successful;

						break;
					}
				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
					{
						if (string.IsNullOrWhiteSpace(HeaderBody) || string.IsNullOrWhiteSpace(SubHeader))
							throw new NotImplementedException();

						Header = HeaderBody;
						ItemKind = StatusCenterItemKind.Error;
						ItemIconKind = StatusCenterItemIconKind.Error;

						break;
					}
			}
		}

		private void ReportProgressToBanner(FileSystemProgress value)
		{
			// File operation has been cancelled, so don't update the progress text
			if (CancellationToken.IsCancellationRequested)
				return;

			if (value.Status is FileSystemStatusCode status)
				FileSystemOperationReturnResult = status.ToStatus();

			IsInProgress = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is int p)
			{
				ProgressPercentage = p;
				Header = $"{HeaderBody} ({ProgressPercentage}%)";

				// TODO: Show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						ProgressPercentage = (int)(value.ProcessedSize * 100f / value.TotalSize);
						Header = $"{HeaderBody} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {ProgressPercentage}%)";
						break;

					case (not 0, _):
						ProgressPercentage = (int)(value.ProcessedSize * 100 / value.TotalSize);
						Header = $"{HeaderBody} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {ProgressPercentage}%)";
						break;

					case (_, not 0):
						ProgressPercentage = (int)(value.ProcessedItemsCount * 100 / value.ItemsCount);
						Header = $"{HeaderBody} ({value.ProcessedItemsCount} / {value.ItemsCount}: {ProgressPercentage}%)";
						break;

					default:
						Header = $"{HeaderBody}";
						break;
				}
			}
			else
			{
				Header = (value.ProcessedSize, value.ProcessedItemsCount) switch
				{
					(not 0, not 0) => $"{HeaderBody} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)",
					(not 0, _) => $"{HeaderBody} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) => $"{HeaderBody} ({value.ProcessedItemsCount} / ...)",
					_ => $"{HeaderBody}",
				};
			}

			_viewModel.NotifyChanges();
			_viewModel.UpdateAverageProgressValue();
		}

		public void ExecuteCancelCommand()
		{
			if (IsCancelable)
			{
				_operationCancellationToken?.Cancel();
				IsCancelled = true;
				Header = $"{HeaderBody} ({"canceling".GetLocalizedResource()})";
			}
		}
	}
}
