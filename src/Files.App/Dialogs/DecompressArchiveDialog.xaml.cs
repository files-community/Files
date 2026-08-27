// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using WinRT;

namespace Files.App.Dialogs
{
	public sealed partial class DecompressArchiveDialog : ContentDialog
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public DecompressArchiveDialogViewModel? ViewModel
		{
			get => DataContext as DecompressArchiveDialogViewModel;
			set => DataContext = value;
		}

		public DecompressArchiveDialog()
		{
			InitializeComponent();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (ViewModel is { IsArchiveEncrypted: true })
				ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}

		private void DestinationFolderPath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				ViewModel?.UpdateSuggestions(sender.Text);
			}
		}
	}
}
