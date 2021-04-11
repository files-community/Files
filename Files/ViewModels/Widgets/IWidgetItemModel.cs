using System;

namespace Files.ViewModels.Widgets
{
    public interface IWidgetItemModel : IDisposable
    {
        string WidgetName { get; }

        bool IsWidgetSettingEnabled { get; }
    }
}