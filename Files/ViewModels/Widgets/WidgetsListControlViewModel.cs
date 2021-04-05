using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlViewModel : ObservableObject, IDisposable
    {
        public ObservableCollection<Control> Widgets { get; private set; } = new ObservableCollection<Control>();

        public bool AddWidget(Control widgetModel)
        {
            return InsertWidget(widgetModel, Widgets.Count + 1);
        }

        public bool InsertWidget(Control widgetModel, int atIndex)
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

        public void RemoveWidget(Control widgetModel)
        {
            int indexToRemove = Widgets.IndexOf(widgetModel);
            (Widgets[indexToRemove] as IDisposable)?.Dispose();
            Widgets.RemoveAt(indexToRemove);
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

            if (indexToRemove == -1)
            {
                return;
            }

            RemoveWidget(Widgets[indexToRemove]);
        }

        public void ReorderWidget(Control widgetModel, int place)
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
