// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Utils.StatusCenter
{
	public class StatusCenterItem : ObservableObject
	{
		private readonly float initialProgress = 0.0f;

		public string Title { get; private set; }

		public StatusCenterItemState State { get; private set; } = StatusCenterItemState.InProgress;

		public FileOperationType Operation { get; private set; }

		public ICommand CancelCommand { get; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public bool CancelButtonVisible
			=> CancellationTokenSource is not null;

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
			set
			{
				if (SetProperty(ref isProgressing, value))
					OnPropertyChanged(nameof(Message));
			}
		}

		private ReturnResult status = ReturnResult.InProgress;
		public ReturnResult Status
		{
			get => status;
			set => SetProperty(ref status, value);
		}

		private string message;
		public string Message
		{
			// A workaround to avoid overlapping the progress bar (#12362)
			get => isProgressing ? message + "\n" : message;
			private set => SetProperty(ref message, value);
		}

		private string fullTitle;
		public string FullTitle
		{
			get => fullTitle;
			set => SetProperty(ref fullTitle, value ?? string.Empty);
		}

		private bool isCancelled;
		public bool IsCancelled
		{
			get => isCancelled;
			set => SetProperty(ref isCancelled, value);
		}

		public int StateNumber
			=> (int)State;

		public string StateIcon =>
			State switch
			{
				StatusCenterItemState.InProgress => "\uF143",
				StatusCenterItemState.Success => "\uF13E",
				StatusCenterItemState.Error => "\uF13D",
				_ => "\uF13D"
			};

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			Message = message;
			Title = title;
			FullTitle = title;
			initialProgress = progress;
			Status = status;
			Operation = operation;

			CancelCommand = new RelayCommand(ExecuteCancelCommand);

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
						State = StatusCenterItemState.Success;
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
						State = StatusCenterItemState.Error;
					}

					break;
			}
		}

		public void ExecuteCancelCommand()
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
