using Microsoft.UI.Xaml.Controls;

namespace Files.UserControls
{
    public class TabItem
    {
        public string Header { get; set; }
        public string Description { get; set; } = null;
        public object Content { get; set; }
        public IconSource IconSource { get; set; }

    }
}