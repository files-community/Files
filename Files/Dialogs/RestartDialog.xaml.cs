using System;
using System.Diagnostics;
using Windows.System;
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
            RestartNotification.Show(10000);
        }

        public void Dismiss()
        {
            RestartNotification.Dismiss();
        }

        private async void YesButton_Click(object sender, RoutedEventArgs e)
        {
            App.AppSettings.ResumeAfterRestart = true;
            App.SaveSessionTabs();
            await Launcher.LaunchUriAsync(new Uri("files-uwp://home/page="));
            Process.GetCurrentProcess().Kill();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            RestartNotification.Dismiss();
        }
    }
}