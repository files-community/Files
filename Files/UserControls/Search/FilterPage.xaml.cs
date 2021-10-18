using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class FilterPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IFilterPageViewModel), typeof(FilterPage), new PropertyMetadata(null));

        public IFilterPageViewModel ViewModel
        {
            get => (IFilterPageViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public FilterPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as IFilterPageViewModel;
            Combo.SelectedItem = ViewModel?.SelectedSource;
        }

        private void Combo_Loaded(object sender, RoutedEventArgs e)
        {
            // prevent a bug of lost focus in uwp. This bug close the flyout when combobox is open.
            CancelButton.Focus(FocusState.Programmatic);
        }
    }

    public class FilterPickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DateRangeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            IDateRangePageViewModel => DateRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
