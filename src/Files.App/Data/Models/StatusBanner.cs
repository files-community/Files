using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.Data.Models
{
	public class StatusBanner : ObservableObject
	{
		private readonly float initialProgress = 0.0f;

		private string fullTitle;

		private bool isCancelled;

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
