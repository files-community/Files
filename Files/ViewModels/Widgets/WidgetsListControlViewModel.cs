using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlViewModel : ObservableObject
    {
        public ObservableCollection<IWidgetItemModel> Widgets { get; private set; } = new ObservableCollection<IWidgetItemModel>();

        public bool AddWidget(IWidgetItemModel widgetModel)
        {
            if (widgetModel == null)
            {
                return false;
            }

            Widgets.Add(widgetModel);
            return true;
        }

        public bool RemoveWidget(IWidgetItemModel widgetModel)
        {
            return Widgets.Remove(widgetModel);
        }

        public void ReorderWidget(IWidgetItemModel widgetModel, int place)
        {
            int widgetIndex = Widgets.IndexOf(widgetModel);
            Widgets.Move(widgetIndex, place);
        }
    }
}
