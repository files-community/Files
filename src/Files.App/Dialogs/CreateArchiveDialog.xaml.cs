// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Archive;
using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Represents an <see cref="ContentDialog"/> UI for archive creation.
	/// </summary>
	public sealed partial class CreateArchiveDialog : ContentDialog, IDialog<CreateArchiveDialogViewModel>
	{
		public CreateArchiveDialogViewModel ViewModel { get; set; }

		public bool CanCreate { get; set; }

		public string FileName
		{
			get => ViewModel.FileName;
			set => ViewModel.FileName = value;
		}

		public bool UseEncryption
		{
			get => ViewModel.UseEncryption;
			set => ViewModel.UseEncryption = value;
		}

		public string Password
		{
			get => ViewModel.Password;
			set => ViewModel.Password = value;
		}

		public ArchiveFormats FileFormat
		{
			get => ViewModel.FileFormat.Key;
			set => ViewModel.FileFormat = ViewModel.FileFormats.First(format => format.Key == value);
		}

		public ArchiveCompressionLevels CompressionLevel
		{
			get => ViewModel.CompressionLevel.Key;
			set => ViewModel.CompressionLevel = ViewModel.CompressionLevels.First(level => level.Key == value);
		}

		public ArchiveSplittingSizes SplittingSize
		{
			get => ViewModel.SplittingSize.Key;
			set => ViewModel.SplittingSize = ViewModel.SplittingSizes.First(size => size.Key == value);
		}

		public CreateArchiveDialog()
		{
			InitializeComponent();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)SetContentDialogRoot(this).ShowAsync().AsTask();
		}

		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = App.Window.Content.XamlRoot; // WinUi3
			return contentDialog;
		}

		private void ContentDialog_Loaded(object _, RoutedEventArgs e)
		{
			Loaded -= ContentDialog_Loaded;

			FileNameBox.SelectionStart = FileNameBox.Text.Length;
			FileNameBox.Focus(FocusState.Programmatic);
		}

		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

			if (e.Result is ContentDialogResult.Primary)
				CanCreate = true;
		}

		private void PasswordBox_Loading(FrameworkElement _, object e)
			=> PasswordBox.Focus(FocusState.Programmatic);

		private void ViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(CreateArchiveDialogViewModel.UseEncryption) && ViewModel.UseEncryption)
				PasswordBox.Focus(FocusState.Programmatic);
		}
	}
}
