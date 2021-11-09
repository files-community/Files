using Files.ViewModels.Search;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Files.UserControls.Search
{
    public sealed partial class Picker : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IPickerViewModel), typeof(Picker), new PropertyMetadata(null));

        public IPickerViewModel ViewModel
        {
            get => (IPickerViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public Picker() => InitializeComponent();

        private void AddFilterButton_Loaded(object sender, RoutedEventArgs e)
        {
            var menu = new MenuFlyout();
            menu.Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;

            var file = new MenuFlyoutSubItem { Text = "SearchPickerAddMenu_File".GetLocalized() };
            file.Items.Add(GetItem(new SizeRangeHeader()));
            file.Items.Add(new MenuFlyoutSeparator());
            file.Items.Add(GetItem(new CreatedHeader()));
            file.Items.Add(GetItem(new ModifiedHeader()));
            file.Items.Add(GetItem(new AccessedHeader()));
            menu.Items.Add(file);

            var group = new MenuFlyoutSubItem { Text = "SearchPickerAddMenu_Group".GetLocalized() };
            group.Items.Add(GetItem(new AndHeader()));
            group.Items.Add(GetItem(new OrHeader()));
            group.Items.Add(GetItem(new NotHeader()));
            menu.Items.Add(group);

            (sender as Button).Flyout = menu;

            MenuFlyoutItem GetItem(ISearchFilterHeader header) => new MenuFlyoutItem
            {
                Tag = header,
                Template = HeaderItemTemplate,
                Command = (ViewModel as IGroupPickerViewModel).OpenCommand,
                CommandParameter = header,
            };
        }

        private void ContextItem_PointerEntered(object sender, PointerRoutedEventArgs e)
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
        private void ContextItem_PointerExited(object sender, PointerRoutedEventArgs e)
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
    }

    public class PickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LocationTemplate { get; set; }
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            ILocationPickerViewModel => LocationTemplate,
            IGroupPickerViewModel => GroupTemplate,
            IDateRangePickerViewModel => DateRangeTemplate,
            ISizeRangePickerViewModel => SizeRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
