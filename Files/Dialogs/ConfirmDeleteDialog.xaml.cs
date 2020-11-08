using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class ConfirmDeleteDialog : ContentDialog
    {
        public bool PermanentlyDelete { get; set; }

        public string Description { get; set; }
        private SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; set; } = null;
        public MyResult Result { get; set; }

        public enum MyResult
        {
            Delete,
            Cancel,
            Nothing
        }

        public ConfirmDeleteDialog(bool deleteFromRecycleBin, bool permanently, SelectedItemsPropertiesViewModel propertiesViewModel)
        {
            this.InitializeComponent();

            this.Result = MyResult.Nothing; //clear the result in case the value is set from last time
            this.PermanentlyDelete = permanently;
            this.SelectedItemsPropertiesViewModel = propertiesViewModel;
            // If deleting from recycle bin disable "permanently delete" option
            chkPermanentlyDelete.IsEnabled = !deleteFromRecycleBin;

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