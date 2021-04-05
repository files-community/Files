using Files.Interacts;
using Files.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl, INotifyPropertyChanged
    {
        #region Singleton

        public SettingsViewModel AppSettings => App.AppSettings;

        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;

        #endregion Singleton

        #region Private Members

        private IStatusCenterActions statusCenterActions => OngoingTasksControl;

        #endregion Private Members

        #region Public Properties

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

        #endregion Public Properties

        #region Constructor

        public StatusBarControl()
        {
            this.InitializeComponent();
            statusCenterActions.ProgressBannerPosted += StatusCenterActions_ProgressBannerPosted;
        }

        #endregion Constructor

        #region Event Handlers

        private void StatusCenterActions_ProgressBannerPosted(object sender, PostedStatusBanner e)
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
        }

        private void FullTrustStatus_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            FullTrustStatusTeachingTip.IsOpen = true;
        }

        #endregion Event Handlers

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}