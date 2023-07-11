// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.UserControls
{
	public class StatusCenterViewModel : ObservableObject
	{
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

				foreach (var item in StatusCenterItems)
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
			=> StatusCenterItems.Any();

		public int InfoBadgeState
		{
			get
			{
				var anyFailure = StatusCenterItems.Any(i => i.ReturnResult != ReturnResult.InProgress && i.ReturnResult != ReturnResult.Success);

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

		public ObservableCollection<StatusCenterItem> StatusCenterItems { get; private set; } = new();

		public event EventHandler<StatusCenterPostedItem>? ProgressBannerPosted;

		public ICommand CloseItemCommand { get; }

		public ICommand CloseAllCommand { get; }

		public StatusCenterViewModel()
		{
			StatusCenterItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AnyBannersPresent));

			CloseItemCommand = new RelayCommand<StatusCenterItem>(RemoveItem);
			CloseAllCommand = new RelayCommand(RemoveAll);
		}

		public StatusCenterPostedItem AddItem(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation)
		{
			StatusCenterItem banner = new(message, title, initialProgress, status, operation);
			StatusCenterPostedItem postedBanner = new(banner);

			StatusCenterItems.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			NotifyPropertyChanges();

			return postedBanner;
		}

		public StatusCenterPostedItem AddCancellableItem(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource)
		{
			StatusCenterItem banner = new(message, title, initialProgress, status, operation)
			{
				CancellationTokenSource = cancellationTokenSource,
			};

			StatusCenterPostedItem postedBanner = new(banner, cancellationTokenSource);

			StatusCenterItems.Insert(0, banner);
			ProgressBannerPosted?.Invoke(this, postedBanner);

			NotifyPropertyChanges();

			return postedBanner;
		}

		public void RemoveItem(StatusCenterItem item)
		{
			if (!StatusCenterItems.Remove(item))
				return;

			NotifyPropertyChanges();
		}

		public void RemoveAll()
		{
			for (int index = StatusCenterItems.Count - 1; index >= 0; index--)
			{
				var itemToDismiss = StatusCenterItems[index];
				if (!itemToDismiss.IsProgressing)
					RemoveItem(itemToDismiss);
			}
		}

		public void NotifyPropertyChanges()
		{
			OnPropertyChanged(nameof(OngoingOperationsCount));
			OnPropertyChanged(nameof(AnyOperationsOngoing));
			OnPropertyChanged(nameof(InfoBadgeState));
			OnPropertyChanged(nameof(InfoBadgeValue));
		}

		public void UpdateMedianProgress()
		{
			if (AnyOperationsOngoing)
				MedianOperationProgressValue = (int)StatusCenterItems.Where((item) => item.IsProgressing).Average(x => x.Progress);
		}
	}
}
