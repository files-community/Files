using Files.Backend.SecureStore;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class CredentialDialog : ContentDialog, IDialog<CredentialDialogViewModel>
	{
		public CredentialDialogViewModel ViewModel { get; set; }

		public CredentialDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}
