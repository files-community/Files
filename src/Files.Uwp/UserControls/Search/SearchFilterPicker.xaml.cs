using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Enums;
using Files.Filesystem.Search;
using Files.ViewModels.Search;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Files.UserControls.Search
{
    public sealed partial class SearchFilterPicker : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ISearchFilterViewModel), typeof(SearchFilterPicker), new PropertyMetadata(null));

        public ISearchFilterViewModel ViewModel
        {
            get => (ISearchFilterViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public SearchFilterPicker() => InitializeComponent();

        private void OpenButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var navigator = Ioc.Default.GetService<ISearchNavigator>();
            var filter = (sender as FrameworkElement).DataContext as ISearchFilterViewModel;
            navigator.GoPage(filter);
        }

        private void AddFilterButton_Loaded(object sender, RoutedEventArgs e)
        {
            var provider = Ioc.Default.GetService<ISearchHeaderProvider>();

            var menu = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.BottomEdgeAlignedRight
            };

            var file = new MenuFlyoutSubItem { Text = "File".GetLocalized() };
            file.Items.Add(GetItem(SearchKeys.Size));
            file.Items.Add(new MenuFlyoutSeparator());
            file.Items.Add(GetItem(SearchKeys.DateCreated));
            file.Items.Add(GetItem(SearchKeys.DateModified));
            file.Items.Add(GetItem(SearchKeys.DateAccessed));
            menu.Items.Add(file);

            var group = new MenuFlyoutSubItem { Text = "Group".GetLocalized() };
            group.Items.Add(GetItem(SearchKeys.GroupAnd));
            group.Items.Add(GetItem(SearchKeys.GroupOr));
            group.Items.Add(GetItem(SearchKeys.GroupNot));
            menu.Items.Add(group);

            (sender as Button).Flyout = menu;

            MenuFlyoutItem GetItem(SearchKeys key) => new()
            {
                Tag = new SearchHeaderViewModel(provider.GetHeader(key)),
                Template = HeaderItemTemplate,
            };
        }

        private void AddItemButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var navigator = Ioc.Default.GetService<ISearchNavigator>();
            var header = (sender as Button).Content as ISearchHeaderViewModel;
            var filter = header.CreateFilter();
            navigator.GoPage(filter);
        }
    }

    internal class SearchFilterPickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            ISearchFilterViewModelCollection => GroupTemplate,
            ISizeFilterViewModel => SizeRangeTemplate,
            IDateFilterViewModel => DateRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }

    internal class TagCollectionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTagCollectionTemplate { get; set; }
        public DataTemplate FilterTagCollectionTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            ISearchFilterViewModelCollection => GroupTagCollectionTemplate,
            ISearchFilterViewModel => FilterTagCollectionTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
