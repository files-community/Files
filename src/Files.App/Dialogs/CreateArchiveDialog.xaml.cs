// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Represents an <see cref="ContentDialog"/> UI for archive creation.
	/// </summary>
	public sealed partial class CreateArchiveDialog : ContentDialog, IDialog<CreateArchiveDialogViewModel>
	{
		public CreateArchiveDialogViewModel ViewModel { get; set; }

		public CreateArchiveDialog()
		{
			InitializeComponent();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
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
				ViewModel.CanCreate = true;
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
