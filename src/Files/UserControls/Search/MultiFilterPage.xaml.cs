using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class MultiFilterPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IMultiSearchPageViewModel), typeof(MultiFilterPage), new PropertyMetadata(null));

        private IMultiSearchPageViewModel ViewModel
        {
            get => (IMultiSearchPageViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public MultiFilterPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as IMultiSearchPageViewModel;
            HeaderCombo.SelectedItem = ViewModel?.Header;
        }

        private void HeaderCombo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // prevent a bug of lost focus in uwp. This bug close the flyout when combobox is open.
            ClearButton.Focus(FocusState.Programmatic);
        }
    }
}
