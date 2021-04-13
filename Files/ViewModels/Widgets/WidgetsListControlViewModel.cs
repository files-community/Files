using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlViewModel : ObservableObject, IDisposable
    {
        public event EventHandler WidgetListRefreshRequestedInvoked;

        public ObservableCollection<object> Widgets { get; private set; } = new ObservableCollection<object>();

        public void RefreshWidgetList()
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                if (!(Widgets[i] as IWidgetItemModel).IsWidgetSettingEnabled)
                {
                    RemoveWidgetAt(i);
                }
            }

            WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
        }

        public bool AddWidget(object widgetModel)
        {
            return InsertWidget(widgetModel, Widgets.Count + 1);
        }

        public bool InsertWidget(object widgetModel, int atIndex)
        {
            // The widget must not be null and must implement IWidgetItemModel
            if (!(widgetModel is IWidgetItemModel widgetItemModel))
            {
                return false;
            }

            // Don't add existing ones!
            if (!CanAddWidget(widgetItemModel.WidgetName))
            {
                return false;
            }

            if (atIndex > Widgets.Count)
            {
                Widgets.Add(widgetModel);
            }
            else
            {
                Widgets.Insert(atIndex, widgetModel);
            }

            return true;
        }

        public bool CanAddWidget(string widgetName)
        {
            return !(Widgets.Any((item) => (item as IWidgetItemModel).WidgetName == widgetName));
        }

        public void RemoveWidgetAt(int index)
        {
            if (index < 0)
            {
                return;
            }

            (Widgets[index] as IDisposable)?.Dispose();
            Widgets.RemoveAt(index);
        }

        public void RemoveWidget<TWidget>() where TWidget : IWidgetItemModel
        {
            int indexToRemove = -1;

            for (int i = 0; i < Widgets.Count; i++)
            {
                if (typeof(TWidget).IsAssignableFrom(Widgets[i].GetType()))
                {
                    // Found matching types
                    indexToRemove = i;
                    break;
                }
            }

            RemoveWidgetAt(indexToRemove);
        }

        public void ReorderWidget(object widgetModel, int place)
        {
            int widgetIndex = Widgets.IndexOf(widgetModel);
            Widgets.Move(widgetIndex, place);
        }

        public void Dispose()
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                (Widgets[i] as IDisposable)?.Dispose();
            }

            Widgets.Clear();
            Widgets = null;
        }
    }
}