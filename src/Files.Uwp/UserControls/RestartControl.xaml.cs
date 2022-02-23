using System;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class RestartControl : UserControl
    {
        public RestartControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ShowDialogProperty = DependencyProperty.Register(
          "ShowDialog",
          typeof(bool),
          typeof(RestartControl),
          new PropertyMetadata(false, new PropertyChangedCallback(OnShowDialogPropertyChanged))
        );

        public bool ShowDialog
        {
            get
            {
                return (bool)GetValue(ShowDialogProperty);
            }
            set
            {
                SetValue(ShowDialogProperty, value);
            }
        }

        private static void OnShowDialogPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var dialog = (RestartControl)sender;
            if ((bool)e.NewValue)
            {
                dialog.RestartNotification.Show(10000);
            }
            else
            {
                dialog.RestartNotification.Dismiss();
            }
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
            await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
            Process.GetCurrentProcess().Kill();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            RestartNotification.Dismiss();
        }
    }
}