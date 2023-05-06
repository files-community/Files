// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Dialogs
{
	public sealed partial class DynamicDialog : ContentDialog, IDisposable
	{
		public DynamicDialogViewModel ViewModel
		{
			get => (DynamicDialogViewModel)DataContext;
			private set => DataContext = value;
		}

		public DynamicDialogResult DynamicResult
		{
			get => ViewModel.DynamicResult;
		}

		public new Task<ContentDialogResult> ShowAsync() => this.TryShowAsync();

		public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
		{
			InitializeComponent();

			dynamicDialogViewModel.HideDialog = Hide;
			ViewModel = dynamicDialogViewModel;
		}

		#region IDisposable

		public void Dispose()
		{
			ViewModel?.Dispose();
			ViewModel = null;
		}

		#endregion IDisposable

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonCommand.Execute(args);
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.SecondaryButtonCommand.Execute(args);
		}

		private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.CloseButtonCommand.Execute(args);
		}

		private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			ViewModel.KeyDownCommand.Execute(e);
		}
	}
}