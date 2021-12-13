using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlItemViewModel : ObservableObject, IDisposable
    {
        private object widgetControl;

        public object WidgetControl
        {
            get => widgetControl;
            set => SetProperty(ref widgetControl, value);
        }

        public WidgetsListControlItemViewModel(object widgetControl)
        {
            this.WidgetControl = widgetControl;
        }

        public IWidgetItemModel WidgetItemModel
        {
            get => WidgetControl as IWidgetItemModel;
        }

        public string WidgetAutomationProperties
        {
            get => WidgetItemModel.AutomationProperties;
        }

        public void Dispose()
        {
            (WidgetControl as IDisposable)?.Dispose();
        }
    }
}