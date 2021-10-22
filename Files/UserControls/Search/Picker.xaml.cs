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

        /*private void AddFilterButton_Loaded(object sender, RoutedEventArgs e)
        {
            var menu = new MenuFlyout();

            var file = new MenuFlyoutSubItem{ Text = "File" };
            file.Items.Add(new MenuFlyoutSeparator());
            file.Items.Add(GetItem(new CreatedSource()));
            file.Items.Add(GetItem(new ModifiedSource()));
            file.Items.Add(GetItem(new AccessedSource()));
            menu.Items.Add(file);

            var group = new MenuFlyoutSubItem { Text = "Group" };
            group.Items.Add(GetItem(new AndSource()));
            group.Items.Add(GetItem(new OrSource()));
            group.Items.Add(GetItem(new NotSource()));
            menu.Items.Add(group);

            menu.Items.Add(new MenuFlyoutSeparator());

            (sender as Button).Flyout = menu;

            MenuFlyoutItem GetItem(IFilterSource source) => new MenuFlyoutItem
            {
                Tag = source,
                Template = SourceItemTemplate,
                Command = (ViewModel as IGroupPageViewModel).OpenCommand,
                CommandParameter = source.Key,
            };
        }*/
    }

    public class PickerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate DateRangeTemplate { get; set; }
        public DataTemplate SizeRangeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) => item switch
        {
            //IGroupPageViewModel => GroupTemplate,
            IDateRangePickerViewModel => DateRangeTemplate,
            ISizeRangePickerViewModel => SizeRangeTemplate,
            _ => null,
        };

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
