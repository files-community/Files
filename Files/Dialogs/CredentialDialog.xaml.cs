using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Files.Dialogs
{
    public sealed partial class CredentialDialog : ContentDialog
    {
        private readonly TaskCompletionSource<(string UserName, string Password, bool Anonymous)> _taskCompletionSource;
        public Task<(string UserName, string Password, bool Anonymous)> Result { get; }

        public CredentialDialog()
        {
            _taskCompletionSource = new();
            Result = _taskCompletionSource.Task;
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _taskCompletionSource.SetResult((UserName.Text, Password.Password, Anonymous.IsChecked ?? false));
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _taskCompletionSource.SetResult((null, null, true));
        }

        private void AskCredentialDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (args.Result == ContentDialogResult.None)
            {
                _taskCompletionSource.SetResult((null, null, true));
            }
        }
    }
}
