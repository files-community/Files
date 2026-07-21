// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace Files.App.Dialogs
{
	public sealed partial class CredentialDialog : ContentDialog, IDialog<CredentialDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public CredentialDialogViewModel ViewModel
		{
			get => (CredentialDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CredentialDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}

		private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			// Focus the first editable input so the user can type without clicking first
			var target = UserName is { IsLoaded: true, IsEnabled: true } ? UserName : (Control)Password;
			target.Focus(FocusState.Programmatic);
		}

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			var password = new DisposableArray(Encoding.UTF8.GetBytes(Password.Password));

			if (ViewModel.PasswordValidator is null)
			{
				ViewModel.PrimaryButtonClickCommand.Execute(password);
				return;
			}

			var deferral = args.GetDeferral();
			try
			{
				IsPrimaryButtonEnabled = false;
				if (await ViewModel.PasswordValidator(password))
				{
					ViewModel.PrimaryButtonClickCommand.Execute(password);
				}
				else
				{
					args.Cancel = true;
					ViewModel.IsWrongPassword = true;
					password.Dispose();
				}
			}
			finally
			{
				IsPrimaryButtonEnabled = true;
				deferral.Complete();
			}
		}
	}
}
