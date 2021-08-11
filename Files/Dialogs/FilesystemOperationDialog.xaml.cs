using Files.Enums;
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

        private void MenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var op = (FileNameConflictResolveOptionType)int.Parse((sender as MenuFlyoutItem).Tag as string);
            foreach (var item in DetailsGrid.SelectedItems)
            {
                if(item is FilesystemOperationItemViewModel model)
                {
                    model.TakeAction(op);
                }
            }
        }
    }

    public interface IFilesystemOperationDialogView
    {
        IList<object> SelectedItems { get; }
    }
}