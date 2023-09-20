// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;
using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents an item for Status Center operation tasks.
	/// </summary>
	public sealed class StatusCenterItem : ObservableObject
	{
		private readonly StatusCenterViewModel _viewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		// TODO: Remove this string and its resources too
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
			set
			{
				ProgressPercentageText = $"{value}%";
				SetProperty(ref _ProgressPercentage, value);
			}
		}

		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set
			{
				AnimatedIconState = value ? "NormalOn" : "NormalOff";
				IsSubFooterVisible = !value;

				SetProperty(ref _IsExpanded, value);
			}
		}

		private bool _IsSubFooterVisible;
		public bool IsSubFooterVisible
		{
			get => _IsSubFooterVisible;
			set => SetProperty(ref _IsSubFooterVisible, value);
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

		private StatusCenterItemProgressModel _Progress = null!;
		public StatusCenterItemProgressModel Progress
		{
			get => _Progress;
			set => SetProperty(ref _Progress, value);
		}

		private string? _SpeedText;
		public string? SpeedText
		{
			get => _SpeedText;
			set => SetProperty(ref _SpeedText, value);
		}

		private string? _CurrentProcessingItemNameText;
		public string? CurrentProcessingItemNameText
		{
			get => _CurrentProcessingItemNameText;
			set => SetProperty(ref _CurrentProcessingItemNameText, value);
		}

		private string? _CurrentProcessedSizeText;
		public string? CurrentProcessedSizeText
		{
			get => _CurrentProcessedSizeText;
			set => SetProperty(ref _CurrentProcessedSizeText, value);
		}

		private string? _ProgressPercentageText;
		public string? ProgressPercentageText
		{
			get => _ProgressPercentageText;
			set => SetProperty(ref _ProgressPercentageText, value);
		}

		public ObservableCollection<ObservablePoint> Values { get; set; }

		public ObservableCollection<ISeries> Series { get; set; }

		public IList<ICartesianAxis> XAxes { get; set; } = new ICartesianAxis[]
		{
			new Axis
			{
				Padding = new Padding(0, 0),
				Labels = new List<string>(),
				MaxLimit = 100,

				ShowSeparatorLines = false,
			}
		};

		public IList<ICartesianAxis> YAxes { get; set; } = new ICartesianAxis[]
		{
			new Axis
			{
				Padding = new Padding(0, 0),
				Labels = new List<string>(),

				ShowSeparatorLines = false,
			}
		};

		public CancellationToken CancellationToken
			=> _operationCancellationToken?.Token ?? default;

		public bool IsCancelable
			=> _operationCancellationToken is not null;

		public string HeaderBody { get; set; }

		public ReturnResult FileSystemOperationReturnResult { get; set; }

		public FileOperationType Operation { get; private set; }

		public StatusCenterItemKind ItemKind { get; private set; }

		public StatusCenterItemIconKind ItemIconKind { get; private set; }

		public readonly Progress<StatusCenterItemProgressModel> ProgressEventSource;

		private readonly CancellationTokenSource? _operationCancellationToken;

		public ICommand CancelCommand { get; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation, CancellationTokenSource operationCancellationToken = default)
		{
			_operationCancellationToken = operationCancellationToken;
			SubHeader = message;
			HeaderBody = title;
			Header = title;
			FileSystemOperationReturnResult = status;
			Operation = operation;
			ProgressEventSource = new Progress<StatusCenterItemProgressModel>(ReportProgress);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);

			CancelCommand = new RelayCommand(ExecuteCancelCommand);

			Values = new();

			// TODO: One-time fetch could cause an issue where the color won't be changed when user change accent color
			var accentBrush = App.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

			Series = new()
			{
				new LineSeries<ObservablePoint>
				{
					Values = Values,
					GeometrySize = 0d,
					DataPadding = new(0, 0),
					IsHoverable = false,

					// Stroke
					Stroke = new SolidColorPaint(
						new(accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B),
						1),

					// Fill under the stroke
					Fill = new LinearGradientPaint(
						new SKColor[] {
							new(accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B, 50),
							new(accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B, 10)
						},
						new(0.5f, 0f),
						new(0.5f, 1.0f),
						new[] { 0.2f, 1.3f }),
				}
			};

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

		private void ReportProgress(StatusCenterItemProgressModel value)
		{
			// The Operation has been cancelled. Do update neither progress value nor text.
			if (CancellationToken.IsCancellationRequested)
				return;

			// Update status code
			if (value.Status is FileSystemStatusCode status)
				FileSystemOperationReturnResult = status.ToStatus();

			// Get if the operation is in progress
			IsInProgress = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is double p)
			{
				if (ProgressPercentage != value.Percentage)
				{
					ProgressPercentage = (int)p;

					CurrentProcessedSizeText = string.Format(
						"StatusCenter_ProcessedSize".GetLocalizedResource(),
						value.ProcessedSize.ToSizeString(),
						value.TotalSize.ToSizeString());
				}

				if (CurrentProcessingItemNameText != value.FileName)
				{
					CurrentProcessingItemNameText = value.FileName;
				}
			}

			ObservablePoint point;
			switch (value.TotalSize, value.ItemsCount)
			{
				// In progress, displaying items count & processed size
				case (not 0, not 0):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedSize * 100.0 / value.TotalSize), 0, 100);
					SpeedText = $"{value.ProcessingItemsCountSpeed:0} items ({value.ProcessingSizeSpeed.ToSizeString()})/s";
					point = new(value.ProcessedSize * 100.0 / value.TotalSize, value.ProcessingSizeSpeed);
					break;
				// In progress, displaying processed size
				case (not 0, _):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedSize * 100.0 / value.TotalSize), 0, 100);
					SpeedText = $"{value.ProcessingSizeSpeed.ToSizeString()}/s";
					point = new(value.ProcessedSize * 100.0 / value.TotalSize, value.ProcessingSizeSpeed);
					break;
				// In progress, displaying items count
				case (_, not 0):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedItemsCount * 100.0 / value.ItemsCount), 0, 100);
					SpeedText = $"{value.ProcessingItemsCountSpeed:0} items/s";
					point = new(value.ProcessedItemsCount * 100.0 / value.ItemsCount, value.ProcessingItemsCountSpeed);
					break;
				default:
					point = new(ProgressPercentage, value.ProcessingItemsCountSpeed);
					SpeedText = (value.ProcessedSize, value.ProcessedItemsCount) switch
					{
						(not 0, not 0) => $"{value.ProcessingItemsCountSpeed:0} items ({value.ProcessingSizeSpeed.ToSizeString()})/s",
						(not 0, _) => $"{value.ProcessingSizeSpeed.ToSizeString()}/s",
						(_, not 0) => $"{value.ProcessingItemsCountSpeed:0} items/s",
						_ => "N/A",
					};
					break;
			}

			if (Values.FirstOrDefault(v => v.X == point.X) is ObservablePoint existingPoint)
			{
				Values.Remove(existingPoint);
			}

			Values.Add(point);

			Header = $"{HeaderBody}";

			if (value.FileName is not null)
			{
				SubHeader = value.FileName;
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
