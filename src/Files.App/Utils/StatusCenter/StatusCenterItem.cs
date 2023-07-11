// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents an item for StatusCenter.
	/// </summary>
	public class StatusCenterItem : ObservableObject
	{
		private readonly float _initialProgress = 0.0f;

		private string? _FullTitle;
		public string? FullTitle
		{
			get => _FullTitle;
			set => SetProperty(ref _FullTitle, value ?? string.Empty);
		}

		private string? _Message;
		public string? Message
		{
			// A workaround to avoid overlapping the progress bar (#12362)
			get => _IsProgressing ? _Message + "\n" : _Message;
			private set => SetProperty(ref _Message, value);
		}

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

		private bool _IsCancelled;
		public bool IsCancelled
		{
			get => _IsCancelled;
			set => SetProperty(ref _IsCancelled, value);
		}

		public string Title { get; private set; }

		public InfoBarSeverity Status { get; private set; }

		public ReturnResult ReturnResult { get; set; }

		public CancellationTokenSource? CancellationTokenSource { get; set; }

		public int StatusNumber
			=> (int)Status;

		public string StatusIcon =>
			Status switch
			{
				InfoBarSeverity.Informational => "\uF13F",
				InfoBarSeverity.Success => "\uF13D",
				InfoBarSeverity.Error => "\uF13E",
				_ => "\uF13F"
			};

		public bool CancelButtonVisible
			=> CancellationTokenSource is not null;

		public ICommand CancelCommand { get; }

		public StatusCenterItem(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			Message = message;
			Title = title;
			FullTitle = title;
			ReturnResult = status;
			_initialProgress = progress;

			CancelCommand = new RelayCommand(CancelItem);

			switch (ReturnResult)
			{
				case ReturnResult.InProgress:
					{
						IsProgressing = true;

						if (string.IsNullOrWhiteSpace(Title))
						{
							Title = operation switch
							{
								FileOperationType.Extract => Title = "ExtractInProgress/Title".GetLocalizedResource(),
								FileOperationType.Copy => Title = "CopyInProgress/Title".GetLocalizedResource(),
								FileOperationType.Move => Title = "MoveInProgress".GetLocalizedResource(),
								FileOperationType.Delete => Title = "DeleteInProgress/Title".GetLocalizedResource(),
								FileOperationType.Recycle => Title = "RecycleInProgress/Title".GetLocalizedResource(),
								FileOperationType.Prepare => Title = "PrepareInProgress".GetLocalizedResource(),
								_ => Title = "",
							};
						}

						FullTitle = $"{Title} ({_initialProgress}%)";

						break;
					}
				case ReturnResult.Success:
					{
						IsProgressing = false;

						if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
						{
							throw new NotImplementedException();
						}
						else
						{
							FullTitle = Title;
							Status = InfoBarSeverity.Success;
						}

						break;
					}
				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
					{
						IsProgressing = false;

						if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
						{
							throw new NotImplementedException();
						}
						else
						{
							// Expanded banner
							FullTitle = Title;
							Status = InfoBarSeverity.Error;
						}

						break;
					}
			}
		}

		private void CancelItem()
		{
			if (CancelButtonVisible)
			{
				CancellationTokenSource?.Cancel();
				IsCancelled = true;
				FullTitle = $"{Title} ({"canceling".GetLocalizedResource()})";
			}
		}
	}
}
 