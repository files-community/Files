using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

            var file = new MenuFlyoutSubItem{ Text = "File" };
            file.Items.Add(GetItem(new SizeRangeHeader()));
            file.Items.Add(new MenuFlyoutSeparator());
            file.Items.Add(GetItem(new CreatedHeader()));
            file.Items.Add(GetItem(new ModifiedHeader()));
            file.Items.Add(GetItem(new AccessedHeader()));
            menu.Items.Add(file);

            var group = new MenuFlyoutSubItem { Text = "Group" };
            group.Items.Add(GetItem(new AndHeader()));
            group.Items.Add(GetItem(new OrHeader()));
            group.Items.Add(GetItem(new NotHeader()));
            menu.Items.Add(group);

            menu.Items.Add(new MenuFlyoutSeparator());

            menu.Items.Add(new MenuFlyoutSubItem { Text = "Document" });
            menu.Items.Add(new MenuFlyoutSubItem { Text = "Image" });
            menu.Items.Add(new MenuFlyoutSubItem { Text = "Music" });
            menu.Items.Add(new MenuFlyoutSubItem { Text = "Video" });

            (sender as Button).Flyout = menu;

            MenuFlyoutItem GetItem(IFilterHeader header) => new MenuFlyoutItem
            {
                Tag = header,
                Template = HeaderItemTemplate,
                Command = (ViewModel as IGroupPickerViewModel).OpenCommand,
                CommandParameter = header,
            };
        }
    }

    public class PickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            IGroupPickerViewModel => GroupTemplate,
            IDateRangePickerViewModel => DateRangeTemplate,
            ISizeRangePickerViewModel => SizeRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
