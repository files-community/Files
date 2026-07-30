// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class DynamicDialog : ContentDialog, IDisposable
	{
		private DynamicDialogViewModel? viewModel;

		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public DynamicDialogViewModel ViewModel
			=> viewModel ?? throw new ObjectDisposedException(nameof(DynamicDialog));

		public DynamicDialogResult DynamicResult
		{
			get => ViewModel.DynamicResult;
		}

		public new Task<ContentDialogResult> ShowAsync()
		{
			return this.TryShowAsync();
		}

		public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
		{
			InitializeComponent();

			dynamicDialogViewModel.HideDialog = Hide;
			viewModel = dynamicDialogViewModel;
			DataContext = dynamicDialogViewModel;
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonCommand?.Execute(args);
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.SecondaryButtonCommand?.Execute(args);
		}

		private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.CloseButtonCommand?.Execute(args);
		}

		private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			ViewModel.KeyDownCommand?.Execute(e);
		}

		// Focus is moved by the dialog itself while opening, so handlers that focus the
		// display content can only run reliably once the dialog has fully opened
		private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			ViewModel.DisplayControlOnLoadedCommand?.Execute(null);
		}

		public void Dispose()
		{
			viewModel?.Dispose();
			viewModel = null;
			DataContext = null;
		}
	}
}
