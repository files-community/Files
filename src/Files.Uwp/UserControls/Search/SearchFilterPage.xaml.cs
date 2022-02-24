using Files.ViewModels.Search;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class SearchFilterPage : Page
    {
        private static readonly ISearchNavigator navigator = Ioc.Default.GetService<ISearchNavigator>();

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ISearchPageViewModel), typeof(SearchFilterPage), new PropertyMetadata(null));

        public ISearchPageViewModel ViewModel
        {
            get => (ISearchPageViewModel)GetValue(ViewModelProperty);
            set
            {
                if (ViewModel is not null)
                {
                    ViewModel.Filter.PropertyChanged -= Filter_PropertyChanged;
                }
                SetValue(ViewModelProperty, value);
                if (ViewModel is not null)
                {
                    ViewModel.Filter.PropertyChanged += Filter_PropertyChanged;
                }
            }
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISearchFilterViewModel.IsEmpty))
            {
                ViewModel.Save();
            }
        }

        public SearchFilterPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as ISearchPageViewModel;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel = null;
        }

        private void BackButton_Tapped(object sender, TappedRoutedEventArgs e) => navigator.Back();

        private void HeaderCombo_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel as IMultiSearchPageViewModel;
            (sender as ComboBox).SelectedItem = viewModel.Headers.FirstOrDefault(header => header.Key == viewModel.Key);
        }
        private void HeaderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => (ViewModel as IMultiSearchPageViewModel).Key = ((sender as ComboBox).SelectedValue as ISearchHeaderViewModel).Key;
    }

    internal class SearchFilterPageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SettingsPageTemplate { get; set; }
        public DataTemplate SinglePageTemplate { get; set; }
        public DataTemplate MultiPageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            ISearchSettingsPageViewModel => SettingsPageTemplate,
            IMultiSearchPageViewModel => MultiPageTemplate,
            ISearchPageViewModel => SinglePageTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
