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
        public ICommand SelectAllInvokedCommand { get; set; }
        public ICommand InvertSelectionInvokedCommand { get; set; }
        public ICommand ClearSelectionInvokedCommand { get; set; }

        private DirectoryPropertiesViewModel directoryPropertiesViewModel;

        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel
        {
            get => directoryPropertiesViewModel;
            set
            {
                if (value != directoryPropertiesViewModel)
                {
                    directoryPropertiesViewModel = value;
                    NotifyPropertyChanged(nameof(DirectoryPropertiesViewModel));
                }
            }
        }

        private SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel;

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
        {
            get => selectedItemsPropertiesViewModel;
            set
            {
                if (value != selectedItemsPropertiesViewModel)
                {
                    selectedItemsPropertiesViewModel = value;
                    NotifyPropertyChanged(nameof(SelectedItemsPropertiesViewModel));
                }
            }
        }

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
                StatusCenterTeachingTip.Visibility = Windows.UI.Xaml.Visibility.Visible;
                AppSettings.ShowStatusCenterTeachingTip = false;
            }
            else
            {
                StatusCenterTeachingTip.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                StatusCenterTeachingTip.IsOpen = false;
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
    }
}