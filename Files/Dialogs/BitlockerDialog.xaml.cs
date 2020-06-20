using Windows.System;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class BitlockerDialog : ContentDialog
    {
        public TextBox inputBox;
        public string storedPasswordInput;

        public BitlockerDialog(string drive)
        {
            this.InitializeComponent();
            inputBox = PasswordInput;
        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            storedPasswordInput = inputBox.Text;
        }

        private void BitlockerInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                IsPrimaryButtonEnabled = false;
                return;
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