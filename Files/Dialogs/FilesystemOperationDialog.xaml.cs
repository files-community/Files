using Files.ViewModels.Dialogs;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class FilesystemOperationDialog : ContentDialog, IFilesystemOperationDialogView
    {
        public FilesystemOperationDialogViewModel ViewModel
        {
            get => (FilesystemOperationDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public IList<object> SelectedItems => DetailsGrid.SelectedItems;

        public FilesystemOperationDialog(FilesystemOperationDialogViewModel viewModel)
        {
            this.InitializeComponent();

            ViewModel = viewModel;
            ViewModel.View = this;
        }
    }

    public interface IFilesystemOperationDialogView
    {
        IList<object> SelectedItems { get; }
    }
}