using Files.Filesystem.Search;
using Files.ViewModels.Search;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Files.UserControls.Search
{
    public sealed partial class FilterPicker : UserControl
    {
        /*private readonly INavigator navigator = Navigator.Instance;
        private readonly IFilterViewModelFactory factory = new FilterViewModelFactory();

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IFilterViewModel), typeof(FilterPicker), new PropertyMetadata(null));

        public IFilterViewModel ViewModel
        {
            get => (IFilterViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }*/

        public FilterPicker() => InitializeComponent();

        /*private MenuFlyout GetMenu()
        {
            var menu = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

            var file = new MenuFlyoutSubItem { Text = "File" };
            file.Items.Add(GetMenuItem(new CreatedFilter()));
            file.Items.Add(GetMenuItem(new ModifiedFilter()));
            file.Items.Add(new MenuFlyoutSeparator());
            file.Items.Add(GetMenuItem(new SizeFilter()));
            menu.Items.Add(file);

            var op = new MenuFlyoutSubItem { Text = "Operator" };
            op.Items.Add(GetMenuItem(new AndFilter()));
            op.Items.Add(GetMenuItem(new OrFilter()));
            op.Items.Add(GetMenuItem(new NotFilter()));
            menu.Items.Add(op);

            menu.Items.Add(new MenuFlyoutSeparator());

            var image = new MenuFlyoutSubItem { Text = "Image" };
            image.Items.Add(GetMenuItem("Aspect Ratio"));
            image.Items.Add(GetMenuItem("Resolution"));
            image.Items.Add(GetMenuItem("Format"));
            menu.Items.Add(image);

            var video = new MenuFlyoutSubItem { Text = "Video" };
            video.Items.Add(GetMenuItem("Aspect Ratio"));
            video.Items.Add(GetMenuItem("Resolution"));
            video.Items.Add(GetMenuItem("Format"));
            menu.Items.Add(video);

            return menu;
        }

        private MenuFlyoutItem GetMenuItem(IFilter filter) => new()
        {
            Icon = new FontIcon { FontSize = 14, Glyph = filter.Glyph },
            Text = filter.Label,
            Command = new RelayCommand(() => Select(filter)),
        };
        private static MenuFlyoutItem GetMenuItem(string text) => new() { Text = text };

        private void Select(IFilter filter)
        {
            var viewModel = factory.GetViewModel(filter);
            Select(viewModel);
        }
        private void Select(IFilterViewModel filterViewModel)
        {
            var parentViewModel = ViewModel as IFilterCollectionViewModel;
            filterViewModel = parentViewModel.ItemViewModels.FirstOrDefault(item => item.Filter.Key == filterViewModel.Filter.Key) ?? filterViewModel;

            var viewModel = new FilterPageViewModel
            {
                Parent = parentViewModel,
                Filter = filterViewModel,
            };
            navigator.GoPage(viewModel);
        }

        private void AddFilterButton_Loaded(object sender, RoutedEventArgs e) => (sender as Button).Flyout = GetMenu();
        private void OpenButton_Click(object sender, RoutedEventArgs e) => Select((sender as Button).Content as IFilterViewModel);

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var button = element.FindDescendant("CloseButton") as Button;
                if (button is not null)
                {
                    button.Visibility = Visibility.Visible;
                }
            }
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var button = element.FindDescendant("CloseButton") as Button;
                if (button is not null)
                {
                    button.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var parentViewModel = ViewModel as IFilterCollectionViewModel;
            var viewModel = (sender as Button).DataContext as IFilterViewModel;

            if (parentViewModel.Filter.Contains(viewModel.Filter))
            {
                parentViewModel.Filter.Remove(viewModel.Filter);
            }
        }*/
    }

    /*public class FilterPickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CollectionTemplate { get; set; }
        public DataTemplate OperatorTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }
        public DataTemplate KindTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            IFilterCollectionViewModel => CollectionTemplate,
            IDateRangeFilterViewModel _ => DateRangeTemplate,
            ISizeRangeFilterViewModel _ => SizeRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }

    public class FilterButtonTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            IFilterCollectionViewModel => DefaultTemplate,
            IDateRangeFilterViewModel => DefaultTemplate,
            ISizeRangeFilterViewModel => DefaultTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }

    public class FilterButtonContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CollectionTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }
        public DataTemplate KindTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            IFilterCollectionViewModel => CollectionTemplate,
            IDateRangeFilterViewModel => DateRangeTemplate,
            ISizeRangeFilterViewModel => SizeRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }*/
}
