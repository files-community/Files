using Files.Interacts;
using Files.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl, INotifyPropertyChanged
    {

        public SettingsViewModel AppSettings => App.AppSettings;

        public MainViewModel MainViewModel => App.MainViewModel;

        public StatusCenterViewModel StatusCenterViewModel { get; set; }

        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel
        {
            get => (DirectoryPropertiesViewModel)GetValue(DirectoryPropertiesViewModelProperty);
            set => SetValue(DirectoryPropertiesViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for DirectoryPropertiesViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
            DependencyProperty.Register(nameof(DirectoryPropertiesViewModel), typeof(DirectoryPropertiesViewModel), typeof(StatusBarControl), new PropertyMetadata(null));


        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
        {
            get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
            set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
            DependencyProperty.Register(nameof(SelectedItemsPropertiesViewModel), typeof(SelectedItemsPropertiesViewModel), typeof(StatusBarControl), new PropertyMetadata(null));



        public bool ShowInfoText
        {
            get => (bool)GetValue(ShowInfoTextProperty);
            set => SetValue(ShowInfoTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for HideInfoText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowInfoTextProperty =
            DependencyProperty.Register(nameof(ShowInfoText), typeof(bool), typeof(StatusBarControl), new PropertyMetadata(null));




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

        public StatusBarControl()
        {
            this.InitializeComponent();
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UserControl_Loading(Windows.UI.Xaml.FrameworkElement sender, object args)
        {
            StatusCenterViewModel.ProgressBannerPosted += StatusCenterActions_ProgressBannerPosted;
        }
    }
}