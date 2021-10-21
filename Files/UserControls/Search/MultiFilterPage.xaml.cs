using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class MultiFilterPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IMultiFilterPageViewModel), typeof(MultiFilterPage), new PropertyMetadata(null));

        private IMultiFilterPageViewModel ViewModel
        {
            get => (IMultiFilterPageViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public MultiFilterPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as IMultiFilterPageViewModel;
            HeaderCombo.SelectedItem = ViewModel?.Header;
        }

        private void Combo_Loaded(object sender, RoutedEventArgs e)
        {
            // prevent a bug of lost focus in uwp. This bug close the flyout when combobox is open.
            CancelButton.Focus(FocusState.Programmatic);
        }
    }
}
