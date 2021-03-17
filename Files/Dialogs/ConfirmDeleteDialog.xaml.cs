using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public enum DialogResult
    {
        Delete,
        Cancel,
        Nothing
    }

    public sealed partial class ConfirmDeleteDialog : ContentDialog
    {
        private SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; set; } = null;

        public bool PermanentlyDelete { get; set; }

        public string Description { get; set; }

        public DialogResult Result { get; set; }

        public ConfirmDeleteDialog(bool deleteFromRecycleBin, bool permanently, SelectedItemsPropertiesViewModel propertiesViewModel)
        {
            this.InitializeComponent();

            Result = DialogResult.Nothing; //clear the result in case the value is set from last time
            PermanentlyDelete = permanently;
            SelectedItemsPropertiesViewModel = propertiesViewModel;

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
            Result = DialogResult.Delete;
            Hide();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResult.Cancel;
            Hide();
        }
    }
}