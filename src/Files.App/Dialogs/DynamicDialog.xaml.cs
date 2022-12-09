using Files.App.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

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

		public new Task<ContentDialogResult> ShowAsync() => SetContentDialogRoot(this).ShowAsync().AsTask();

		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			// WinUI3: https://github.com/microsoft/microsoft-ui-xaml/issues/2504
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}

			return contentDialog;
		}

		public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
		{
			this.InitializeComponent();

			dynamicDialogViewModel.HideDialog = this.Hide;
			this.ViewModel = dynamicDialogViewModel;
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
