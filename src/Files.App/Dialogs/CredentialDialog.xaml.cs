using Files.Core.SecureStore;
using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;
using System.Threading.Tasks;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
{
	public sealed partial class CredentialDialog : ContentDialog, IDialog<CredentialDialogViewModel>
	{
		public CredentialDialogViewModel ViewModel
		{
			get => (CredentialDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CredentialDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}