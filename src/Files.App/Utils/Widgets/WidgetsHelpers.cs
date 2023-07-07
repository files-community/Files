// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;

namespace Files.App.Utils.Widgets
{
	/// <summary>
	/// Provides static helper for Home page Widgets.
	/// </summary>
	public static class WidgetsHelpers
	{
		public static TWidget? TryGetWidget<TWidget>(IGeneralSettingsService generalSettingsService, WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget? defaultValue = default) where TWidget : IWidgetItemModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(generalSettingsService);

			if (canAddWidget && isWidgetSettingEnabled)
			{
				shouldReload = true;
				return new TWidget();
			}
			else if (!canAddWidget && !isWidgetSettingEnabled) // The widgets exists but the setting has been disabled for it
			{
				// Remove the widget
				widgetsViewModel.RemoveWidget<TWidget>();
				shouldReload = false;
				return default;
			}
			else if (!isWidgetSettingEnabled)
			{
				shouldReload = false;
				return default;
			}

			shouldReload = EqualityComparer<TWidget>.Default.Equals(defaultValue, default);

			return (defaultValue);
		}

		public static bool TryGetIsWidgetSettingEnabled<TWidget>(IGeneralSettingsService generalSettingsService) where TWidget : IWidgetItemModel
		{
			if (typeof(TWidget) == typeof(QuickAccessWidget))
				return generalSettingsService.ShowQuickAccessWidget;

			if (typeof(TWidget) == typeof(DrivesWidget))
				return generalSettingsService.ShowDrivesWidget;

			if (typeof(TWidget) == typeof(FileTagsWidget))
				return generalSettingsService.ShowFileTagsWidget;

			if (typeof(TWidget) == typeof(RecentFilesWidget))
				return generalSettingsService.ShowRecentFilesWidget;

			// A custom widget it is - TWidget implements ICustomWidgetItemModel
			// Return true for custom widgets - they're always enabled
			return typeof(ICustomWidgetItemModel).IsAssignableFrom(typeof(TWidget));
		}
	}
}
