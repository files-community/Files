using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class RestartDialog : UserControl
    {
        public RestartDialog()
        {
            this.InitializeComponent();
        }

        public void Show()
        {
            RestartNotification.Show();
        }

        public void Dismiss()
        {
            RestartNotification.Dismiss();
        }

        private async void YesButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Restart app");
            AppRestartFailureReason failureReason = await CoreApplication.RequestRestartAsync("");
            if (failureReason == AppRestartFailureReason.NotInForeground)
            {
                System.Diagnostics.Debug.WriteLine("App not in foreground");
            }
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            RestartNotification.Dismiss();
        }
    }
}
