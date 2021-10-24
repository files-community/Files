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

        public StatusBarControl()
        {
            this.InitializeComponent();
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
    }
}