// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.SecureStore;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System.Text;

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
		{
			return (DialogResult)await base.ShowAsync();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(CredentialPasswordBox.Password)));
		}
	}
}