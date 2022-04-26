using Windows.UI.Xaml.Controls;

namespace Files.Views
{
    public class ColumnParam : NavigationArguments
    {
        public int Column { get; set; }
        public ListView ListView { get; set; }
    }
}