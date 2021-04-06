using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.Widgets
{
    public class WidgetsListControlViewModel : ObservableObject, IDisposable
    {
        public event EventHandler WidgetListRefreshRequestedInvoked;

        #region Public Properties

        public ObservableCollection<object> Widgets { get; private set; } = new ObservableCollection<object>();

        private bool showLibraryCards = true;
        public bool ShowLibraryCards
        {
            get => showLibraryCards;
            set
            {
                if (SetProperty(ref showLibraryCards, value))
                {
                    App.AppSettings.ShowLibraryCardsWidget = value;

                    if (value)
                    {
                        RefreshWidgetList();
                    }
                }
            }
        }

        private bool showDrives = true;
        public bool ShowDrives
        {
            get => showDrives;
            set
            {
                if (SetProperty(ref showDrives, value))
                {
                    App.AppSettings.ShowDrivesWidget = value;

                    if (value)
                    {
                        RefreshWidgetList();
                    }
                }
            }
        }

        private bool showBundles = true;
        public bool ShowBundles
        {
            get => showBundles;
            set
            {
                if (SetProperty(ref showBundles, value))
                {
                    App.AppSettings.ShowBundlesWidget = value;

                    if (value)
                    {
                        RefreshWidgetList();
                    }
                }
            }
        }

        private bool showRecentFiles = true;
        public bool ShowRecentFiles
        {
            get => showRecentFiles;
            set
            {
                if (SetProperty(ref showRecentFiles, value))
                {
                    App.AppSettings.ShowRecentFilesWidget = value;

                    if (value)
                    {
                        RefreshWidgetList();
                    }
                }
            }
        }

        #endregion

        private void RefreshWidgetList()
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
