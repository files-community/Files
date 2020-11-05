using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class ConfirmDeleteDialog : ContentDialog
    {
        public StorageDeleteOption PermanentlyDelete { get; set; }
        public string Description { get; set; }
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel => App.CurrentInstance.ContentPage.SelectedItemsPropertiesViewModel;
        public MyResult Result { get; set; }

        public enum MyResult
        {
            Delete,
            Cancel,
            Nothing
        }

        public ConfirmDeleteDialog(bool deleteFromRecycleBin, StorageDeleteOption deleteOption)
        {
            this.InitializeComponent();

            this.Result = MyResult.Nothing; //clear the result in case the value is set from last time
            this.PermanentlyDelete = deleteOption;

            // If deleting from recycle bin disable "permanently delete" option
            this.chkPermanentlyDelete.IsEnabled = !deleteFromRecycleBin;

            if (SelectedItemsPropertiesViewModel.SelectedItemsCount == 1)
            {
                Description = "ConfirmDeleteDialogDeleteOneItem/Text".GetLocalized();
            }
            else
            {
                Description = string.Format("ConfirmDeleteDialogDeleteMultipleItems/Text".GetLocalized(), SelectedItemsPropertiesViewModel.SelectedItemsCount);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Result = MyResult.Delete;
            Hide();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MyResult.Cancel;
            Hide();
        }
    }
}