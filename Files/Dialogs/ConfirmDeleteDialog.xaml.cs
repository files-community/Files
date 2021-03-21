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
        public bool PermanentlyDelete { get; set; }

        public string Description { get; set; }

        public DialogResult Result { get; set; }

        public ConfirmDeleteDialog(bool deleteFromRecycleBin, bool permanently, int itemsSelected)
        {
            this.InitializeComponent();

            Result = DialogResult.Nothing; //clear the result in case the value is set from last time
            PermanentlyDelete = permanently;

            // If deleting from recycle bin disable "permanently delete" option
            chkPermanentlyDelete.IsEnabled = !deleteFromRecycleBin;

            if (itemsSelected == 1)
            {
                Description = "ConfirmDeleteDialogDeleteOneItem/Text".GetLocalized();
            }
            else
            {
                Description = string.Format("ConfirmDeleteDialogDeleteMultipleItems/Text".GetLocalized(), itemsSelected);
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