// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Widgets
{
	public class WidgetsListControlViewModel : ObservableObject, IDisposable
	{
		public event EventHandler WidgetListRefreshRequestedInvoked;

		public ObservableCollection<WidgetsListControlItemViewModel> Widgets { get; private set; } = new();

		public void RefreshWidgetList()
		{
			for (int i = 0; i < Widgets.Count; i++)
			{
				if (!Widgets[i].WidgetItemModel.IsWidgetSettingEnabled)
				{
					RemoveWidgetAt(i);
				}
			}

			WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
		}

		public bool AddWidget(WidgetsListControlItemViewModel widgetModel)
		{
			return InsertWidget(widgetModel, Widgets.Count + 1);
		}

		public bool InsertWidget(WidgetsListControlItemViewModel widgetModel, int atIndex)
		{
			// The widget must not be null and must implement IWidgetItemModel
			if (widgetModel.WidgetItemModel is not IWidgetItemModel widgetItemModel)
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
			return !(Widgets.Any((item) => item.WidgetItemModel.WidgetName == widgetName));
		}

		public void RemoveWidgetAt(int index)
		{
			if (index < 0)
			{
				return;
			}

			Widgets[index].Dispose();
			Widgets.RemoveAt(index);
		}

		public void RemoveWidget<TWidget>() where TWidget : IWidgetItemModel
		{
			int indexToRemove = -1;

			for (int i = 0; i < Widgets.Count; i++)
			{
				if (typeof(TWidget).IsAssignableFrom(Widgets[i].WidgetControl.GetType()))
				{
					// Found matching types
					indexToRemove = i;
					break;
				}
			}

			RemoveWidgetAt(indexToRemove);
		}

		public void ReorderWidget(WidgetsListControlItemViewModel widgetModel, int place)
		{
			int widgetIndex = Widgets.IndexOf(widgetModel);
			Widgets.Move(widgetIndex, place);
		}

		public void Dispose()
		{
			for (int i = 0; i < Widgets.Count; i++)
			{
				Widgets[i].Dispose();
			}

			Widgets.Clear();
		}
	}
}
