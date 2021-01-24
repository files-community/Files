using Microsoft.UI.Xaml.Controls;

namespace Files.UserControls.MultitaskingControl
{
    public interface ITabItemControl
    {
        string Header { get; }

        string Description { get; }

        IconSource IconSource { get; }

        TabItemControl Control { get; }

        bool AllowStorageItemDrop { get; }
    }
}