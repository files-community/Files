using Files.App.Helpers;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog
	{
		public CreateShortcutDialogViewModel ViewModel
		{
			get => (CreateShortcutDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CreateShortcutDialog()
		{
			this.InitializeComponent();
		}

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			var destinationName = Path.GetFileName(ViewModel.DestinationItemPath);
			destinationName ??= Path.GetDirectoryName(ViewModel.DestinationItemPath);
			destinationName ??= Path.GetPathRoot(ViewModel.DestinationItemPath);

			if (string.IsNullOrWhiteSpace(destinationName))
			{
				var uri = new Uri(ViewModel.DestinationItemPath);
				if (!uri.IsFile)
					destinationName = uri.Host;
			}

			var filePath = Path.Combine(
				ViewModel.WorkingDirectory,
				string.Format("ShortcutCreateNewSuffix".ToLocalized(), destinationName) + ".lnk");

			await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, ViewModel.DestinationItemPath);
		}
	}
}