// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
{
	public sealed partial class GitHubLoginDialog : ContentDialog, IDialog<GitHubLoginDialogViewModel>
	{
		public GitHubLoginDialogViewModel ViewModel
		{
			get => (GitHubLoginDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public GitHubLoginDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			args.Cancel = true;

			var data = new DataPackage();
			data.SetText(ViewModel.UserCode);

			Clipboard.SetContent(data);
			Clipboard.Flush();
		}
	}
}
