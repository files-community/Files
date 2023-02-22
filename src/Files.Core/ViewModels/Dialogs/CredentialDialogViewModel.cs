using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Core.SecureStore;

namespace Files.Core.ViewModels.Dialogs
{
	public sealed class CredentialDialogViewModel : ObservableObject
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

		public DisposableArray? Password { get; private set; }

		public IRelayCommand PrimaryButtonClickCommand { get; }

		public CredentialDialogViewModel()
		{
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray?>((password) => Password = password);
		}
	}
}