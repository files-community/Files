// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.StatusCenter;

namespace Files.App.ViewModels.UserControls
{
	public class StatusCenterViewModel : ObservableObject
	{
		public ObservableCollection<StatusCenterItem> StatusCenterItems { get; } = new();

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

				foreach (var item in StatusCenterItems)
				{
					if (item.IsInProgress)
						count++;
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
			get => StatusCenterItems.Any();
		}

		public int InfoBadgeState
		{
			get
			{
				var anyFailure = StatusCenterItems.Any(i => i.FileSystemOperationReturnResult != ReturnResult.InProgress && i.FileSystemOperationReturnResult != ReturnResult.Success);

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

		public event EventHandler<StatusCenterPostItem> ProgressBannerPosted;

		public StatusCenterViewModel()
		{
			StatusCenterItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AnyBannersPresent));
		}

		/// <summary>
		/// Posts a new banner to the Status Center control for an operation.
		/// It may be used to return the progress, success, or failure of the respective operation.
		/// </summary>
		/// <param name="title">Reserved for success and error banners. Otherwise, pass an empty string for this argument.</param>
		/// <param name="message"></param>
		/// <param name="initialProgress"></param>
		/// <param name="status"></param>
		/// <param name="operation"></param>
		/// <returns>A StatusBanner object which may be used to track/update the progress of an operation.</returns>
		public StatusCenterPostItem PostBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation)
		{
			StatusCenterItem banner = new StatusCenterItem(message, title, initialProgress, status, operation);
			StatusCenterPostItem postedBanner = new StatusCenterPostItem(banner, this);

			StatusCenterItems.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		/// <summary>
		/// Posts a banner that represents an operation that can be canceled.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="initialProgress"></param>
		/// <param name="status"></param>
		/// <param name="operation"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <returns></returns>
		public StatusCenterPostItem PostOperationBanner(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource)
		{
			StatusCenterItem banner = new(message, title, initialProgress, status, operation)
			{
				CancellationTokenSource = cancellationTokenSource,
			};

			StatusCenterPostItem postedBanner = new(banner, this, cancellationTokenSource);

			StatusCenterItems.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			UpdateBanner(banner);

			return postedBanner;
		}

		/// <summary>
		/// Dismisses <paramref name="banner"/> and removes it from the collection
		/// </summary>
		/// <param name="banner">The banner to close</param>
		/// <returns>true if operation completed successfully; otherwise false</returns>
		public bool CloseBanner(StatusCenterItem banner)
		{
			if (!StatusCenterItems.Contains(banner))
				return false;

			StatusCenterItems.Remove(banner);

			UpdateBanner(banner);

			return true;
		}

		public void CloseAllBanner()
		{
			StatusCenterItems.ForEach((x) =>
			{
				if (x.IsInProgress)
					CloseBanner(x);
			});

			StatusCenterItems.Clear();
		}

		/// <summary>
		/// Communicates a banner's progress or status has changed
		/// </summary>
		/// <param name="banner"></param>
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
				MedianOperationProgressValue = (int)StatusCenterItems.Where((item) => item.IsInProgress).Average(x => x.ProgressPercentage);
		}
	}
}
