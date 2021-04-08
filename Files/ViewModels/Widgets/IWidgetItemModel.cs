using System;

namespace Files.ViewModels.Widgets
{
    public interface IWidgetItemModel : IDisposable
    {
        bool IsWidgetSettingEnabled { get; }
        string WidgetName { get; }
    }
}