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
		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

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
			set => SetProperty(ref _ProgressPercentage, value);
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

		public bool IsCancelable
			=> CancellationTokenSource is not null;

		public string HeaderBody { get; set; }

		public ReturnResult FileSystemOperationReturnResult { get; set; }

		public FileOperationType Operation { get; private set; }

		public StatusCenterItemKind ItemKind { get; private set; }

		public StatusCenterItemIconKind ItemIconKind { get; private set; }

		public CancellationTokenSource? CancellationTokenSource { get; set; }

		public ICommand CancelCommand { get; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			SubHeader = message;
			HeaderBody = title;
			Header = title;
			FileSystemOperationReturnResult = status;
			Operation = operation;

			CancelCommand = new RelayCommand(ExecuteCancelCommand);

			switch (FileSystemOperationReturnResult)
			{
				case ReturnResult.InProgress:
					{
						IsInProgress = true;

						if (string.IsNullOrWhiteSpace(HeaderBody))
						{
							HeaderBody = Operation switch
							{
								FileOperationType.Extract => "ExtractInProgress/Title".GetLocalizedResource(),
								FileOperationType.Copy => "CopyInProgress/Title".GetLocalizedResource(),
								FileOperationType.Move => "MoveInProgress".GetLocalizedResource(),
								FileOperationType.Delete => "DeleteInProgress/Title".GetLocalizedResource(),
								FileOperationType.Recycle => "RecycleInProgress/Title".GetLocalizedResource(),
								FileOperationType.Prepare => "PrepareInProgress".GetLocalizedResource(),
								_ => "PrepareInProgress".GetLocalizedResource()
							};
						}

						Header = $"{HeaderBody} ({progress}%)";

						break;
					}
				case ReturnResult.Success:
					{
						IsInProgress = false;

						if (string.IsNullOrWhiteSpace(HeaderBody) || string.IsNullOrWhiteSpace(SubHeader))
						{
							throw new NotImplementedException();
						}
						else
						{
							Header = HeaderBody;
							ItemKind = StatusCenterItemKind.Successful;
						}

						break;
					}
				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
					{
						IsInProgress = false;

						if (string.IsNullOrWhiteSpace(HeaderBody) || string.IsNullOrWhiteSpace(SubHeader))
						{
							throw new NotImplementedException();
						}
						else
						{
							// Expanded banner
							Header = HeaderBody;
							ItemKind = StatusCenterItemKind.Error;
						}

						break;
					}
			}
		}

		public void ExecuteCancelCommand()
		{
			if (IsCancelable)
			{
				CancellationTokenSource?.Cancel();
				IsCancelled = true;
				Header = $"{HeaderBody} ({"canceling".GetLocalizedResource()})";
			}
		}
	}
}
