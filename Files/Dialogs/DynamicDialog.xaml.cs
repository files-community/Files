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
    }
}