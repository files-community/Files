using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class RenameDialog : ContentDialog
    {
        public TextBox inputBox;
        public string storedRenameInput;

        public RenameDialog()
        {
            this.InitializeComponent();
            inputBox = RenameInput;
        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            storedRenameInput = inputBox.Text;
        }
    }
}