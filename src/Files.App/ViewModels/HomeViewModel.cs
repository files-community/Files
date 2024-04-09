// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public sealed class HomeViewModel : ObservableObject, IDisposable
	{
		public ObservableCollection<WidgetContainerItem> WidgetItems { get; } = [];

		public ICommand HomePageLoadedCommand { get; }

		public event EventHandler<RoutedEventArgs>? HomePageLoadedInvoked;
		public event EventHandler? WidgetListRefreshRequestedInvoked;

		public HomeViewModel()
		{
			HomePageLoadedCommand = new RelayCommand<RoutedEventArgs>(ExecuteHomePageLoadedCommand);
		}

		private void ExecuteHomePageLoadedCommand(RoutedEventArgs? e)
		{
			HomePageLoadedInvoked?.Invoke(this, e!);
		}

		public void RefreshWidgetList()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
			{
				if (!WidgetItems[i].WidgetItemModel.IsWidgetSettingEnabled)
				{
					RemoveWidgetAt(i);
				}
			}

			WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
		}

		public bool AddWidget(WidgetContainerItem widgetModel)
		{
			return InsertWidget(widgetModel, WidgetItems.Count + 1);
		}

		public bool InsertWidget(WidgetContainerItem widgetModel, int atIndex)
		{
			// The widget must not be null and must implement IWidgetItemModel
			if (widgetModel.WidgetItemModel is not IWidgetViewModel widgetItemModel)
			{
				return false;
			}

			// Don't add existing ones!
			if (!CanAddWidget(widgetItemModel.WidgetName))
			{
				return false;
			}

			if (atIndex > WidgetItems.Count)
			{
				WidgetItems.Add(widgetModel);
			}
			else
			{
				WidgetItems.Insert(atIndex, widgetModel);
			}

			return true;
		}

		public bool CanAddWidget(string widgetName)
		{
			return !(WidgetItems.Any((item) => item.WidgetItemModel.WidgetName == widgetName));
		}

		public void RemoveWidgetAt(int index)
		{
			if (index < 0)
			{
				return;
			}

			WidgetItems[index].Dispose();
			WidgetItems.RemoveAt(index);
		}

		public void RemoveWidget<TWidget>() where TWidget : IWidgetViewModel
		{
			int indexToRemove = -1;

			for (int i = 0; i < WidgetItems.Count; i++)
			{
				if (typeof(TWidget).IsAssignableFrom(WidgetItems[i].WidgetControl.GetType()))
				{
					// Found matching types
					indexToRemove = i;
					break;
				}
			}

			RemoveWidgetAt(indexToRemove);
		}

		public void ReorderWidget(WidgetContainerItem widgetModel, int place)
		{
			int widgetIndex = WidgetItems.IndexOf(widgetModel);
			WidgetItems.Move(widgetIndex, place);
		}

		public void Dispose()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
				WidgetItems[i].Dispose();

			WidgetItems.Clear();
		}
	}
}
