using Files.Enums;
using Files.ViewModels.Dialogs;
using System;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
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

        private void ContentDialog_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            ViewModel.KeyDownCommand.Execute(e);
        }
    }
}