// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an item of Files widget container.
	/// </summary>
	public class WidgetContainerItem : ObservableObject, IDisposable
	{
		// Fields

		private readonly Action<bool> _expanderValueChangedCallback;
		private readonly Func<bool> _expanderValueRequestedCallback;

		// Properties

		public string WidgetAutomationProperties
			=> WidgetItemModel.AutomationProperties;

		public bool ShowMenuFlyout
			=> WidgetItemModel.ShowMenuFlyout;

		public MenuFlyoutItem? MenuFlyoutItem
			=> WidgetItemModel.MenuFlyoutItem;

		private IWidgetViewModel _WidgetItemModel;
		public IWidgetViewModel WidgetItemModel
		{
			get => _WidgetItemModel;
			set => SetProperty(ref _WidgetItemModel, value);
		}

		private object _WidgetControl;
		public object WidgetControl
		{
			get => _WidgetControl;
			set => SetProperty(ref _WidgetControl, value);
		}

		public bool IsExpanded
		{
			get => _expanderValueRequestedCallback?.Invoke() ?? true;
			set
			{
				_expanderValueChangedCallback?.Invoke(value);
				OnPropertyChanged();
			}
		}

		// Constructor

		public WidgetContainerItem(
			UserControl widget,
			IWidgetViewModel widgetViewModel,
			Action<bool> expanderValueChangedCallback,
			Func<bool> expanderValueRequestedCallback)
		{
			WidgetItemModel = widgetViewModel;
			WidgetControl = widget;

			_expanderValueChangedCallback = expanderValueChangedCallback;
			_expanderValueRequestedCallback = expanderValueRequestedCallback;
		}

		// Disposer

		public void Dispose()
		{
			(WidgetControl as IDisposable)?.Dispose();
		}
	}
}
