using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PropertyListItem : UserControl
    {
        public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register("ColumnWidth", typeof(GridLength), typeof(PropertyListItem), null);

        public GridLength ColumnWidth
        {
            get => (GridLength)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, (GridLength)value);
        }

        public string Text { get; set; }

        public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register("ValueText", typeof(string), typeof(PropertyListItem), null);

        public string ValueText
        {
            get => GetValue(ValueTextProperty) as string;
            set => SetValue(ValueTextProperty, value as string);
        }

        public bool IsReadOnly { get; set; }

        public PropertyListItem()
        {
            this.InitializeComponent();
        }
    }
}