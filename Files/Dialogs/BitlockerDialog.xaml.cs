using System;
using System.Threading.Tasks;
using Windows.Foundation;
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

        public new IAsyncOperation<ContentDialogResult> ShowAsync()
        {
            var tcs = new TaskCompletionSource<ContentDialogResult>();

            this.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key.Equals(VirtualKey.Enter))
                {
                    var element = Windows.UI.Xaml.Input.FocusManager.GetFocusedElement() as Button;
                    if (element == null || element.Name == "PrimaryButton")
                    {
                        if (!IsPrimaryButtonEnabled) return;
                        BitlockerDialog_PrimaryButtonClick(null, null);
                        tcs.TrySetResult(ContentDialogResult.Primary);
                        Hide();
                        e.Handled = true;
                    }
                    else if (element != null && element.Name == "SecondaryButton")
                    {
                        tcs.TrySetResult(ContentDialogResult.Secondary);
                        Hide();
                        e.Handled = true;
                    }
                }
                else if (e.Key.Equals(VirtualKey.Escape))
                {
                    Hide();
                    e.Handled = true;
                }
            };

            var asyncOperation = base.ShowAsync();
            asyncOperation.AsTask().ContinueWith(task => tcs.TrySetResult(task.Result));
            return tcs.Task.AsAsyncOperation();
        }
    }
}