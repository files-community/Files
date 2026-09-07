// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Dialogs
{
	public sealed partial class DynamicDialog : ContentDialog, IDisposable
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public DynamicDialogViewModel ViewModel
			=> DataContext as DynamicDialogViewModel ?? throw new ObjectDisposedException(nameof(DynamicDialog));

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
			DataContext = dynamicDialogViewModel;
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			(ViewModel.PrimaryButtonCommand
				?? throw new InvalidOperationException("The primary button command has not been initialized.")).Execute(args);
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			(ViewModel.SecondaryButtonCommand
				?? throw new InvalidOperationException("The secondary button command has not been initialized.")).Execute(args);
		}

		private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			(ViewModel.CloseButtonCommand
				?? throw new InvalidOperationException("The close button command has not been initialized.")).Execute(args);
		}

		private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			(ViewModel.KeyDownCommand
				?? throw new InvalidOperationException("The key-down command has not been initialized.")).Execute(e);
		}

		// The dialog moves focus to its default button after Opened is raised, so handlers
		// that focus the display content must be dispatched to run after that
		private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			DispatcherQueue.TryEnqueue(() =>
				(DataContext as DynamicDialogViewModel)?.DisplayControlOnLoadedCommand?.Execute(null));
		}

		public void Dispose()
		{
			(DataContext as DynamicDialogViewModel)?.Dispose();
			DataContext = null;
		}
	}
}
