// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents an item for Status Center operation tasks.
	/// </summary>
	public sealed class StatusCenterItem : ObservableObject
	{
		private readonly float _initialProgress = 0.0f;

		private int _ProgressPercentage = 0;
		public int ProgressPercentage
		{
			get => _ProgressPercentage;
			set => SetProperty(ref _ProgressPercentage, value);
		}

		private bool _IsInProgress = false;
		public bool IsInProgress
		{
			get => _IsInProgress;
			set
			{
				if (SetProperty(ref _IsInProgress, value))
					OnPropertyChanged(nameof(Message));
			}
		}

		private ReturnResult _FileSystemOperationReturnResult = ReturnResult.InProgress;
		public ReturnResult FileSystemOperationReturnResult
		{
			get => _FileSystemOperationReturnResult;
			set => SetProperty(ref _FileSystemOperationReturnResult, value);
		}

		private string? _Message;
		public string? Message
		{
			// A workaround to avoid overlapping the progress bar (#12362)
			get => _IsInProgress ? _Message + "\n" : _Message;
			private set => SetProperty(ref _Message, value);
		}

		private string? _FullTitle;
		public string? FullTitle
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

		public int ItemStateInteger
			=> (int)ItemState;

		public bool IsCancelButtonVisible
			=> CancellationTokenSource is not null;

		public string StateIcon =>
			ItemState switch
			{
				StatusCenterItemState.InProgress => "\uE895",
				StatusCenterItemState.Success => "\uE73E",
				StatusCenterItemState.Error => "\uE894",
				_ => "\uE895"
			};

		public string Title { get; private set; }

		public StatusCenterItemState ItemState { get; private set; }

		public FileOperationType Operation { get; private set; }

		public CancellationTokenSource? CancellationTokenSource { get; set; }

		public ICommand CancelCommand { get; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			_initialProgress = progress;
			Message = message;
			Title = title;
			FullTitle = title;
			FileSystemOperationReturnResult = status;
			Operation = operation;

			CancelCommand = new RelayCommand(ExecuteCancelCommand);

			switch (FileSystemOperationReturnResult)
			{
				case ReturnResult.InProgress:
					IsInProgress = true;
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
					IsInProgress = false;
					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
					{
						throw new NotImplementedException();
					}
					else
					{
						FullTitle = Title;
						ItemState = StatusCenterItemState.Success;
					}
					break;

				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
					IsInProgress = false;
					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
					{
						throw new NotImplementedException();
					}
					else
					{
						// Expanded banner
						FullTitle = Title;
						ItemState = StatusCenterItemState.Error;
					}

					break;
			}
		}

		public void ExecuteCancelCommand()
		{
			if (IsCancelButtonVisible)
			{
				CancellationTokenSource?.Cancel();
				IsCancelled = true;
				FullTitle = $"{Title} ({"canceling".GetLocalizedResource()})";
			}
		}
	}
}
