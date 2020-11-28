using System;

namespace Files.UserControls.MultitaskingControl
{
    public interface ITabItem : IDisposable
    {
        string Path { get; }

        object NavigationArgs { get; }
    }
}
