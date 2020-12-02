using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.UserControls.MultitaskingControl
{
    public interface ITabItemControl : IDisposable
    {
        string Header { get; }

        string Description { get; }

        IconSource IconSource { get; }

        object Content { get; }

        bool AllowStorageItemDrop { get; }
    }
}
