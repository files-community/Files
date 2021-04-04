using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlViewModel : ObservableObject
    {
        public ObservableCollection<Control> Widgets { get; private set; } = new ObservableCollection<Control>();

        public bool AddWidget(Control widgetModel)
        {
            // The widget must not be null and must implement IWidgetItemModel
            if (!(widgetModel is IWidgetItemModel widgetItemModel))
            {
                return false;
            }

            if (Widgets.Any((item) => (item as IWidgetItemModel).WidgetName == widgetItemModel.WidgetName))
            {
                return false;
            }

            Widgets.Add(widgetModel);
            return true;
        }

        public void RemoveWidget(Control widgetModel)
        {
            int indexToRemove = Widgets.IndexOf(widgetModel);
            (Widgets[indexToRemove] as IDisposable)?.Dispose();
            Widgets.RemoveAt(indexToRemove);
        }

        public void ReorderWidget(Control widgetModel, int place)
        {
            int widgetIndex = Widgets.IndexOf(widgetModel);
            Widgets.Move(widgetIndex, place);
        }
    }
}
