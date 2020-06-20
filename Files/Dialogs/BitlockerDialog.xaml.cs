using Windows.System;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class BitlockerDialog : ContentDialog
    {
        public PasswordBox inputBox;
        public string storedPasswordInput;

        public BitlockerDialog(string drive)
        {
            this.InitializeComponent();
            inputBox = PasswordInput;
            IsPrimaryButtonEnabled = false;
        }

        private void BitlockerDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            storedPasswordInput = inputBox.Password;
        }

        private void BitlockerInput_TextChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var textBox = sender as PasswordBox;

            if (string.IsNullOrEmpty(textBox.Password))
            {
                IsPrimaryButtonEnabled = false;
            }
            else
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        private void BitlockerDialog_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                Hide();
            }
        }
    }
}