// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
{
	public sealed partial class AddBranchDialog : ContentDialog, IDialog<AddBranchDialogViewModel>
	{
		public AddBranchDialogViewModel ViewModel
		{
			get => (AddBranchDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public AddBranchDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
		}
	}
}
