// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for Widget, providing the Widget name, the right-click Context<see cref="MenuFlyout"/>.
	/// </summary>
	public class WidgetItem : ObservableObject, IDisposable
	{
		private readonly Action<bool> _expanderValueChangedCallback;
		private readonly Func<bool> _expanderValueRequestedCallback;

		private object? _WidgetControl;
		public object? WidgetControl
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

		public IWidgetViewModel? WidgetItemModel { get; }

		public string? WidgetAutomationProperties
			=> WidgetItemModel?.AutomationProperties;

		public bool ShowMenuFlyout
			=> WidgetItemModel?.ShowMenuFlyout ?? false;

		public MenuFlyoutItem? MenuFlyoutItem
			=> WidgetItemModel?.MenuFlyoutItem;

		public WidgetItem(object innerControl, IWidgetViewModel widgetModel, Action<bool> expanderValueChangedCallback, Func<bool> expanderValueRequestedCallback)
		{
			WidgetControl = innerControl;
			WidgetItemModel = widgetModel;

			_expanderValueChangedCallback = expanderValueChangedCallback;
			_expanderValueRequestedCallback = expanderValueRequestedCallback;
		}

		public void Dispose()
		{
			(WidgetControl as IDisposable)?.Dispose();
		}
	}
}
