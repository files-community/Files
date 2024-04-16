using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Views
{
    public class ColumnParam : NavigationArguments
    {
        public int Column { get; set; }
        public ListView ListView { get; set; }
    }
}