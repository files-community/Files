using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views
{
    public class ColumnParam : LayoutModeArguments
    {
        public int Column { get; set; }
        public ListView ListView { get; set; }
    }
}