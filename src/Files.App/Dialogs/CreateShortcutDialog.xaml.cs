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
		private bool _pathExists = false;

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
			string? destinationName;
			var extension = _pathExists ? ".lnk" : ".url";

			if (_pathExists)
			{
				destinationName = Path.GetFileName(ViewModel.DestinationItemPath);
				destinationName ??= Path.GetDirectoryName(ViewModel.DestinationItemPath);
			}
			else
			{
				var uri = new Uri(ViewModel.DestinationItemPath);
				destinationName = uri.Host;
			}

			var shortcutName = string.Format("ShortcutCreateNewSuffix".ToLocalized(), destinationName);
			var filePath = Path.Combine(
				ViewModel.WorkingDirectory,
				shortcutName + extension);

			int fileNumber = 1;
			while (Path.Exists(filePath))
			{
				filePath = Path.Combine(
					ViewModel.WorkingDirectory,
					shortcutName + $" ({++fileNumber})" + extension);
			}

			await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, ViewModel.DestinationItemPath);
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void DestinationItemPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(DestinationItemPath.Text))
			{
				ViewModel.IsLocationValid = false;
				return;
			}

			try
			{
				_pathExists = Path.Exists(DestinationItemPath.Text) && DestinationItemPath.Text != Path.GetPathRoot(DestinationItemPath.Text);
				if (_pathExists)
				{
					ViewModel.IsLocationValid = true;
				}
				else
				{
					var uri = new Uri(DestinationItemPath.Text);
					ViewModel.IsLocationValid = uri.IsWellFormedOriginalString();
				}
			}
			catch (Exception)
			{
				ViewModel.IsLocationValid = false;
			}
		}
	}
}