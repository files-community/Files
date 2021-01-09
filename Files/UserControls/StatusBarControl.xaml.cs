using Files.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public FolderSettingsViewModel FolderSettings { get; set; } = null;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; set; } = null;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; set; } = null;
        public ICommand SelectAllInvokedCommand { get; set; }
        public ICommand InvertSelectionInvokedCommand { get; set; }
        public ICommand ClearSelectionInvokedCommand { get; set; }

        public StatusBarControl()
        {
            this.InitializeComponent();
            OngoingTasksControl.ProgressBannerPosted += OngoingTasksControl_ProgressBannerPosted;
        }

        private void OngoingTasksControl_ProgressBannerPosted(object sender, EventArgs e)
        {
            if (AppSettings.ShowStatusCenterTeachingTip)
            {
                StatusCenterTeachingTip.IsOpen = true;
                AppSettings.ShowStatusCenterTeachingTip = false;
            }

            PlayBannerAddedVisualAnimation();
        }

        public async void PlayBannerAddedVisualAnimation()
        {
            StatusCenterPulseVisualPlayer.Visibility = Windows.UI.Xaml.Visibility.Visible;
            await StatusCenterPulseVisualPlayer.PlayAsync(0, 1, false);
            await StatusCenterPulseVisualPlayer.PlayAsync(0, 1, false);
            StatusCenterPulseVisualPlayer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool showStatusCenter;

        public bool ShowStatusCenter
        {
            get => showStatusCenter;
            set
            {
                if (value != showStatusCenter)
                {
                    showStatusCenter = value;
                    NotifyPropertyChanged(nameof(ShowStatusCenter));
                }
            }
        }

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AppSettings.PreviewPaneEnabled = !AppSettings.PreviewPaneEnabled;
        }
    }
}