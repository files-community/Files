// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model class that post an error message banner following a failed operation
	/// </summary>
	public class StatusBanner : ObservableObject
	{
		private readonly float _initialProgress = 0.0f;

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

		private int _Progress = 0;
		public int Progress
		{
			get => _Progress;
			set => SetProperty(ref _Progress, value);
		}

		private bool _IsProgressing;
		public bool IsProgressing
		{
			get => _IsProgressing;
			set => SetProperty(ref _IsProgressing, value);
		}

		private ReturnResult _Status;
		public ReturnResult Status
		{
			get => _Status;
			set => SetProperty(ref _Status, value);
		}

		public string Title { get; private set; }

		public FileOperationType Operation { get; private set; }

		public string Message { get; private set; }

		public InfoBarSeverity InfoBarSeverity { get; private set; }

		public string? PrimaryButtonText { get; set; }

		public string SecondaryButtonText { get; set; } = "Cancel".GetLocalizedResource();

		public Action? PrimaryButtonClick { get; }

		public ICommand CancelCommand { get; }

		public bool SolutionButtonsVisible { get; } = false;

		public bool CancelButtonVisible
			=> CancellationTokenSource is not null;

		public CancellationTokenSource? CancellationTokenSource { get; set; }

		public StatusBanner(string message, string title, float progress, ReturnResult status, FileOperationType operation)
		{
			_initialProgress = progress;
			Message = message;
			Title = title;
			FullTitle = title;
			Status = status;
			Operation = operation;

			CancelCommand = new RelayCommand(CancelOperation);

			switch (Status)
			{
				case ReturnResult.InProgress:
				{
					IsProgressing = true;

					if (string.IsNullOrWhiteSpace(Title))
					{
						Title = Operation switch
						{
							FileOperationType.Extract => Title = "ExtractInProgress/Title".GetLocalizedResource(),
							FileOperationType.Copy    => Title = "CopyInProgress/Title".GetLocalizedResource(),
							FileOperationType.Move    => Title = "MoveInProgress".GetLocalizedResource(),
							FileOperationType.Delete  => Title = "DeleteInProgress/Title".GetLocalizedResource(),
							FileOperationType.Recycle => Title = "RecycleInProgress/Title".GetLocalizedResource(),
							FileOperationType.Prepare => Title = "PrepareInProgress".GetLocalizedResource(),
						};
					}

					FullTitle = $"{Title} ({_initialProgress}%)";

					break;
				}
				case ReturnResult.Success:
				{
					IsProgressing = false;

					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
						throw new NotImplementedException();

					InfoBarSeverity = InfoBarSeverity.Success;

					break;
				}
				case ReturnResult.Failed:
				case ReturnResult.Cancelled:
				{
					IsProgressing = false;

					if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
						throw new NotImplementedException();

					InfoBarSeverity = InfoBarSeverity.Error;

					break;
				}
			}
		}

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
				throw new NotImplementedException();

			if (!string.IsNullOrWhiteSpace(PrimaryButtonText))
				SolutionButtonsVisible = true;

			FullTitle = Title;
			InfoBarSeverity = InfoBarSeverity.Error;
		}

		public void CancelOperation()
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
