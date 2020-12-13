using Files.Filesystem;
using Files.Interacts;
using Windows.System;
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

        private void RenameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
            {
                RenameDialogSymbolsTip.Opacity = 1;
                IsPrimaryButtonEnabled = false;
                return;
            }
            else
            {
                RenameDialogSymbolsTip.Opacity = 0;
                IsPrimaryButtonEnabled = true;
            }

            if (FilesystemHelpers.ContainsRestrictedFileName(textBox.Text))
            {
                IsPrimaryButtonEnabled = false;
            }
            else
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        private void NameDialog_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                Hide();
            }
        }
    }
}