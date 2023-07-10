// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.Utils.StatusCenter
{
	public class StatusCenterItem : ObservableObject
	{
		private readonly float _initialProgress = 0.0f;

		private int _Progress;
		public int Progress
		{
			get => _Progress;
			set => SetProperty(ref _Progress, value);
		}

		private bool _IsProgressing;
		public bool IsProgressing
		{
			get => _IsProgressing;
			set
			{
				if (SetProperty(ref _IsProgressing, value))
					OnPropertyChanged(nameof(Message));
			}
		}

		private ReturnResult _Status;
		public ReturnResult Status
		{
			get => _Status;
			set => SetProperty(ref _Status, value);
		}

		private string _Message;
		public string Message
		{
			// A workaround to avoid overlapping the progress bar (#12362)
			get => _IsProgressing ? _Message + "\n" : _Message;
			private set => SetProperty(ref _Message, value);
		}

		private string _FullTitle;
		public string FullTitle
		{
			get => _FullTitle;
			set => SetProperty(ref _FullTitle, value ?? string.Empty);
		}

		private bool _IsCancelled;
		public bool IsCancelled
		{
			get => _IsCancelled;
			set => SetProperty(ref _IsCancelled, value);
		}

		public string Title { get; private set; }

		public FileOperationType Operation { get; private set; }

		public InfoBarSeverity InfoBarSeverity { get; private set; }

		public bool SolutionButtonsVisible { get; }

		public bool CancelButtonVisible
			=> CancellationTokenSource is not null;

		public string PrimaryButtonText { get; set; }

		public string SecondaryButtonText { get; set; } = "Cancel";

		public Action PrimaryButtonClick { get; }

		public ICommand CancelCommand { get; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			Message = message;
			Title = title;
			FullTitle = title;
			_initialProgress = progress;
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

					FullTitle = $"{Title} ({_initialProgress}%)";

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

		public StatusCenterItem(string message, string title, string primaryButtonText, string secondaryButtonText, Action primaryButtonClicked)
		{
			Message = message;
			Title = title;
			PrimaryButtonText = primaryButtonText;
			SecondaryButtonText = secondaryButtonText;
			PrimaryButtonClick = primaryButtonClicked;
			Status = ReturnResult.Failed;

			CancelCommand = new RelayCommand(CancelOperation);

			if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
				throw new NotImplementedException();
			else
			{
				if (!string.IsNullOrWhiteSpace(PrimaryButtonText))
					SolutionButtonsVisible = true;

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
