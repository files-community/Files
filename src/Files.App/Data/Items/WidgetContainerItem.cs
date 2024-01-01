// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public class WidgetContainerItem : ObservableObject, IDisposable
	{
		// Fields

		private readonly Action<bool> _expanderValueChangedCallback;
		private readonly Func<bool> _expanderValueRequestedCallback;

		// Properties

		public string WidgetAutomationProperties
			=> WidgetViewModel.AutomationProperties;

		public bool ShowMenuFlyout
			=> WidgetViewModel.ShowMenuFlyout;
		
		public bool IsExpanded
		{
			get => _expanderValueRequestedCallback?.Invoke() ?? true;
			set
			{
				_expanderValueChangedCallback?.Invoke(value);
				OnPropertyChanged();
			}
		}

		public MenuFlyoutItem MenuFlyoutItem
			=> WidgetViewModel.MenuFlyoutItem;

		public IWidgetViewModel WidgetViewModel
		{
			get
			{
				if (WidgetControl is DrivesWidget drivesWidget)
					return drivesWidget.ViewModel!;
				else if (WidgetControl is FileTagsWidget fileTagsWidget)
					return fileTagsWidget.ViewModel!;
				else if (WidgetControl is QuickAccessWidget quickAccessWidget)
					return quickAccessWidget.ViewModel!;
				else if (WidgetControl is RecentFilesWidget recentFilesWidget)
					return recentFilesWidget.ViewModel!;
				else
					return default;
			}
		}

		private object? _WidgetControl;
		public object? WidgetControl
		{
			get => _WidgetControl;
			set => SetProperty(ref _WidgetControl, value);
		}

		// Constructor

		public WidgetContainerItem(object widgetControl, Action<bool> expanderValueChangedCallback, Func<bool> expanderValueRequestedCallback)
		{
			WidgetControl = widgetControl;

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
