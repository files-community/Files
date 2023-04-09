using Files.App.Helpers;
using Files.App.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class DynamicDialog : ContentDialog, IDisposable
	{
		public DynamicDialogViewModel ViewModel { get;  set; }

		public DynamicDialogResult DynamicResult
			=> ViewModel.DynamicResult;

		public new Task<ContentDialogResult> ShowAsync()
    	=> this.TryShowAsync();

		public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
		{
			InitializeComponent();

			dynamicDialogViewModel.HideDialog = Hide;
			ViewModel = dynamicDialogViewModel;
		}

		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			// WINUI3
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;

			return contentDialog;
		}

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

		public void Dispose()
		{
			ViewModel?.Dispose();
			ViewModel = null;
		}
	}
}
