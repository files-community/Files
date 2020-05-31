using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class ConfirmDeleteDialog : ContentDialog
    {
        public MyResult Result { get; set; }

        public enum MyResult
        {
            Delete,
            Cancel,
            Nothing
        }

        public ConfirmDeleteDialog(bool deleteFromRecycleBin = false)
        {
            this.InitializeComponent();

            this.Result = MyResult.Nothing; //clear the result in case the value is set from last time

            // If deleting from recycle bin disable "permanently delete" option
            this.chkPermanentlyDelete.IsEnabled = !deleteFromRecycleBin;
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