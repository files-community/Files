using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Interacts;
using Files.Shared.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public class OngoingTasksViewModel : ObservableObject, IOngoingTasksActions
	{
		#region Public Properties

		public ObservableCollection<StatusBanner> StatusBannersSource { get; private set; } = new ObservableCollection<StatusBanner>();

		private float medianOperationProgressValue = 0.0f;

		public OngoingTasksViewModel()
		{
			StatusBannersSource.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AnyBannersPresent));
		}

		public float MedianOperationProgressValue
		{
			get => medianOperationProgressValue;
			private set => SetProperty(ref medianOperationProgressValue, value);
		}

		public int OngoingOperationsCount
		{
			get
			{
				int count = 0;

				foreach (var item in StatusBannersSource)
				{
					if (item.IsProgressing)
					{
						count++;
					}
				}

				return count;
			}
		}

		public bool AnyOperationsOngoing
		{
			get => OngoingOperationsCount > 0;
		}

		public bool AnyBannersPresent
		{
			get => StatusBannersSource.Any();
		}

		public int InfoBadgeState
		{
			get
			{
				var anyFailure = StatusBannersSource.Any(i => i.Status != ReturnResult.InProgress && i.Status != ReturnResult.Success);

				return (anyFailure, AnyOperationsOngoing) switch
				{
					(false, false) => 0, // all success
					(false, true) => 1, // ongoing
					(true, true) => 2, // onging with failure
					(true, false) => 3 // completed with failure
				};
			}
		}

		public int InfoBadgeValue
		{
			get => OngoingOperationsCount > 0 ? OngoingOperationsCount : -1;
		}

		#endregion Public Properties

		#region Events

		public event EventHandler<PostedStatusBanner> ProgressBannerPosted;

		#endregion Events

		#region IOngoingTasksActions

		public PostedStatusBanner PostBanner(string title, string message, float initialProgress, ReturnResult status, FileOperationType operation)
		{
			StatusBanner banner = new StatusBanner(message, title, initialProgress, status, operation);
			PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this);
			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);
			UpdateBanner(banner);
			return postedBanner;
		}

		public PostedStatusBanner PostOperationBanner(string title, string message, float initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource)
		{
			StatusBanner banner = new StatusBanner(message, title, initialProgress, status, operation)
			{
				CancellationTokenSource = cancellationTokenSource,
			};
			PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this, cancellationTokenSource);
			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);
			UpdateBanner(banner);
			return postedBanner;
		}

		public PostedStatusBanner PostActionBanner(string title, string message, string primaryButtonText, string cancelButtonText, Action primaryAction)
		{
			StatusBanner banner = new StatusBanner(message, title, primaryButtonText, cancelButtonText, primaryAction);
			PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this);
			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);
			UpdateBanner(banner);
			return postedBanner;
		}

		public bool CloseBanner(StatusBanner banner)
		{
			if (!StatusBannersSource.Contains(banner))
			{
				return false;
			}

			StatusBannersSource.Remove(banner);
			UpdateBanner(banner);
			return true;
		}

		public void UpdateBanner(StatusBanner banner)
		{
			OnPropertyChanged(nameof(OngoingOperationsCount));
			OnPropertyChanged(nameof(AnyOperationsOngoing));
			OnPropertyChanged(nameof(InfoBadgeState));
			OnPropertyChanged(nameof(InfoBadgeValue));
		}

		public void UpdateMedianProgress()
		{
			if (AnyOperationsOngoing)
			{
				MedianOperationProgressValue = StatusBannersSource.Where((item) => item.IsProgressing).Average(x => x.Progress);
			}
		}

		#endregion IOngoingTasksActions
	}

	public class PostedStatusBanner
	{
		#region Private Members

		private readonly IOngoingTasksActions OngoingTasksActions;

		private readonly StatusBanner Banner;

		private readonly CancellationTokenSource cancellationTokenSource;

		#endregion Private Members

		#region Public Members

		public readonly FileSystemProgress Progress;
		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken => cancellationTokenSource?.Token ?? default;

		#endregion Public Members

		#region Constructor

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions OngoingTasksActions)
		{
			this.Banner = banner;
			this.OngoingTasksActions = OngoingTasksActions;
			this.ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			this.Progress = new(this.ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		public PostedStatusBanner(StatusBanner banner, IOngoingTasksActions OngoingTasksActions, CancellationTokenSource cancellationTokenSource)
		{
			this.Banner = banner;
			this.OngoingTasksActions = OngoingTasksActions;
			this.cancellationTokenSource = cancellationTokenSource;
			this.ProgressEventSource = new Progress<FileSystemProgress>(ReportProgressToBanner);
			this.Progress = new(this.ProgressEventSource, status: FileSystemStatusCode.InProgress);
		}

		#endregion Constructor

		#region Private Helpers

		private void ReportProgressToBanner(FileSystemProgress value)
		{
			if (CancellationToken.IsCancellationRequested) // file operation has been cancelled, so don't update the progress text
			{
				return;
			}

			if (value.Status is FileSystemStatusCode status)
				Banner.Status = status.ToStatus();

			Banner.IsProgressing = (value.Status & FileSystemStatusCode.InProgress) != 0;

			if (value.Percentage is float f)
			{
				Banner.Progress = f;
				Banner.FullTitle = $"{Banner.Title} ({Banner.Progress:0.00}%)";
				// TODO: show detailed progress if Size/Count information available
			}
			else if (value.EnumerationCompleted)
			{
				switch (value.TotalSize, value.ItemsCount)
				{
					case (not 0, not 0):
						Banner.Progress = value.ProcessedSize * 100f / value.TotalSize;
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / {value.ItemsCount} ({value.TotalSize.ToSizeString()}): {Banner.Progress:0.00}%)";
						break;
					case (not 0, _):
						Banner.Progress = value.ProcessedSize * 100f / value.TotalSize;
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedSize.ToSizeString()} / {value.TotalSize.ToSizeString()}: {Banner.Progress:0.00}%)";
						break;
					case (_, not 0):
						Banner.Progress = value.ProcessedItemsCount * 100f / value.ItemsCount;
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} / {value.ItemsCount}: {Banner.Progress:0.00}%)";
						break;
					default:
						Banner.FullTitle = $"{Banner.Title} (...)";
						break;
				}
			}
			else
			{
				switch (value.ProcessedSize, value.ProcessedItemsCount)
				{
					case (not 0, not 0):
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} ({value.ProcessedSize.ToSizeString()}) / ...)";
						break;
					case (not 0, _):
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedSize.ToSizeString()} / ...)";
						break;
					case (_, not 0):
						Banner.FullTitle = $"{Banner.Title} ({value.ProcessedItemsCount} / ...)";
						break;
					default:
						Banner.FullTitle = $"{Banner.Title} (...)";
						break;
				}
			}

			OngoingTasksActions.UpdateBanner(Banner);
			OngoingTasksActions.UpdateMedianProgress();
		}

		#endregion Private Helpers

		#region Public Helpers

		public void Remove()
		{
			OngoingTasksActions.CloseBanner(Banner);
		}

		public void RequestCancellation()
		{
			cancellationTokenSource?.Cancel();
		}

		#endregion Public Helpers
	}

	public class StatusBanner : ObservableObject
	{
		#region Private Members

		private readonly float initialProgress = 0.0f;

		private string fullTitle;

		private bool isCancelled;

		#endregion Private Members

		#region Public Properties

		private float progress = 0.0f;

		public float Progress
		{
			get => progress;
			set
			{
				SetProperty(ref progress, value);
			}
		}

		private bool isProgressing = false;

		public bool IsProgressing
		{
			get => isProgressing;
			set
			{
				SetProperty(ref isProgressing, value);
			}
		}

		public string Title { get; private set; }

		private ReturnResult status = ReturnResult.InProgress;

		public ReturnResult Status
		{
			get => status;
			set
			{
				SetProperty(ref status, value);
			}
		}

		public FileOperationType Operation { get; private set; }

		public string Message { get; private set; }

		public SolidColorBrush StrokeColor { get; private set; } = new SolidColorBrush(Colors.DeepSkyBlue);

		public IconSource GlyphSource { get; private set; }

		public string PrimaryButtonText { get; set; }

		public string SecondaryButtonText { get; set; } = "Cancel";

		public Action PrimaryButtonClick { get; }

		public ICommand CancelCommand { get; }

		public bool SolutionButtonsVisible { get; } = false;

		public bool CancelButtonVisible => CancellationTokenSource is not null;

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public string FullTitle
		{
			get => fullTitle;
			set => SetProperty(ref fullTitle, value ?? string.Empty);
		}

		public bool IsCancelled
		{
			get => isCancelled;
			set => SetProperty(ref isCancelled, value);
		}

		#endregion Public Properties

		public StatusBanner(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			Message = message;
			Title = title;
			FullTitle = title;
			initialProgress = progress;
			Status = status;
			Operation = operation;

			CancelCommand = new RelayCommand(CancelOperation);

			switch (Status)
			{
				case ReturnResult.InProgress:
					IsProgressing = true;
					if (string.IsNullOrWhiteSpace(Title))
					{
						switch (Operation)
						{
							case FileOperationType.Extract:
								Title = "ExtractInProgress/Title".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									FontFamily = Application.Current.Resources["CustomGlyph"] as FontFamily,
									Glyph = "\xF11A"    // Extract glyph
								};
								break;

							case FileOperationType.Copy:
								Title = "CopyInProgress/Title".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									Glyph = "\xE8C8"    // Copy glyph
								};
								break;

							case FileOperationType.Move:
								Title = "MoveInProgress".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									Glyph = "\xE77F"    // Move glyph
								};
								break;

							case FileOperationType.Delete:
								Title = "DeleteInProgress/Title".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									Glyph = "\xE74D"    // Delete glyph
								};
								break;

							case FileOperationType.Recycle:
								Title = "RecycleInProgress/Title".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
									Glyph = "\xEF87"    // RecycleBin Custom Glyph
								};
								break;

							case FileOperationType.Prepare:
								Title = "PrepareInProgress".GetLocalizedResource();
								GlyphSource = new FontIconSource()
								{
									Glyph = "\xE89A"
								};
								break;
						}
					}
					FullTitle = $"{Title} ({initialProgress}%)";
					break;

				case ReturnResult.Success:
					IsProgressing = false;
					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
					{
						throw new NotImplementedException();
					}
					else
					{
						FullTitle = Title;
						StrokeColor = new SolidColorBrush(Colors.Green);
						GlyphSource = new FontIconSource()
						{
							Glyph = "\xE73E"    // CheckMark glyph
						};
					}
					break;

				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
					IsProgressing = false;
					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
					{
						throw new NotImplementedException();
					}
					else
					{
						// Expanded banner
						FullTitle = Title;
						StrokeColor = new SolidColorBrush(Colors.Red);
						GlyphSource = new FontIconSource()
						{
							Glyph = "\xE783"    // Error glyph
						};
					}
					break;
			}
		}

		/// <summary>
		/// Post an error message banner following a failed operation
		/// </summary>
		/// <param name="message"></param>
		/// <param name="title"></param>
		/// <param name="primaryButtonText">Solution buttons are not visible if this property is an empty string</param>
		/// <param name="secondaryButtonText">Set to "Cancel" by default</param>
		public StatusBanner(string message, string title, string primaryButtonText, string secondaryButtonText, Action primaryButtonClicked)
		{
			Message = message;
			Title = title;
			PrimaryButtonText = primaryButtonText;
			SecondaryButtonText = secondaryButtonText;
			PrimaryButtonClick = primaryButtonClicked;
			Status = ReturnResult.Failed;

			CancelCommand = new RelayCommand(CancelOperation);

			if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
			{
				throw new NotImplementedException();
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(PrimaryButtonText))
				{
					SolutionButtonsVisible = true;
				}

				// Expanded banner
				FullTitle = Title;
				StrokeColor = new SolidColorBrush(Colors.Red);
				GlyphSource = new FontIconSource()
				{
					Glyph = "\xE783" // Error glyph
				};
			}
		}

		public void CancelOperation()
		{
			if (CancelButtonVisible)
			{
				CancellationTokenSource.Cancel();
				IsCancelled = true;
				FullTitle = $"{Title} ({"canceling".GetLocalizedResource()})";
			}
		}
	}
}