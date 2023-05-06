// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
{
	public sealed partial class FilesystemOperationDialog : ContentDialog, IDialog<FileSystemDialogViewModel>
	{
		public FileSystemDialogViewModel ViewModel
		{
			get => (FileSystemDialogViewModel)DataContext;
			set
			{
				if (value is not null)
				{
					value.PrimaryButtonEnabled = true;
				}

				DataContext = value;
			}
		}

		public FilesystemOperationDialog()
		{
			InitializeComponent();

			App.Window.SizeChanged += Current_SizeChanged;
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await SetContentDialogRoot(this).ShowAsync();

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			if (ViewModel.FileSystemDialogMode.ConflictsExist)
				ContainerGrid.Width = App.Window.Bounds.Width <= 700 ? App.Window.Bounds.Width - 50 : 650;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var primaryButton = this.FindDescendant("PrimaryButton") as Button;
			if (primaryButton is not null)
			{
				primaryButton.GotFocus += PrimaryButton_GotFocus;
			}
		}

		private void PrimaryButton_GotFocus(object sender, RoutedEventArgs e)
		{
			(sender as Button).GotFocus -= PrimaryButton_GotFocus;
			if (chkPermanentlyDelete is not null)
			{
				chkPermanentlyDelete.IsEnabled = ViewModel.IsDeletePermanentlyEnabled;
			}
			DetailsGrid.IsEnabled = true;
		}

		private void RootDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			if (args.Result == ContentDialogResult.Primary)
				ViewModel.SaveConflictResolveOption();

			App.Window.SizeChanged -= Current_SizeChanged;
			ViewModel.CancelCts();
		}

		private void NameStackPanel_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element
				&& element.DataContext is FileSystemDialogConflictItemViewModel conflictItem
				&& conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName)
			{
				conflictItem.IsTextBoxVisible = conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName;
				conflictItem.CustomName = conflictItem.DestinationDisplayName;
			}
		}

		private void NameEdit_LostFocus(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is FileSystemDialogConflictItemViewModel conflictItem)
			{
				conflictItem.CustomName = FilesystemHelpers.FilterRestrictedCharacters(conflictItem.CustomName);

				if (ViewModel.IsNameAvailableForItem(conflictItem, conflictItem.CustomName!))
				{
					conflictItem.IsTextBoxVisible = false;
				}
				else
				{
					ViewModel.PrimaryButtonEnabled = false;
				}

				if (conflictItem.CustomName.Equals(conflictItem.DisplayName))
				{
					var savedName = conflictItem.DestinationDisplayName;
					conflictItem.CustomName = string.Empty;
					conflictItem.DestinationDisplayName = savedName;
				}
			}
		}

		private void NameEdit_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as TextBox)?.Focus(FocusState.Programmatic);
		}

		private void ConflictOptions_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is ComboBox comboBox)
				comboBox.SelectedIndex = ViewModel.LoadConflictResolveOption() switch
				{
					FileNameConflictResolveOptionType.None => -1,
					FileNameConflictResolveOptionType.GenerateNewName => 0,
					FileNameConflictResolveOptionType.ReplaceExisting => 1,
					FileNameConflictResolveOptionType.Skip => 2,
					_ => -1
				};
		}

		private void FilesystemOperationDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			if (ViewModel.FileSystemDialogMode.IsInDeleteMode)
			{
				DescriptionText.Foreground = App.Current.Resources["TextControlForeground"] as SolidColorBrush;
			}

			UpdateDialogLayout();
		}
	}
}