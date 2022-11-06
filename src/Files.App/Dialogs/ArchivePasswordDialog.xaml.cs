using Files.App.ViewModels.Dialogs;
using Files.Backend.SecureStore;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace Files.App.Dialogs
{
	public sealed partial class ArchivePasswordDialog : ContentDialog
	{
		public ArchivePasswordDialogViewModel ViewModel
		{
			get => (ArchivePasswordDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ArchivePasswordDialog()
		{
			this.InitializeComponent();
		
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}
