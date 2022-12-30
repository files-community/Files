using Files.App.Helpers;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Extensions;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>
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
			if (string.IsNullOrWhiteSpace(ViewModel.DestinationItemPath))
			{
				ViewModel.IsLocationValid = false;
				args.Cancel = true;
				return;
			}

			try
			{
				var destinationName = string.Empty;
				var extension = ".lnk";
				if (Path.Exists(ViewModel.DestinationItemPath))
				{
					destinationName = Path.GetFileName(ViewModel.DestinationItemPath);
					destinationName ??= Path.GetDirectoryName(ViewModel.DestinationItemPath);
					if (string.IsNullOrEmpty(destinationName))
					{
						ViewModel.IsLocationValid = false;
						args.Cancel = true;
						return;
					}
				}
				else
				{
					var uri = new Uri(ViewModel.DestinationItemPath);
					if (!uri.IsWellFormedOriginalString())
					{
						ViewModel.IsLocationValid = false;
						args.Cancel = true;
						return;
					}
					destinationName = uri.Host;
					extension = ".url";
				}

				var filePath = Path.Combine(
					ViewModel.WorkingDirectory,
					string.Format("ShortcutCreateNewSuffix".ToLocalized(), destinationName) + extension);

				await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, ViewModel.DestinationItemPath);
			}
			catch (Exception)
			{
				ViewModel.IsLocationValid = false;
				args.Cancel = true;
			}
		}

		private void DestinationItemPath_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
			=> ViewModel.IsLocationValid = true;

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}