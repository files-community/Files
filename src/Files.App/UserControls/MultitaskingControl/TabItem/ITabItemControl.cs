using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.MultitaskingControl
{
    public interface ITabItemControl
    {
        string Header { get; }

        IconSource IconSource { get; }

        bool AllowStorageItemDrop { get; }
    }
}