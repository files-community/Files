// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class CredentialDialogViewModel : ObservableObject
	{
		private string? _UserName;
		public string? UserName
		{
			get => _UserName;
			set => SetProperty(ref _UserName, value);
		}

		private bool _IsAnonymous;
		public bool IsAnonymous
		{
			get => _IsAnonymous;
			set => SetProperty(ref _IsAnonymous, value);
		}

		private bool _PasswordOnly;
		public bool PasswordOnly
		{
			get => _PasswordOnly;
			set => SetProperty(ref _PasswordOnly, value);
		}

		private bool _CanBeAnonymous;
		public bool CanBeAnonymous
		{
			get => _CanBeAnonymous;
			set => SetProperty(ref _CanBeAnonymous, value);
		}

		private bool _IsWrongPassword;
		public bool IsWrongPassword
		{
			get => _IsWrongPassword;
			set => SetProperty(ref _IsWrongPassword, value);
		}

		/// <summary>
		/// When set, the dialog stays open on OK until the entered password passes validation.
		/// </summary>
		public Func<DisposableArray, Task<bool>>? PasswordValidator { get; set; }

		public DisposableArray? Password { get; private set; }

		public IRelayCommand PrimaryButtonClickCommand { get; }

		public CredentialDialogViewModel()
		{
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray?>((password) => Password = password);
		}
	}
}
