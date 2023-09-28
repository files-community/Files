// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class ReleaseNotesDialog : ContentDialog, IDialog<ReleaseNotesDialogViewModel>
	{
		public ReleaseNotesDialogViewModel ViewModel
		{
			get => (ReleaseNotesDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ReleaseNotesDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await SetContentDialogRoot(this).TryShowAsync();
		}

		private void CloseDialogButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}
	}
}
