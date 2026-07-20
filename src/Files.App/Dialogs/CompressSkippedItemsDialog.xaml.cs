// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class CompressSkippedItemsDialog : ContentDialog, IDialog<CompressSkippedItemsDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public CompressSkippedItemsDialogViewModel ViewModel
		{
			get => (CompressSkippedItemsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CompressSkippedItemsDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await SetContentDialogRoot(this).TryShowAsync();
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}
	}
}