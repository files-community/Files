using CommunityToolkit.Mvvm.Input;
using Files.Backend.SecureStore;

namespace Files.App.ViewModels.Dialogs
{
	public class ArchivePasswordDialogViewModel
	{
		public DisposableArray? Password { get; private set; }

		public IRelayCommand PrimaryButtonClickCommand { get; }

		public ArchivePasswordDialogViewModel()
		{
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray?>(password => Password = password);
		}
	}
}
