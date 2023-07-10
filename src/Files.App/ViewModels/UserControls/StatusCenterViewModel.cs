// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls
{
	public class StatusCenterViewModel : ObservableObject, IStatusCenterViewModel
	{
		public ObservableCollection<StatusCenterItem> StatusBannersSource { get; private set; } = new();

		private int _MedianOperationProgressValue = 0;
		public int MedianOperationProgressValue
		{
			get => _MedianOperationProgressValue;
			private set => SetProperty(ref _MedianOperationProgressValue, value);
		}

		public int OngoingOperationsCount
		{
			get
			{
				int count = 0;

				foreach (var item in StatusBannersSource)
				{
					if (item.IsProgressing)
						count++;
				}

				return count;
			}
		}

		public bool AnyOperationsOngoing
			=> OngoingOperationsCount > 0;

		public bool AnyBannersPresent
			=> StatusBannersSource.Any();

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
			=> OngoingOperationsCount > 0 ? OngoingOperationsCount : -1;

		public event EventHandler<StatusCenterPostedItem> ProgressBannerPosted;

		public StatusCenterViewModel()
		{
			StatusBannersSource.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AnyBannersPresent));
		}

		public StatusCenterPostedItem PostBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation)
		{
			StatusCenterItem banner = new(message, title, initialProgress, status, operation);
			StatusCenterPostedItem postedBanner = new(banner, this);

			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		public StatusCenterPostedItem PostOperationBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource)
		{
			StatusCenterItem banner = new(message, title, initialProgress, status, operation)
			{
				CancellationTokenSource = cancellationTokenSource,
			};

			StatusCenterPostedItem postedBanner = new(banner, this, cancellationTokenSource);

			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		public StatusCenterPostedItem PostActionBanner(string title, string message, string primaryButtonText, string cancelButtonText, Action primaryAction)
		{
			StatusCenterItem banner = new(message, title, primaryButtonText, cancelButtonText, primaryAction);
			StatusCenterPostedItem postedBanner = new(banner, this);

			StatusBannersSource.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		public bool CloseBanner(StatusCenterItem banner)
		{
			if (!StatusBannersSource.Contains(banner))
				return false;

			StatusBannersSource.Remove(banner);

			UpdateBanner(banner);

			return true;
		}

		public void UpdateBanner(StatusCenterItem banner)
		{
			OnPropertyChanged(nameof(OngoingOperationsCount));
			OnPropertyChanged(nameof(AnyOperationsOngoing));
			OnPropertyChanged(nameof(InfoBadgeState));
			OnPropertyChanged(nameof(InfoBadgeValue));
		}

		public void UpdateMedianProgress()
		{
			if (AnyOperationsOngoing)
				MedianOperationProgressValue = (int)StatusBannersSource.Where((item) => item.IsProgressing).Average(x => x.Progress);
		}
	}
}
