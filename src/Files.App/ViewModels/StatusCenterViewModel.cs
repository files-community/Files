using Files.App.Interacts;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public class OngoingTasksViewModel : ObservableObject, IOngoingTasksActions
	{
		// Public Properties

		public ObservableCollection<StatusBanner> StatusBannersSource { get; private set; } = new ObservableCollection<StatusBanner>();

		private int medianOperationProgressValue = 0;
		public int MedianOperationProgressValue
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
					(false, false) => 0, // All success
					(false, true) => 1,  // Ongoing
					(true, true) => 2,   // Ongoing with failure
					(true, false) => 3   // Completed with failure
				};
			}
		}

		public int InfoBadgeValue
		{
			get => OngoingOperationsCount > 0 ? OngoingOperationsCount : -1;
		}

		// Events

		public event EventHandler<PostedStatusBanner> ProgressBannerPosted;

		// Constructors

		public OngoingTasksViewModel()
		{
			StatusBannersSource.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AnyBannersPresent));
		}

		// IOngoingTasksActions

		public PostedStatusBanner PostBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation)
		{
			StatusBanner banner = new StatusBanner(message, title, initialProgress, status, operation);
			PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this);

			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		public PostedStatusBanner PostOperationBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource)
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
				MedianOperationProgressValue = (int)StatusBannersSource.Where((item) => item.IsProgressing).Average(x => x.Progress);
			}
		}
	}

	public class PostedStatusBanner
	{
		// Private Members

		private readonly IOngoingTasksActions OngoingTasksActions;

		private readonly StatusBanner Banner;

		private readonly CancellationTokenSource cancellationTokenSource;


		// Public Members

		public readonly FileSystemProgress Progress;

		public readonly Progress<FileSystemProgress> ProgressEventSource;

		public CancellationToken CancellationToken => cancellationTokenSource?.Token ?? default;

		// Constructor

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

		// Private Helpers

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
					(not 0, _) =>     $"{Banner.Title} ({value.ProcessedSize.ToSizeString()} / ...)",
					(_, not 0) =>     $"{Banner.Title} ({value.ProcessedItemsCount} / ...)",
					_ =>              $"{Banner.Title} (...)",
				};
			}

			OngoingTasksActions.UpdateBanner(Banner);
			OngoingTasksActions.UpdateMedianProgress();
		}

		// Public Helpers

		public void Remove()
		{
			OngoingTasksActions.CloseBanner(Banner);
		}

		public void RequestCancellation()
		{
			cancellationTokenSource?.Cancel();
		}
	}

	public class StatusBanner : ObservableObject
	{
		#region Private Members

		private readonly float initialProgress = 0.0f;

		private string fullTitle;

		private bool isCancelled;

		#endregion Private Members

		#region Public Properties

		private int progress = 0;
		public int Progress
		{
			get => progress;
			set => SetProperty(ref progress, value);
		}

		private bool isProgressing = false;

		public bool IsProgressing
		{
			get => isProgressing;
			set => SetProperty(ref isProgressing, value);
		}

		public string Title { get; private set; }

		private ReturnResult status = ReturnResult.InProgress;
		public ReturnResult Status
		{
			get => status;
			set => SetProperty(ref status, value);
		}

		public FileOperationType Operation { get; private set; }

		public string Message { get; private set; }

		public InfoBarSeverity InfoBarSeverity { get; private set; } = InfoBarSeverity.Informational;

		public string PrimaryButtonText { get; set; }

		public string SecondaryButtonText { get; set; } = "Cancel";

		public Action PrimaryButtonClick { get; }

		public ICommand CancelCommand { get; }

		public bool SolutionButtonsVisible { get; } = false;

		public bool CancelButtonVisible
			=> CancellationTokenSource is not null;

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
								break;

							case FileOperationType.Copy:
								Title = "CopyInProgress/Title".GetLocalizedResource();
								break;

							case FileOperationType.Move:
								Title = "MoveInProgress".GetLocalizedResource();
								break;

							case FileOperationType.Delete:
								Title = "DeleteInProgress/Title".GetLocalizedResource();
								break;

							case FileOperationType.Recycle:
								Title = "RecycleInProgress/Title".GetLocalizedResource();
								break;

							case FileOperationType.Prepare:
								Title = "PrepareInProgress".GetLocalizedResource();
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
						InfoBarSeverity = InfoBarSeverity.Success;
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
						InfoBarSeverity = InfoBarSeverity.Error;
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
				InfoBarSeverity = InfoBarSeverity.Error;
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
